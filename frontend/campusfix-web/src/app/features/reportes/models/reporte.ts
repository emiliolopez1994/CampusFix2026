export interface Persona {
  id: number;
  nombre: string;
  correo: string;
}

export interface Historial {
  id: number;
  estadoAnterior: number;
  estadoNuevo: number;
  fechaCambio: string;
}

export interface Reporte {
  id: number;
  problema: string;
  ubicacion: string;
  descripcion: string | null;
  prioridad: number;
  estado: number;
  reportadoPorId: number;
  reportadoPor: string;
  fechaReporte: string;
  fechaActualizacion: string;

  // Solución documentada del incidente.
  solucion: string | null;
  fechaSolucion: string | null;

  historial: Historial[];
}

export interface Entrada {
  problema: string;
  ubicacion: string;
  descripcion: string;
  prioridad: number;
  reportadoPorId: number;
}

export const ESTADOS = [
  '',
  'Reportado',
  'En revisión',
  'En reparación',
  'Solucionado',
];

export const PRIORIDADES = [
  '',
  'Baja',
  'Media',
  'Alta',
  'Crítica',
];