import {TestBed} from '@angular/core/testing';
import {DonutChart} from './charts';
describe('Porcentajes visibles',()=>{
 it('redondea fracciones sin mostrar decimales extensos y admite total cero',()=>{
  const f=TestBed.createComponent(DonutChart);
  f.componentRef.setInput('points',[{label:'Resueltos',value:7},{label:'Pendientes',value:10}]);f.detectChanges();
  expect(f.nativeElement.querySelector('.donut-legend small').textContent).toBe('41.2%');
  f.componentRef.setInput('points',[{label:'Vacío',value:0}]);f.detectChanges();
  expect(f.nativeElement.querySelector('.donut-legend small').textContent).toBe('0%');f.destroy();
 });
});
