import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ScraperService, ScrapedJob } from '../../core/services/scraper.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-jobs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './jobs.html',
  styleUrl: './jobs.scss'
})
export class JobsComponent implements OnInit {
  jobs: ScrapedJob[] = [];
  loading = true;
  running = false;
  filters = { jobType: '', remoteOnly: false, minScore: null as number | null };
  expandedJobId: string | null = null;
  importingIds = new Set<string>();

  constructor(
    private scraperService: ScraperService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.loading = true;
    const filters: { jobType?: string; remoteOnly?: boolean; minScore?: number } = {};
    if (this.filters.jobType) filters.jobType = this.filters.jobType;
    if (this.filters.remoteOnly) filters.remoteOnly = true;
    if (this.filters.minScore !== null) filters.minScore = this.filters.minScore;

    this.scraperService.getJobs(filters).subscribe({
      next: (data) => { this.jobs = data; this.loading = false; },
      error: () => this.loading = false
    });
  }

  runScraper(): void {
    this.running = true;
    this.scraperService.runScraper().subscribe({
      next: (result) => {
        this.toastService.success(`Found ${result.totalFound} jobs in your feed!`);
        this.loadJobs();
        this.running = false;
      },
      error: () => {
        this.toastService.error('Scraper failed. Try again.');
        this.running = false;
      }
    });
  }

  toggleExpand(id: string): void {
    this.expandedJobId = this.expandedJobId === id ? null : id;
  }

  importJob(job: ScrapedJob): void {
    this.importingIds.add(job.id);
    this.scraperService.importJob(job.id).subscribe({
      next: (updated) => {
        const idx = this.jobs.findIndex(j => j.id === job.id);
        if (idx !== -1) this.jobs[idx] = updated;
        this.importingIds.delete(job.id);
        this.toastService.success(`${job.company} added to Kanban!`);
      },
      error: () => {
        this.importingIds.delete(job.id);
        this.toastService.error('Import failed. Try again.');
      }
    });
  }

  applyFilters(): void {
    this.loadJobs();
  }

  isImporting(id: string): boolean {
    return this.importingIds.has(id);
  }

  getScoreBadgeClass(score: number | null): string {
    if (score === null || score === undefined) return 'score-none';
    if (score >= 75) return 'score-high';
    if (score >= 50) return 'score-mid';
    return 'score-low';
  }

  getScoreLabel(score: number | null): string {
    if (score === null || score === undefined) return '—';
    return `${score}%`;
  }

  getDaysSince(dateStr: string | null): string {
    if (!dateStr) return '';
    const diff = Math.floor((Date.now() - new Date(dateStr).getTime()) / 86400000);
    if (diff === 0) return 'today';
    if (diff === 1) return '1d ago';
    return `${diff}d ago`;
  }

  getCountryFlag(country: string): string {
    if (!country) return '';
    const lower = country.toLowerCase();
    if (lower.includes('sloven')) return '🇸🇮';
    if (lower.includes('austria') || lower.includes('wien') || lower.includes('vienna')) return '🇦🇹';
    if (lower.includes('german')) return '🇩🇪';
    if (lower.includes('italy') || lower.includes('italia')) return '🇮🇹';
    if (lower.includes('croatia') || lower.includes('hrvatska')) return '🇭🇷';
    if (lower.includes('united states') || lower.includes('usa')) return '🇺🇸';
    if (lower.includes('united kingdom') || lower.includes('uk')) return '🇬🇧';
    if (lower.includes('netherlands')) return '🇳🇱';
    if (lower.includes('france')) return '🇫🇷';
    if (lower.includes('switzerland')) return '🇨🇭';
    if (lower.includes('remote')) return '🌍';
    return '';
  }

  getBoardBadgeClass(board: string): string {
    if (board === 'LinkedIn') return 'board-linkedin';
    if (board === 'Indeed') return 'board-indeed';
    return 'board-other';
  }

  getTypeBadgeClass(jobType: string | null): string {
    if (jobType === 'Internship') return 'type-internship';
    if (jobType === 'Junior') return 'type-junior';
    return 'type-other';
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}
