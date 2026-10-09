import { Component, input } from '@angular/core';
import { ESTADOS } from '../models/reporte';
@Component({
  selector: 'app-estado',
  template: `<span class="badge" [class]="'badge estado-' + valor()"
    ><span class="dot"></span>{{ estados[valor()] }}</span
  >`,
})
export class EstadoBadge {
  valor = input.required<number>();
  estados = ESTADOS;
}
