import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PrepareService, PrepareResult } from '../../core/services/prepare.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-prepare',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './prepare.html',
  styleUrl: './prepare.scss'
})
export class PrepareComponent {
  jobDescription = '';
  loading = false;
  result: PrepareResult | null = null;
  error: string | null = null;

  constructor(
    private prepareService: PrepareService,
    private router: Router,
    private toastService: ToastService
  ) {}

  prepare(): void {
    if (!this.jobDescription.trim()) return;
    this.loading = true;
    this.result = null;
    this.error = null;

    this.prepareService.prepare(this.jobDescription).subscribe({
      next: (result) => {
        this.result = result;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Failed to generate materials. Try again.';
        this.loading = false;
      }
    });
  }

  copy(text: string): void {
    navigator.clipboard.writeText(text);
    this.toastService.success('Copied to clipboard!');
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}
