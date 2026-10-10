import {TestBed} from '@angular/core/testing';
import {provideHttpClient} from '@angular/common/http';
import {provideHttpClientTesting,HttpTestingController} from '@angular/common/http/testing';
import {provideRouter,Router} from '@angular/router';
import {Auth} from './auth';
describe('Destino de inicio de sesión',()=>{
 it('abre tablero en tres ingresos aunque panel esté deshabilitado',async()=>{
  TestBed.configureTestingModule({providers:[provideRouter([]),provideHttpClient(),provideHttpClientTesting()]});
  const auth=TestBed.inject(Auth),http=TestBed.inject(HttpTestingController),navigate=vi.spyOn(TestBed.inject(Router),'navigateByUrl').mockResolvedValue(true);
  for(let i=0;i<3;i++){const login=auth.login({correo:'a@example.edu',password:'Local12345678'});http.expectOne('/api/auth/login').flush({token:'synthetic',role:'Solicitante',areaId:'biblioteca',mustChange:false,needsProfile:false,permissions:{tablero:true,panel:false}});await login;expect(navigate).toHaveBeenLastCalledWith('/tablero');auth.clear();}
  http.verify();vi.restoreAllMocks();
 });
 it('solo flags explícitos requieren cuenta y se libera al completarlos',()=>{
  TestBed.configureTestingModule({providers:[provideRouter([]),provideHttpClient()]});const auth=TestBed.inject(Auth);
  auth.user.set({mustChange:true,permissions:{tablero:true}});expect(auth.home()).toBe('/cuenta');
  auth.user.set({needsProfile:true,permissions:{tablero:true}});expect(auth.home()).toBe('/cuenta');
  auth.user.set({needsProfile:false,mustChange:false,permissions:{tablero:true}});expect(auth.home()).toBe('/tablero');auth.clear();
 });
});
