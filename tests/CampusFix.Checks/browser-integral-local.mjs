import {createRequire} from 'node:module';
import {readFile,writeFile} from 'node:fs/promises';
import {join} from 'node:path';
import assert from 'node:assert/strict';
const require=createRequire(import.meta.url);
const {chromium}=require(process.env.CAMPUSFIX_PLAYWRIGHT||'playwright');
const base=process.env.CAMPUSFIX_LOCAL_URL||'http://localhost:4200';
assert.ok(['localhost','127.0.0.1'].includes(new URL(base).hostname));
const privateDir=process.env.CAMPUSFIX_PRIVATE_DIR||join(process.env.LOCALAPPDATA,'CampusFix','integral-2026-10-10');
const access=JSON.parse(await readFile(join(privateDir,'accesos-locales.json'),'utf8'));
const inventory=JSON.parse(await readFile(join(privateDir,'resultado-actual.json'),'utf8'));
const progressPath=join(privateDir,'recorrido-local.json');let progress={};try{progress=JSON.parse(await readFile(progressPath,'utf8'));}catch{}
let checks=0;const errors=[];const check=s=>{checks++;console.log('PASS local UI '+s);};
const browser=await chromium.launch({headless:true,channel:'msedge'});
try{
 const login=async credential=>{assert.ok(credential?.password,'Credential must be a newly provisioned account');const context=await browser.newContext({viewport:{width:1440,height:1000}});const p=await context.newPage();p.on('pageerror',e=>errors.push(e.message));await p.goto(base+'/login');await p.locator('#access-email').fill(credential.id);await p.locator('input[name="password"]').fill(credential.password);await p.locator('form button[type="submit"]').click();await p.waitForURL('**/tablero');return p;};
 const api=(p,path,method='GET',body)=>p.evaluate(async({path,method,body})=>{const r=await fetch('/api/'+path,{method,headers:{Authorization:'Bearer '+sessionStorage.getItem('campusfix-token'),'Content-Type':'application/json'},...(body?{body:JSON.stringify(body)}:{})});return {status:r.status,data:await r.json()};},{path,method,body});
 const open=async(p,id)=>{await p.goto(base+'/tablero?ticket='+id);await p.getByRole('dialog').waitFor();return p.getByRole('dialog');};
 // Ordinary password login for every newly created administrator; never forge sessions.
 let audited=0;const unavailable=[];
 for(const area of inventory.areas){const credential=access.find(c=>c.id===area.administratorIds[0]);if(!credential){unavailable.push(area.area);continue;}const p=await login(credential);const response=await api(p,'data');assert.equal(response.status,200);const d=response.data;assert.ok(d.tickets.every(t=>d.user.permissions.managedAreas.includes(t.responsibleAreaId)));const notices=d.notifications.filter(n=>n.eventType==='created'&&area.ticketIds.includes(n.ticketId));assert.equal(notices.length,5);assert.ok(notices.every(n=>!n.read));const other=inventory.areas.find(a=>a.areaId!==area.areaId);assert.equal((await api(p,'reportes/'+other.ticketIds[0])).status,404);audited++;await p.context().close();}
 check(`${audited} administradores: cinco eventos sin leer, ámbito de tickets y rechazo por URL`);
 const direction=inventory.areas.find(a=>a.area==='Dirección Académica');const admin=await login(access.find(c=>c.id===direction.administratorIds[0]));const tech=await login(access.find(c=>c.id===direction.responsibleId));
 const library=inventory.areas.find(a=>a.area==='Biblioteca');const credential=access.find(c=>c.role==='Solicitante'&&c.areaId===library.areaId);let requester;
 for(let i=0;i<3;i++){const p=await login(credential);assert.equal(new URL(p.url()).pathname,'/tablero');if(i<2)await p.context().close();else requester=p;}
 check('tres inicios normales abren tablero con pertenencia persistente');
 await requester.getByRole('button',{name:'Nuevo reporte',exact:true}).click();let d=requester.getByRole('dialog');const wellbeing=inventory.areas.find(a=>a.area.toLowerCase().includes('bienestar'));const ti=inventory.areas.find(a=>a.area.includes('Tecnolog'));
 await d.locator('[name="attentionArea"]').selectOption(wellbeing.areaId);const options=await d.locator('[name="service"] option').allTextContents();assert.ok(options.includes('Orientación estudiantil'));assert.ok(!options.includes('Soporte tecnológico'));await d.locator('[name="service"]').selectOption(wellbeing.serviceIds[0]);await d.locator('[name="attentionArea"]').selectOption(ti.areaId);assert.equal(await d.locator('[name="service"]').inputValue(),'');assert.equal(await d.locator('[name="category"]').inputValue(),'');check('Biblioteca → Bienestar filtra catálogo y cambio a TI limpia selección');
 requester.once('dialog',dialog=>dialog.accept());await d.getByRole('button',{name:'Cancelar',exact:true}).click();
 await admin.goto(base+'/configuracion');await admin.getByRole('button',{name:/^Servicios/}).click();assert.equal(await admin.getByLabel('Área que estás administrando').inputValue(),direction.areaId);const adminData=(await api(admin,'data')).data;const ownService=adminData.config.find(c=>c.id===direction.serviceIds[0]);const foreignService=inventory.after.config.find(c=>c.kind==='servicios'&&c.responsibleAreaId===ti.areaId);assert.equal((await api(admin,'config','POST',foreignService)).status,403);check('configuración propia y rechazo de modificación de servicio ajeno');
 // An additional authentic UI workflow preserves the initial five pending requests per area.
 if(!progress.ticketId){await requester.goto(base+'/tablero');await requester.getByRole('button',{name:'Nuevo reporte',exact:true}).click();d=requester.getByRole('dialog');await d.locator('[name="attentionArea"]').selectOption(direction.areaId);await d.locator('[name="service"]').selectOption(direction.serviceIds[0]);await d.locator('[name="category"]').selectOption(direction.categoryIds[0]);await d.locator('[name="problem"]').fill('Coordinación de una sesión informativa académica');await d.locator('[name="location"]').fill('Atención en línea');await d.locator('[name="description"]').fill('Solicito información para coordinar una sesión sobre planificación académica y conocer los horarios disponibles.');const response=requester.waitForResponse(r=>r.url().endsWith('/api/reportes')&&r.request().method()==='POST');await d.getByRole('button',{name:'Guardar reporte'}).click();const r=await response;assert.equal(r.status(),201);progress.ticketId=(await r.json()).id;await writeFile(progressPath,JSON.stringify(progress));}
 let ticket=(await api(admin,'reportes/'+progress.ticketId)).data;
 if(!ticket.tecnicoId){let ad=await open(admin,progress.ticketId);const assigned=admin.waitForResponse(r=>r.url().endsWith('/asignar'));await ad.getByLabel('Responsable asignado').selectOption(direction.responsibleId);assert.equal((await assigned).status(),200);}
 let td=await open(tech,progress.ticketId);ticket=(await api(tech,'reportes/'+progress.ticketId)).data;
 if(ticket.estado===1){await td.getByRole('button',{name:'Pasar a En revisión'}).click();await td.getByRole('button',{name:'Pasar a En reparación'}).waitFor();}
 ticket=(await api(tech,'reportes/'+progress.ticketId)).data;if(ticket.estado===2){await td.getByRole('button',{name:'Pasar a En reparación'}).click();await td.getByRole('button',{name:'Guardar y solucionar'}).waitFor();}
 ticket=(await api(tech,'reportes/'+progress.ticketId)).data;if(ticket.estado===3){await td.locator('textarea[placeholder^="Describe las acciones"]').fill('Se revisaron los horarios de atención y se facilitó el procedimiento para coordinar la sesión informativa.');await td.getByRole('button',{name:'Guardar y solucionar'}).click();await td.waitFor({state:'hidden'});}
 d=await open(requester,progress.ticketId);await d.locator('.ticket-meta').getByText('Solucionado',{exact:true}).waitFor();await requester.reload();await requester.getByRole('dialog').locator('.ticket-meta').getByText('Solucionado',{exact:true}).waitFor();check('solicitud adicional: crear, asignar, atender, solucionar y consultar tras recargar');
 const final=(await api(admin,'data')).data;assert.equal(final.tickets.filter(t=>direction.ticketIds.includes(t.id)&&t.estado===1).length,5);check('las cinco solicitudes iniciales permanecen pendientes');
 const start=Date.now();await api(admin,'data');const elapsed=Date.now()-start;assert.ok(elapsed<5000);check('actualización del área en '+elapsed+' ms');
 assert.deepEqual(errors,[]);check('sin errores JavaScript');progress={...progress,checks,unavailableExistingAccounts:unavailable,completedAt:new Date().toISOString(),dataMilliseconds:elapsed};await writeFile(progressPath,JSON.stringify(progress,null,2));console.log(`SUCCESS ${checks} checks; existing accounts requiring owner login: ${unavailable.join(', ')}. No credentials logged.`);
}finally{await browser.close();}

