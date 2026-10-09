import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Entrada, Persona, Reporte } from '../models/reporte';

@Injectable({ providedIn: 'root' })
export class ReportesService {
  private http = inject(HttpClient);

  listar() {
    return this.http.get<Reporte[]>('/api/reportes');
  }

  personas() {
    return this.http.get<Persona[]>('/api/usuarios');
  }

  crearPersona(nombre: string, correo: string) {
    return this.http.post<Persona>('/api/usuarios', {
      nombre,
      correo,
    });
  }

  guardar(d: Entrada, id?: number) {
    return id
      ? this.http.put<Reporte>('/api/reportes/' + id, d)
      : this.http.post<Reporte>('/api/reportes', d);
  }

  cambiar(id: number, estado: number) {
    return this.http.patch<Reporte>(
      `/api/reportes/${id}/estado`,
      { estado },
    );
  }

  registrarSolucion(id: number, solucion: string) {
    return this.http.put<Reporte>(
      `/api/reportes/${id}/solucion`,
      { solucion },
    );
  }

  archivar(id: number) {
    return this.http.delete<void>(`/api/reportes/${id}`);
  }
}