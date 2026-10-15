import {Component,inject,signal,computed} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {RouterLink} from '@angular/router';
import {firstValueFrom} from 'rxjs';
import {DialogFocus} from '../reportes/components/dialog-focus';
@Component({selector:'cf-continuity',imports:[CommonModule,FormsModule,RouterLink,DialogFocus],templateUrl:'./continuity.html',styleUrl:'./continuity.css'})
export class Continuity {
 private http=inject(HttpClient);data=signal<any>(null);busy=signal(false);error=signal('');success=signal('');tab=signal('impact');edit=signal<any>(null);confirmation=signal<any>(null);
 synthetic=computed(()=>this.data()?.rooms.some((r:any)=>r.origin==='synthetic-continuity-v1'));
 constructor(){void this.load();}
 async load(){if(this.busy())return;this.busy.set(true);this.error.set('');try{this.data.set(await firstValueFrom(this.http.get('/api/continuity')));}catch(e:any){this.error.set(e.error?.title||'No se pudo cargar continuidad académica.');}finally{this.busy.set(false);}}
 room(id:string){return this.data()?.rooms.find((r:any)=>r.id===id)?.name||'Aula archivada';}
 area(id:string){return this.data()?.areas.find((a:any)=>a.id===id)?.name||'';}
 activity(id:string){return this.data()?.activities.find((a:any)=>a.id===id)?.name||'Actividad archivada';}
 ticket(id:string){return this.data()?.tickets.find((t:any)=>t.id===id);}
 tags(value:string){return value.split(',').map(s=>s.trim()).filter(Boolean);}
 localDate(value:string){const d=new Date(value);return new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,16);}
 open(kind:string,item:any={}){
  this.edit.set({kind,id:item.id||'',...item,areaId:item.areaId||this.data()?.areas[0]?.id||'',roomId:item.roomId||this.data()?.rooms[0]?.id||'',equipmentText:(item.equipment||[]).join(', '),requirementsText:(item.requirements||[]).join(', '),capacity:item.capacity||30,participants:item.participants||25,startsAt:item.startsAt?this.localDate(item.startsAt):'',endsAt:item.endsAt?this.localDate(item.endsAt):'',campus:item.campus||'Campus Central',outcome:'continuada',actualMinutes:0,note:''});
 }
 async save(){
  const form={...this.edit()};
  if(form.kind==='room.save')form.equipment=this.tags(form.equipmentText);
  if(form.kind==='activity.save'){
   if(!form.startsAt||!form.endsAt){this.error.set('Completa el horario de la actividad.');return;}
   form.startsAt=new Date(form.startsAt).toISOString();form.endsAt=new Date(form.endsAt).toISOString();form.requirements=this.tags(form.requirementsText);
  }
  await this.send(form);
 }
 ask(command:any,message:string){this.confirmation.set({command,message});}
 async send(command:any){if(this.busy())return;this.busy.set(true);this.error.set('');this.success.set('');try{this.data.set(await firstValueFrom(this.http.post('/api/continuity',{...command,version:this.data().version})));this.edit.set(null);this.confirmation.set(null);this.success.set('Cambio guardado. El impacto y la disponibilidad se han actualizado.');}catch(e:any){this.error.set(e.error?.title||'No se pudo guardar. Actualiza antes de reintentar.');}finally{this.busy.set(false);}}
 canOutcome(d:any){const a=this.data()?.activities.find((a:any)=>a.id===d.activityId);return d.status==='approved'&&!d.outcome&&a&&Date.parse(a.startsAt)<=Date.now();}
 canCancel(d:any){const a=this.data()?.activities.find((a:any)=>a.id===d.activityId);return d.status==='approved'&&!d.outcome&&a&&Date.parse(a.startsAt)>Date.now();}
 hasDecision(id:string){return this.data()?.decisions.some((d:any)=>d.activityId===id&&d.status==='approved');}
 linkTickets(roomId:string){const room=this.data()?.rooms.find((r:any)=>r.id===roomId);return this.data()?.tickets.filter((t:any)=>t.areaId===room?.areaId)||[];}
}
