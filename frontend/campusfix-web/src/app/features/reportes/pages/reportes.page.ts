import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ReportesService } from '../services/reportes.service';
import {
  Reporte,
  Persona,
  Entrada,
  ESTADOS,
  PRIORIDADES,
} from '../models/reporte';
import { DialogFocus } from '../components/dialog-focus';
import { EstadoBadge } from '../components/estado-badge';

@Component({
  selector: 'app-reportes',
  imports: [CommonModule, FormsModule, EstadoBadge, DialogFocus],
  templateUrl: './reportes.page.html',
})
export class ReportesPage {
  api = inject(ReportesService);
  vista = inject(ActivatedRoute).snapshot.data['vista'] as string;

  reportes = signal<Reporte[]>([]);
  personas = signal<Persona[]>([]);
  cargando = signal(true);
  ocupado = signal(false);
  error = signal('');
  aviso = signal('');
  modal = signal(false);
  detalle = signal<Reporte | null>(null);

  editarId?: number;

  buscar = signal('');
  estado = signal(0);
  prioridad = signal(0);

  estados = ESTADOS;
  prioridades = PRIORIDADES;
  pasos = [1, 2, 3, 4];

  form: Entrada = this.vacio();

  nombre = '';
  correo = '';

  // Texto utilizado para documentar la solución del incidente.
  solucion = '';

  filtrados = computed(() =>
    this.reportes().filter(
      (r) =>
        (!this.estado() || r.estado === this.estado()) &&
        (!this.prioridad() || r.prioridad === this.prioridad()) &&
        `${r.id} ${r.problema} ${r.ubicacion} ${r.reportadoPor}`
          .toLowerCase()
          .includes(this.buscar().toLowerCase()),
    ),
  );

  abiertos = computed(
    () => this.reportes().filter((r) => r.estado !== 4).length,
  );

  urgentes = computed(
    () =>
      this.reportes().filter(
        (r) => r.prioridad >= 3 && r.estado !== 4,
      ).length,
  );

  resueltos = computed(
    () => this.reportes().filter((r) => r.estado === 4).length,
  );

  constructor() {
    this.cargar();
  }

  vacio(): Entrada {
    return {
      problema: '',
      ubicacion: '',
      descripcion: '',
      prioridad: 2,
      reportadoPorId: 0,
    };
  }

  cargar() {
    this.cargando.set(true);
    this.error.set('');

    forkJoin({
      r: this.api.listar(),
      p: this.api.personas(),
    }).subscribe({
      next: ({ r, p }) => {
        this.reportes.set(r);
        this.personas.set(p);
        this.cargando.set(false);
      },
      error: (e) => {
        this.fallo(e);
        this.cargando.set(false);
      },
    });
  }

  fallo(e: any) {
    this.ocupado.set(false);

    this.error.set(
      e.error?.title ||
        'No se pudo conectar con la API. Comprueba que el backend y PostgreSQL estén encendidos.',
    );
  }

  nuevo() {
    this.form = this.vacio();
    this.editarId = undefined;
    this.error.set('');
    this.modal.set(true);
  }

  editar(r: Reporte) {
    this.form = {
      problema: r.problema,
      ubicacion: r.ubicacion,
      descripcion: r.descripcion || '',
      prioridad: r.prioridad,
      reportadoPorId: r.reportadoPorId,
    };

    this.editarId = r.id;
    this.detalle.set(null);
    this.modal.set(true);
  }

  guardar() {
    if (this.ocupado()) return;

    this.ocupado.set(true);
    this.error.set('');

    this.api.guardar(this.form, this.editarId).subscribe({
      next: (r) => {
        this.reportes.update((xs) => [
          r,
          ...xs.filter((x) => x.id !== r.id),
        ]);

        this.modal.set(false);
        this.ocupado.set(false);
        this.aviso.set('Reporte guardado correctamente.');
        this.detalle.set(r);
        this.solucion = r.solucion || '';
      },
      error: (e) => this.fallo(e),
    });
  }

  avanzar(r: Reporte) {
    if (this.ocupado() || r.estado === 4) return;

    this.ocupado.set(true);
    this.error.set('');

    this.api.cambiar(r.id, r.estado + 1).subscribe({
      next: (n) => {
        this.reportes.update((xs) =>
          xs.map((x) => (x.id === n.id ? n : x)),
        );

        this.detalle.set(n);
        this.solucion = n.solucion || '';
        this.ocupado.set(false);
        this.aviso.set(
          'Estado actualizado e historial registrado.',
        );
      },
      error: (e) => {
        this.fallo(e);

        this.api.listar().subscribe({
          next: (xs) => {
            this.reportes.set(xs);

            const actualizado =
              xs.find((x) => x.id === r.id) || null;

            this.detalle.set(actualizado);
            this.solucion = actualizado?.solucion || '';
          },
          error: () => {},
        });
      },
    });
  }

  registrarSolucion(r: Reporte) {
    if (this.ocupado()) return;

    const texto = this.solucion.trim();

    if (!texto) {
      this.error.set(
        'Debes escribir la solución aplicada al incidente.',
      );
      return;
    }

    this.ocupado.set(true);
    this.error.set('');

    this.api.registrarSolucion(r.id, texto).subscribe({
      next: (n) => {
        this.reportes.update((xs) =>
          xs.map((x) => (x.id === n.id ? n : x)),
        );

        this.detalle.set(n);
        this.solucion = n.solucion || '';
        this.ocupado.set(false);

        this.aviso.set(
          `Solución del reporte #${n.id} registrada correctamente.`,
        );
      },
      error: (e) => this.fallo(e),
    });
  }

  archivar(r: Reporte) {
    if (this.ocupado()) return;

    const confirmar = window.confirm(
      `¿Deseas archivar el reporte #${r.id}?\n\n` +
        'El reporte dejará de aparecer en los listados, pero permanecerá guardado en PostgreSQL.',
    );

    if (!confirmar) return;

    this.ocupado.set(true);
    this.error.set('');

    this.api.archivar(r.id).subscribe({
      next: () => {
        this.reportes.update((xs) =>
          xs.filter((x) => x.id !== r.id),
        );

        this.detalle.set(null);
        this.solucion = '';
        this.ocupado.set(false);
        this.aviso.set(
          `Reporte #${r.id} archivado correctamente.`,
        );
      },
      error: (e) => this.fallo(e),
    });
  }

  crearPersona() {
    if (this.ocupado()) return;

    this.ocupado.set(true);
    this.error.set('');

    this.api.crearPersona(this.nombre, this.correo).subscribe({
      next: (p) => {
        this.personas.update((ps) => [...ps, p]);

        this.nombre = '';
        this.correo = '';
        this.ocupado.set(false);

        this.aviso.set(
          'Persona registrada. Ya puede reportar incidencias.',
        );
      },
      error: (e) => this.fallo(e),
    });
  }

  grupo(e: number) {
    return this.filtrados().filter((r) => r.estado === e);
  }

  abrirDetalle(r: Reporte) {
    this.detalle.set(r);
    this.solucion = r.solucion || '';
    this.error.set('');
  }

  cerrar() {
    if (!this.ocupado()) {
      this.modal.set(false);
      this.detalle.set(null);
      this.solucion = '';
    }
  }
}