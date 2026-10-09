import {dayKey,trend,sla,csvRows,mean} from './metrics';
describe('Indicadores y exportación',()=>{
 it('agrupa por Guayaquil y cuenta cierres de tickets anteriores al rango',()=>{const t=[{id:'1',fechaReporte:'2026-09-20T12:00:00Z',fechaSolucion:'2026-10-02T01:00:00Z',estado:4},{id:'2',fechaReporte:'2026-10-02T03:00:00Z',estado:1}];expect(dayKey(t[1].fechaReporte)).toBe('2026-10-01');const points=trend(t,'2026-10-01','2026-10-02','America/Guayaquil');expect(points[0].values).toEqual([1,1]);expect(points[0].items.length).toBe(2);expect(points[1].values).toEqual([0,0]);});
 it('distingue ausencia de datos de cero y congela el cumplimiento al cierre',()=>{expect(mean([])).toBeNull();expect(mean([0])).toBe(0);expect(sla({estado:1},Date.now())).toBe('Sin meta');expect(sla({estado:4,fechaSolucion:'2026-10-01T12:00:00Z',meta:{resolutionDue:'2026-10-01T13:00:00Z'}},Date.now())).toBe('Dentro de meta');});
 it('escapa delimitadores, comillas y fórmulas en CSV',()=>{const csv=csvRows([['=1+2','texto;con "comillas"','línea\nnueva','@SUM(A1)']]);expect(csv).toContain('"\'=1+2"');expect(csv).toContain('"texto;con ""comillas"""');expect(csv).toContain('"\'@SUM(A1)"');});
});
