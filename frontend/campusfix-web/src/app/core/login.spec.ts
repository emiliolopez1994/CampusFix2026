import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { Login } from './login';
import { Auth } from './auth';
describe('Acceso CampusFix',()=>{
 it('conserva el acceso y registro reales, sin guardar contraseñas',async()=>{
 localStorage.removeItem('campusfix-login-email');const login=vi.fn().mockResolvedValue(undefined);
 TestBed.configureTestingModule({providers:[{provide:Auth,useValue:{login}}]});
 const f=TestBed.createComponent(Login);f.detectChanges();await f.whenStable();
 const c=f.componentInstance;c.correo=' persona@institucion.edu ';c.password='MiClaveSegura123';c.remember=true;
 await c.submit();expect(login).toHaveBeenCalledWith({correo:'persona@institucion.edu',password:'MiClaveSegura123',nombre:''},false);
 expect(localStorage.getItem('campusfix-login-email')).toBe('persona@institucion.edu');
 c.changeMode(true);c.nombre='Persona';await c.submit();expect(login).toHaveBeenLastCalledWith({correo:'persona@institucion.edu',password:'MiClaveSegura123',nombre:'Persona'},true);
 localStorage.removeItem('campusfix-login-email');f.destroy();
 });
 it('muestra errores del acceso y permite navegar la presentación y recuperar ayuda',async()=>{
 const login=vi.fn().mockRejectedValue({error:{title:'Credenciales incorrectas'}});
 TestBed.configureTestingModule({providers:[{provide:Auth,useValue:{login}}]});const f=TestBed.createComponent(Login);f.detectChanges();await f.whenStable();
 await f.componentInstance.submit();f.detectChanges();expect(f.nativeElement.querySelector('[role=alert]').textContent).toContain('Credenciales incorrectas');
 f.nativeElement.querySelector('[aria-label=Siguiente]').click();f.detectChanges();expect(f.nativeElement.textContent).toContain('Las personas primero.');expect(f.nativeElement.querySelector('.active-photo').getAttribute('src')).toBe('/login/comunidad.png');expect(new Set(Array.from(f.nativeElement.querySelectorAll('.campus-photo')).map((e:any)=>e.getAttribute('src'))).size).toBe(4);f.nativeElement.querySelector('[aria-label="Pausar carrusel"]').click();expect(f.componentInstance.playing()).toBe(false);
 f.nativeElement.querySelector('[aria-controls=access-help]').click();f.detectChanges();expect(f.nativeElement.querySelector('#access-help').textContent).toContain('administrador');f.destroy();
 });
});
