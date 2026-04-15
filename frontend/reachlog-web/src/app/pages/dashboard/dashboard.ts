import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { OutreachService } from '../../core/services/outreach.service';
import { Outreach } from '../../core/models/outreach.model';
import { Router } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  outreaches: Outreach[] = [];
  loading = true;
  confirmDeleteId: string | null = null;

  constructor(
    private outreachService: OutreachService,
    private authService: AuthService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.outreachService.getAll().subscribe({
      next: (data) => { this.outreaches = data; this.loading = false; },
      error: () => this.loading = false
    });
  }

  askDelete(id: string): void {
    this.confirmDeleteId = id;
  }

  cancelDelete(): void {
    this.confirmDeleteId = null;
  }

  confirmDelete(id: string): void {
    this.outreachService.delete(id).subscribe({
      next: () => {
        this.outreaches = this.outreaches.filter(o => o.id !== id);
        this.confirmDeleteId = null;
        this.toastService.success('Outreach deleted.');
      },
      error: () => this.toastService.error('Failed to delete. Try again.')
    });
  }

  editOutreach(o: Outreach): void { this.router.navigate([`/outreach/${o.id}/edit`]); }

  goToAccount(): void { this.router.navigate(['/account']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToJobs(): void { this.router.navigate(['/jobs']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  logout(): void { this.authService.logout(); }
}