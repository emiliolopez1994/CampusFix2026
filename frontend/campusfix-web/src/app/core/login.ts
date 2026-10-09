import { Component,inject,signal,OnInit,OnDestroy } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { Auth } from './auth';
import { Icon } from './icon';
@Component({selector:'cf-login',imports:[ReactiveFormsModule,Icon],templateUrl:'./login.html',styleUrl:'./login.css'})
export class Login implements OnInit,OnDestroy {
 playing=signal(true);hovered=false;focused=false;private timer?:ReturnType<typeof setInterval>;
 ngOnInit(){if(window.matchMedia?.("(prefers-reduced-motion: reduce)").matches)this.playing.set(false);this.timer=setInterval(()=>{if(this.playing()&&!this.hovered&&!this.focused&&!document.hidden)this.move(1);},7000);}
 ngOnDestroy(){if(this.timer)clearInterval(this.timer);}
 focusLeft(event:FocusEvent){this.focused=!!(event.currentTarget as HTMLElement)?.contains(event.relatedTarget as Node);}

 auth=inject(Auth);register=signal(false);busy=signal(false);show=signal(false);error=signal('');help=signal(false);slide=signal(0);
 accessForm=new FormGroup({correo:new FormControl(this.savedEmail(),{nonNullable:true,validators:[Validators.required,Validators.email]}),password:new FormControl('',{nonNullable:true,validators:[Validators.required]}),nombre:new FormControl('',{nonNullable:true}),remember:new FormControl(!!this.savedEmail(),{nonNullable:true})});
 get correo(){return this.accessForm.controls.correo.value;} set correo(v:string){this.accessForm.controls.correo.setValue(v);}
 get password(){return this.accessForm.controls.password.value;} set password(v:string){this.accessForm.controls.password.setValue(v);}
 get nombre(){return this.accessForm.controls.nombre.value;} set nombre(v:string){this.accessForm.controls.nombre.setValue(v);}
 get remember(){return this.accessForm.controls.remember.value;} set remember(v:boolean){this.accessForm.controls.remember.setValue(v);}
 slides=[
 {label:'Gestión inteligente',title:'Un campus conectado.',accent:'Una solución real.',body:'Reporta, gestiona y da seguimiento a las incidencias de tu institución desde una sola plataforma.',position:'55% center',icon:'building',image:'/login/campus.png'},
 {label:'Comunidad conectada',title:'Las personas primero.',accent:'El equipo, contigo.',body:'Conecta con el equipo de soporte y mantén la conversación en el mismo lugar que tu solicitud.',position:'center',icon:'users',image:'/login/comunidad.png'},
 {label:'Seguimiento de solicitudes',title:'Cada paso cuenta.',accent:'Sigue tu solicitud.',body:'Consulta el estado de tus reportes, las respuestas del equipo y la solución registrada.',position:'center',icon:'board',image:'/login/seguimiento.png'},
 {label:'Un campus más eficiente',title:'Lo que resolvemos hoy.',accent:'Nos ayuda mañana.',body:'Encuentra respuestas en la base de conocimiento y comparte tu valoración de la atención.',position:'center',icon:'book',image:'/login/eficiencia.png'}
 ];
 savedEmail(){try{return localStorage.getItem('campusfix-login-email')||'';}catch{return '';}}
 changeMode(value:boolean){this.register.set(value);this.accessForm.controls.nombre.setValidators(value?[Validators.required,Validators.maxLength(120)]:[]);this.accessForm.controls.password.setValidators(value?[Validators.required,Validators.minLength(12),Validators.maxLength(128),Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).*$/)]:[Validators.required]);this.accessForm.controls.nombre.updateValueAndValidity();this.accessForm.controls.password.updateValueAndValidity();this.error.set('');this.help.set(false);}
 move(n:number){this.slide.update(v=>(v+n+this.slides.length)%this.slides.length);}
 async submit(){if(this.busy())return;this.busy.set(true);this.error.set('');try{await this.auth.login({correo:this.correo.trim(),password:this.password,nombre:this.nombre.trim()},this.register());try{if(this.remember)localStorage.setItem('campusfix-login-email',this.correo.trim());else localStorage.removeItem('campusfix-login-email');}catch{}}catch(e:any){this.error.set(e.error?.title||e.error?.mensaje||'No se pudo iniciar sesión. Inténtalo nuevamente.');}finally{this.busy.set(false);}}
}
