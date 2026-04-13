import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem
} from '@angular/cdk/drag-drop';
import { OutreachService } from '../../core/services/outreach.service';
import { NudgeService, Nudge } from '../../core/services/nudge.service';
import { Outreach } from '../../core/models/outreach.model';
import { ToastService } from '../../core/services/toast.service';

export const STATUSES = ['Sent', 'Opened', 'Replied', 'Interview', 'Rejected', 'Offer'] as const;
export type Status = typeof STATUSES[number];

@Component({
  selector: 'app-kanban',
  standalone: true,
  imports: [CommonModule, DragDropModule],
  templateUrl: './kanban.html',
  styleUrl: './kanban.scss'
})
export class KanbanComponent implements OnInit {
  statuses = [...STATUSES];
  columns: Record<string, Outreach[]> = {};
  loading = true;
  connectedLists: string[] = [];
  scoringIds = new Set<string>();

  nudges: Nudge[] = [];
  nudgesExpanded = true;

  selectedOutreach: Outreach | null = null;

  constructor(
    private outreachService: OutreachService,
    private nudgeService: NudgeService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.statuses.forEach(s => this.columns[s] = []);
    this.connectedLists = this.statuses.map(s => `list-${s}`);

    this.outreachService.getAll().subscribe({
      next: (data) => {
        data.forEach(o => {
          const col = this.columns[o.status];
          if (col) col.push(o);
          else this.columns['Sent'].push(o);
        });
        this.loading = false;
      },
      error: () => this.loading = false
    });

    this.nudgeService.getNudges().subscribe({
      next: (data) => this.nudges = data,
      error: () => this.nudges = []
    });
  }

  toggleNudges(): void {
    this.nudgesExpanded = !this.nudgesExpanded;
  }

  dismissNudge(outreachId: string): void {
    this.nudges = this.nudges.filter(n => n.outreachId !== outreachId);
  }

  openDetail(o: Outreach): void {
    this.selectedOutreach = o;
  }

  closeDetail(): void {
    this.selectedOutreach = null;
  }

  getListId(status: string): string {
    return `list-${status}`;
  }

  drop(event: CdkDragDrop<Outreach[]>, targetStatus: string): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    const item = event.previousContainer.data[event.previousIndex];
    const previousStatus = item.status;

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );

    item.status = targetStatus;

    this.outreachService.updateStatus(item.id, { status: targetStatus }).subscribe({
      error: () => {
        transferArrayItem(
          event.container.data,
          event.previousContainer.data,
          event.currentIndex,
          event.previousIndex
        );
        item.status = previousStatus;
      }
    });
  }

  scoreOutreach(event: MouseEvent, o: Outreach): void {
    event.stopPropagation();
    if (this.scoringIds.has(o.id)) return;
    this.scoringIds.add(o.id);

    this.outreachService.score(o.id).subscribe({
      next: (result) => {
        o.matchScore = result.matchScore;
        o.missingSkills = result.missingSkills;
        this.scoringIds.delete(o.id);
        this.toastService.success(`${o.companyName} scored ${result.matchScore}%!`);
      },
      error: () => {
        this.scoringIds.delete(o.id);
        this.toastService.error(`Scoring failed. Try again!`);
      }
    });
  }

  isScoring(id: string): boolean {
    return this.scoringIds.has(id);
  }

  getChannelClass(channel: string): string {
    const c = channel?.toLowerCase();
    if (c === 'email') return 'channel-email';
    if (c === 'linkedin') return 'channel-linkedin';
    return 'channel-other';
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

  getDaysSince(sentAt: string): string {
    if (!sentAt) return '';
    const diff = Math.floor((Date.now() - new Date(sentAt).getTime()) / 86400000);
    if (diff === 0) return 'today';
    if (diff === 1) return '1d ago';
    return `${diff}d ago`;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric'
    });
  }

  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
}