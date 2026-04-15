import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AnalyticsService, Analytics } from '../../core/services/analytics.service';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './analytics.html',
  styleUrl: './analytics.scss'
})
export class AnalyticsComponent implements OnInit {
  analytics: Analytics | null = null;
  loading = true;

  readonly statusOrder = ['Sent', 'Opened', 'Replied', 'Interview', 'Rejected', 'Offer'];
  readonly statusColors: Record<string, string> = {
    Sent: '#6b6b7b',
    Opened: '#5b8ef9',
    Replied: '#a78bfa',
    Interview: '#f5a623',
    Rejected: '#f06565',
    Offer: '#3ecf8e'
  };
  readonly channelColors: Record<string, string> = {
    Email: '#5b8ef9',
    LinkedIn: '#e8a030',
    Other: '#6b6b7b'
  };

  constructor(
    private analyticsService: AnalyticsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.analyticsService.getAnalytics().subscribe({
      next: (data) => { this.analytics = data; this.loading = false; },
      error: () => this.loading = false
    });
  }

  getStatusEntries(): { key: string; value: number }[] {
    if (!this.analytics) return [];
    return this.statusOrder
      .filter(s => this.analytics!.byStatus[s] !== undefined)
      .map(s => ({ key: s, value: this.analytics!.byStatus[s] }));
  }

  getChannelEntries(): { key: string; value: number }[] {
    if (!this.analytics) return [];
    return Object.entries(this.analytics.byChannel)
      .map(([key, value]) => ({ key, value }))
      .sort((a, b) => b.value - a.value);
  }

  getMaxStatus(): number {
    if (!this.analytics) return 1;
    return Math.max(...Object.values(this.analytics.byStatus), 1);
  }

  getMaxChannel(): number {
    if (!this.analytics) return 1;
    return Math.max(...Object.values(this.analytics.byChannel), 1);
  }

  getMaxSkill(): number {
    if (!this.analytics?.topMissingSkills.length) return 1;
    return Math.max(...this.analytics.topMissingSkills.map(s => s.count), 1);
  }

  getBarWidth(value: number, max: number): string {
    return `${Math.round((value / max) * 100)}%`;
  }

  getStatusColor(status: string): string {
    return this.statusColors[status] ?? '#6b6b7b';
  }

  getChannelColor(channel: string): string {
    return this.channelColors[channel] ?? '#6b6b7b';
  }

  formatRate(rate: number): string {
    return `${Math.round(rate * 100)}%`;
  }

  formatScore(score: number | null): string {
    if (score === null || score === undefined) return '—';
    return `${score}%`;
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToJobs(): void { this.router.navigate(['/jobs']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAccount(): void { this.router.navigate(['/account']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}