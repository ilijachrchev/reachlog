import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toasts = signal<Toast[]>([]);

  success(message: string): void {
    this.add(message, 'success');
  }

  error(message: string): void {
    this.add(message, 'error');
  }

  info(message: string): void {
    this.add(message, 'info');
  }

  dismiss(id: string): void {
    this.toasts.update(t => t.filter(x => x.id !== id));
  }

  private add(message: string, type: Toast['type']): void {
    const id = crypto.randomUUID();
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.dismiss(id), 3000);
  }
}