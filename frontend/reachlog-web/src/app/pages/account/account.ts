import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ScraperService, UserJobPreference } from '../../core/services/scraper.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './account.html',
  styleUrl: './account.scss'
})
export class AccountComponent implements OnInit {
  preference: UserJobPreference = { country: '', city: '', jobType: 'Internship', keywords: '' };
  loading = true;
  saving = false;

  constructor(
    private scraperService: ScraperService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.scraperService.getPreference().subscribe({
      next: (pref) => {
        if (pref) this.preference = { ...pref };
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  save(): void {
    this.saving = true;
    this.scraperService.savePreference(this.preference).subscribe({
      next: () => {
        this.toastService.success('Preferences saved.');
        this.saving = false;
      },
      error: () => {
        this.toastService.error('Failed to save. Try again.');
        this.saving = false;
      }
    });
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToJobs(): void { this.router.navigate(['/jobs']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}
