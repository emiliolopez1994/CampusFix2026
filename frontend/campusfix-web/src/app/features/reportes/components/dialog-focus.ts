import {
  AfterViewInit,
  Directive,
  ElementRef,
  HostListener,
  inject,
  OnDestroy,
} from '@angular/core';

/** Mantiene el foco del teclado dentro del diálogo y lo devuelve al cerrar. */
@Directive({ selector: '[appDialogFocus]' })
export class DialogFocus implements AfterViewInit, OnDestroy {
  private element = inject<ElementRef<HTMLElement>>(ElementRef);
  private previous = document.activeElement as HTMLElement | null;
  private previousOverflow = document.body.style.overflow;
  private elements() {
    return Array.from(
      this.element.nativeElement.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href]',
      ),
    );
  }
  ngAfterViewInit() {
    document.body.style.overflow = 'hidden';
    this.elements()[0]?.focus({ preventScroll: true });
  }
  ngOnDestroy() {
    document.body.style.overflow = this.previousOverflow;
    if(this.previous?.isConnected)this.previous.focus({ preventScroll: true });
  }
  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent) {
    if (event.key !== 'Tab') return;
    const elements = this.elements();
    const first = elements[0],
      last = elements.at(-1);
    if (!first) {
      event.preventDefault();
      return;
    }
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last?.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
}
