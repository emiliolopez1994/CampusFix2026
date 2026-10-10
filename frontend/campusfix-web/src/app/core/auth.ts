import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpInterceptorFn } from '@angular/common/http';
import { Router, CanActivateFn } from '@angular/router';
import { firstValueFrom, catchError, throwError } from 'rxjs';
@Injectable({providedIn:'root'})
export class Auth {
 http=inject(HttpClient); router=inject(Router); user=signal<any>(null); organization=signal('Mi institución'); unread=signal(0); token=signal(sessionStorage.getItem('campusfix-token')||'');
 registrationAreas(){return firstValueFrom(this.http.get<any[]>('/api/auth/areas'));}
 async restore(){if(!this.token())return false;try{this.user.set(await firstValueFrom(this.http.get('/api/auth/me')));return true;}catch{this.clear();return false;}}
 async login(data:any,register=false){let r:any=await firstValueFrom(this.http.post('/api/auth/'+(register?'registro':'login'),data));sessionStorage.setItem('campusfix-token',r.token);this.token.set(r.token);this.user.set(r);await this.router.navigateByUrl(r.mustChange?'/cuenta':'/panel');}
 clear(){this.unread.set(0);this.user.set(null);this.token.set('');sessionStorage.removeItem('campusfix-token');}
 async logout(){try{await firstValueFrom(this.http.post('/api/auth/logout',{}));}finally{this.clear();await this.router.navigateByUrl('/login');}}
 can(key:string){return !!this.user()?.permissions?.[key];}
}
export const authGuard:CanActivateFn=async(route,state)=>{let auth=inject(Auth),router=inject(Router);if(!await auth.restore())return router.parseUrl('/login');if(auth.user()?.mustChange&&!state.url.startsWith('/cuenta'))return router.parseUrl('/cuenta');let key=route.data?.['permission'];return !key||auth.can(key)?true:router.parseUrl(auth.can('panel')?'/panel':'/cuenta');};
export const ownerGuard:CanActivateFn=()=>{let a=inject(Auth);return a.user()?.role==='Superadministrador'||inject(Router).parseUrl('/panel');};
export const authInterceptor:HttpInterceptorFn=(req,next)=>{let auth=inject(Auth);if(req.url.startsWith('/api/')&&auth.token())req=req.clone({setHeaders:{Authorization:'Bearer '+auth.token()}});return next(req).pipe(catchError(e=>{if(e.status===401&&!req.url.includes('/auth/login')){auth.clear();void auth.router.navigateByUrl('/login');}return throwError(()=>e);}));};
