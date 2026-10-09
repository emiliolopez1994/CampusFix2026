import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Auth, authGuard, authInterceptor } from './core/auth';
import { Login } from './core/login';
describe('Acceso CampusFix', () => {
 beforeEach(()=>{sessionStorage.clear();TestBed.configureTestingModule({providers:[provideRouter([]),provideHttpClient(withInterceptors([authInterceptor])),provideHttpClientTesting()]});});
 afterEach(()=>TestBed.inject(HttpTestingController).verify());
 it('login separado del dashboard',async()=>{let f=TestBed.createComponent(Login);await f.whenStable();expect(f.nativeElement.querySelector('form')).not.toBeNull();expect(f.nativeElement.querySelector('nav')).toBeNull();expect(f.nativeElement.querySelector('.sidebar')).toBeNull();});
 it('redirige rutas privadas sin sesión',async()=>{let result=await TestBed.runInInjectionContext(()=>authGuard({data:{}} as any,{url:'/panel'} as any));expect(TestBed.inject(Router).serializeUrl(result as any)).toBe('/login');});
 it('guarda JWT y navega al panel',async()=>{let a=TestBed.inject(Auth);let nav=vi.spyOn(a.router,'navigateByUrl').mockResolvedValue(true);let p=a.login({correo:'prueba@example.test',password:'test'});TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush({token:'jwt-de-prueba',nombre:'Prueba',permissions:{panel:true}});await p;expect(sessionStorage.getItem('campusfix-token')).toBe('jwt-de-prueba');expect(nav).toHaveBeenCalledWith('/panel');});
 it('envía Bearer y elimina sesiones rechazadas',async()=>{let a=TestBed.inject(Auth);a.token.set('jwt-rechazado');vi.spyOn(a.router,'navigateByUrl').mockResolvedValue(true);let p=a.restore();let req=TestBed.inject(HttpTestingController).expectOne('/api/auth/me');expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-rechazado');req.flush({}, {status:401,statusText:'Unauthorized'});expect(await p).toBe(false);expect(a.token()).toBe('');});
 it('cierra sesión y limpia el token',async()=>{let a=TestBed.inject(Auth);a.token.set('jwt');sessionStorage.setItem('campusfix-token','jwt');let nav=vi.spyOn(a.router,'navigateByUrl').mockResolvedValue(true);let p=a.logout();TestBed.inject(HttpTestingController).expectOne('/api/auth/logout').flush({ok:true});await p;expect(sessionStorage.getItem('campusfix-token')).toBeNull();expect(nav).toHaveBeenCalledWith('/login');});
 it('impide rutas sin permiso',async()=>{let a=TestBed.inject(Auth);vi.spyOn(a,'restore').mockResolvedValue(true);a.user.set({permissions:{panel:true,usuarios:false}});let result=await TestBed.runInInjectionContext(()=>authGuard({data:{permission:'usuarios'}} as any,{url:'/usuarios'} as any));expect(TestBed.inject(Router).serializeUrl(result as any)).toBe('/panel');});
 it('sin permiso de panel redirige a cuenta sin un bucle',async()=>{let a=TestBed.inject(Auth);vi.spyOn(a,'restore').mockResolvedValue(true);a.user.set({permissions:{panel:false}});let result=await TestBed.runInInjectionContext(()=>authGuard({data:{permission:'panel'}} as any,{url:'/panel'} as any));expect(TestBed.inject(Router).serializeUrl(result as any)).toBe('/cuenta');});
});
