import { createRequire } from 'node:module';
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:net';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';

const require = createRequire(import.meta.url);
const { chromium } = require(process.env.CAMPUSFIX_PLAYWRIGHT || 'playwright');
const connection = process.env.CAMPUSFIX_TEST_POSTGRES || '';
assert.match(connection, /(?:^|;)Host=(?:127\.0\.0\.1|localhost)(?:;|$)/i);
assert.match(connection, /(?:^|;)Database=campusfix_qa[^;]*(?:;|$)/i);
const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const apiDir = resolve(root, 'backend/CampusFix.Api');
const socket = createServer();
await new Promise(resolve => socket.listen(0, '127.0.0.1', resolve));
const port = socket.address().port;
await new Promise(resolve => socket.close(resolve));
const url = `http://127.0.0.1:${port}`;
const api = spawn('dotnet', [resolve(apiDir, 'bin/Debug/net10.0/CampusFix.Api.dll'), '--urls', url], {
  cwd: apiDir, windowsHide: true, stdio: 'ignore',
  env: {...process.env, ASPNETCORE_ENVIRONMENT:'QaTesting', ConnectionStrings__CampusFix:connection, Jwt__Key:randomBytes(64).toString('base64')}
});
let browser;
const check = label => console.log('PASS browser ' + label);
try {
  let ready = false;
  for (let i=0;i<80;i++) {
    try { const res = await fetch(url+'/api/health');if(res.ok){ready=true;break;} } catch {}
    await new Promise(resolve=>setTimeout(resolve,250));
  }
  assert.ok(ready, 'Local API readiness');
  browser = await chromium.launch({headless:true,channel:process.env.CAMPUSFIX_BROWSER_CHANNEL||'msedge'});
  const context = await browser.newContext({viewport:{width:1366,height:900}});
  const page = await context.newPage();
  const errors=[];page.on('pageerror',error=>errors.push(error.message));
  await page.goto(url+'/login');
  await page.getByRole('button',{name:'Registrarse',exact:true}).click();
  await page.locator('#access-name').fill('Solicitante QA navegador');
  await page.locator('input[name="correo"]').fill(`browser-${Date.now()}@example.edu`);
  await page.locator('input[name="password"]').fill('SoloPruebasQA2026');
  await page.locator('form button[type="submit"]').click();
  await page.waitForURL('**/panel');
  check('registro e inicio de sesión');
  await page.goto(url+'/tablero');
  await page.getByRole('button',{name:'Nuevo reporte',exact:true}).click();
  const dialog = page.getByRole('dialog');
  await dialog.locator('input[name="problem"]').fill('Proyector HDMI QA navegador');
  await dialog.locator('input[name="location"]').fill('Aula QA sintética');
  await page.locator('.modal-backdrop').click({position:{x:5,y:5}});
  assert.ok(await dialog.isVisible());
  page.once('dialog',d=>d.dismiss());
  await dialog.press('Escape');
  assert.ok(await dialog.isVisible());
  assert.equal(await dialog.getByText('Guías relacionadas',{exact:true}).count(),0);
  check('fondo y Escape conservan datos; guías ocultas');
  await page.reload();
  await page.getByRole('button',{name:'Nuevo reporte',exact:true}).click();
  assert.equal(await dialog.locator('input[name="problem"]').inputValue(),'Proyector HDMI QA navegador');
  assert.equal(await dialog.locator('input[name="location"]').inputValue(),'Aula QA sintética');
  check('borrador recuperado tras recarga');
  const created = page.waitForResponse(r=>r.url()===url+'/api/reportes'&&r.request().method()==='POST');
  await dialog.getByRole('button',{name:'Guardar reporte'}).click();
  const response = await created;assert.equal(response.status(),201);
  const ticket=await response.json();
  await page.waitForFunction(()=>document.querySelector('.ticket-meta')?.textContent?.includes('CF-'));
  assert.equal(ticket.problema,'Proyector HDMI QA navegador');
  assert.equal(ticket.estado,1);
  check('Angular → HTTP 201 → PostgreSQL crea ticket');
  await dialog.locator('textarea[placeholder="Escribe una respuesta…"]').fill('Seguimiento QA del solicitante');
  const reply = page.waitForResponse(r=>r.url().endsWith(`/reportes/${ticket.id}/mensajes`)&&r.request().method()==='POST');
  await dialog.getByRole('button',{name:'Enviar',exact:true}).click();
  assert.equal((await reply).status(),200);
  await dialog.getByText('Seguimiento QA del solicitante',{exact:true}).waitFor();
  await page.goto(url+`/tablero?ticket=${ticket.id}`);
  await page.getByText('Seguimiento QA del solicitante',{exact:true}).waitFor();
  check('respuesta persistente después de navegar');
  await page.setViewportSize({width:390,height:844});
  assert.ok(await page.getByRole('dialog').isVisible());
  // Resizing triggers the existing sidebar margin transition; inspect its settled layout.
  try { await page.waitForFunction(()=>document.documentElement.scrollWidth<=window.innerWidth,{},{timeout:3000}); }
  catch (error) {
    console.log(await page.evaluate(()=>[...document.querySelectorAll('body *')].filter(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.right>innerWidth+1&&getComputedStyle(e).position!=='fixed';}).slice(0,15).map(e=>({tag:e.tagName,class:e.className,width:e.getBoundingClientRect().width,right:e.getBoundingClientRect().right}))));
    await page.screenshot({path:resolve(root,'tests/CampusFix.Checks/obj/qa-mobile.png')});
    throw error;
  }
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth),false);
  check('diálogo móvil sin desbordamiento horizontal');
  assert.deepEqual(errors,[]);
  check('sin errores JavaScript de página');
  console.log('SUCCESS: 7 browser checks; isolated synthetic data; no production writes.');
} finally {
  if(browser) await browser.close();
  api.kill();
}
