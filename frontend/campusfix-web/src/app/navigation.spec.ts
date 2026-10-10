import {TestBed} from '@angular/core/testing';
import {provideRouter,Router} from '@angular/router';
import {provideHttpClient} from '@angular/common/http';
import {provideHttpClientTesting} from '@angular/common/http/testing';
import {RouterTestingHarness} from '@angular/router/testing';
import {routes} from './app.routes';
import {Auth} from './core/auth';
import {Workspace} from './features/workspace/workspace';
import {Shell} from './layout/shell';
describe('Navegación final',()=>{
 beforeEach(()=>{sessionStorage.clear();TestBed.configureTestingModule({providers:[provideRouter(routes),provideHttpClient(),provideHttpClientTesting()]});});
 function signed(role:string,permissions:any){let a=TestBed.inject(Auth);a.user.set({id:'test',nombre:'Cuenta de prueba',role,permissions});vi.spyOn(a,'restore').mockResolvedValue(true);vi.spyOn(Workspace.prototype,'load').mockResolvedValue();return a;}
 it('rutas privadas reales redirigen al login sin sidebar',async()=>{const h=await RouterTestingHarness.create();await h.navigateByUrl('/panel');expect(TestBed.inject(Router).url).toBe('/login');expect(h.fixture.nativeElement.querySelector('.sidebar')).toBeNull();await h.navigateByUrl('/personas');expect(TestBed.inject(Router).url).toBe('/login');});
 it('rutas compatibles conservan navegación a tablero y usuarios',async()=>{signed('Administrador',{panel:true,tablero:true,usuarios:true});const h=await RouterTestingHarness.create();await h.navigateByUrl('/reportes');expect(TestBed.inject(Router).url).toBe('/tablero');await h.navigateByUrl('/personas');expect(TestBed.inject(Router).url).toBe('/usuarios');await h.navigateByUrl('/no-existe');expect(TestBed.inject(Router).url).toBe('/panel');});
 it('técnico no entra a roles ni usuarios, y menú respeta permisos',async()=>{signed('Técnico',{panel:true,tablero:true,conocimiento:true});const h=await RouterTestingHarness.create();await h.navigateByUrl('/roles');expect(TestBed.inject(Router).url).toBe('/panel');await h.navigateByUrl('/usuarios');expect(TestBed.inject(Router).url).toBe('/tablero');expect(h.fixture.nativeElement.querySelector('nav a[href="/usuarios"]')).toBeNull();expect(h.fixture.nativeElement.querySelector('nav a[href="/roles"]')).toBeNull();});
 it('cuenta con cambio obligatorio no puede entrar al tablero',async()=>{const a=signed('Solicitante',{panel:true,tablero:true});a.user.update((u:any)=>({...u,mustChange:true}));const h=await RouterTestingHarness.create();await h.navigateByUrl('/tablero');expect(TestBed.inject(Router).url).toBe('/cuenta');});
 it('menú móvil comunica estado y cierra con Escape devolviendo foco',async()=>{signed('Administrador',{panel:true,tablero:true});const f=TestBed.createComponent(Shell);f.detectChanges();const toggle=f.nativeElement.querySelector('.mobile-toggle') as HTMLButtonElement;toggle.click();await f.whenStable();f.detectChanges();expect(toggle.getAttribute('aria-expanded')).toBe('true');document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));f.detectChanges();expect(toggle.getAttribute('aria-expanded')).toBe('false');f.destroy();});
});

