import { TestBed } from '@angular/core/testing';
import { Theme, ThemeToggle } from './theme';
describe('Selector de tema',()=>{
 beforeEach(()=>{localStorage.removeItem('campusfix-theme');document.documentElement.removeAttribute('data-theme');});
 afterEach(()=>{localStorage.removeItem('campusfix-theme');document.documentElement.removeAttribute('data-theme');});
 it('cambia con un clic, actualiza la etiqueta y recuerda la elección',async()=>{const f=TestBed.createComponent(ThemeToggle);await f.whenStable();const b=f.nativeElement.querySelector('button');expect(b.getAttribute('aria-label')).toBe('Cambiar a tema claro');b.click();await f.whenStable();expect(document.documentElement.dataset['theme']).toBe('light');expect(localStorage.getItem('campusfix-theme')).toBe('light');expect(b.getAttribute('aria-label')).toBe('Cambiar a tema oscuro');b.click();await f.whenStable();expect(document.documentElement.dataset['theme']).toBe('dark');});
 it('restaura el tema claro al abrir otra sesión de la aplicación',()=>{localStorage.setItem('campusfix-theme','light');expect(TestBed.inject(Theme).light()).toBe(true);expect(document.documentElement.dataset['theme']).toBe('light');});
});
