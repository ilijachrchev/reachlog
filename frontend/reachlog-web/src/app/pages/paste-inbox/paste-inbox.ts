import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ParseService } from '../../core/services/parse.service';
import { OutreachService } from '../../core/services/outreach.service';
import { ParseResult } from '../../core/models/outreach.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-paste-inbox',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './paste-inbox.html',
  styleUrl: './paste-inbox.scss'
})
export class PasteInboxComponent {
  rawText = '';
  isLoading = false;
  isSaving = false;
  parsedResult: ParseResult | null = null;
  error: string | null = null;

  editCompany = '';
  editContact = '';
  editEmail = '';
  editRole = '';
  editChannel = '';
  editSentAt = '';

  constructor(
    private parseService: ParseService,
    private outreachService: OutreachService,
    private router: Router,
    private toastService: ToastService
  ) {}

  onPaste(event: ClipboardEvent) {
    const text = event.clipboardData?.getData('text') ?? '';
    if (text.length > 50) {
      this.rawText = text;
      setTimeout(() => this.runParse(), 0);
    }
  }

  runParse() {
    if (!this.rawText.trim()) return;
    this.isLoading = true;
    this.parsedResult = null;
    this.error = null;

    this.parseService.parse(this.rawText).subscribe({
      next: (result) => {
        this.parsedResult = result;
        this.editCompany = result.companyName ?? '';
        this.editContact = result.contactName ?? '';
        this.editEmail = result.contactEmail ?? '';
        this.editRole = result.role ?? '';
        this.editChannel = result.channel ?? '';
        this.editSentAt = result.sentAt ?? '';
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Failed to parse message. Try again.';
        this.isLoading = false;
      }
    });
  }

  confirmAndSave() {
    this.isSaving = true;
    this.outreachService.create({
      companyName: this.editCompany,
      contactName: this.editContact,
      contactEmail: this.editEmail,
      role: this.editRole,
      channel: this.editChannel,
      sentAt: this.editSentAt || new Date().toISOString(),
      notes: '',
      rawMessage: this.rawText
    }).subscribe({
      next: () => {
        this.toastService.success('Outreach saved successfully!');
        setTimeout(() => this.router.navigate(['/kanban']), 800);
      },
      error: () => {
        this.toastService.error('Failed to save. Try again.');
        this.isSaving = false;
      }
    });
  }

  reset() {
    this.rawText = '';
    this.parsedResult = null;
    this.error = null;
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToJobs(): void { this.router.navigate(['/jobs']); }
  goToCv(): void { this.router.navigate(['/cv']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}