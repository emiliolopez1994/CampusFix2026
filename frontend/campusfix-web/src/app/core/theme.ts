import { DOCUMENT } from '@angular/common';
import { Component, Injectable, inject, signal } from '@angular/core';
import { Icon } from './icon';
@Injectable({providedIn:'root'})
export class Theme {
 private document=inject(DOCUMENT);
 readonly light=signal(false);
 constructor(){let saved:string|null=null;try{saved=this.document.defaultView?.localStorage.getItem('campusfix-theme')??null;}catch{}this.apply(saved==='light');}
 toggle(){this.apply(!this.light());try{this.document.defaultView?.localStorage.setItem('campusfix-theme',this.light()?'light':'dark');}catch{}}
 private apply(light:boolean){this.light.set(light);this.document.documentElement.setAttribute('data-theme',light?'light':'dark');}
}
@Component({selector:'cf-theme-toggle',imports:[Icon],template:`<button type="button" class="icon-button theme-toggle" (click)="theme.toggle()" [attr.aria-label]="theme.light()?'Cambiar a tema oscuro':'Cambiar a tema claro'" [title]="theme.light()?'Cambiar a tema oscuro':'Cambiar a tema claro'" [attr.aria-pressed]="theme.light()"><cf-icon [name]="theme.light()?'moon':'sun'"/></button>`})
export class ThemeToggle{theme=inject(Theme);}
