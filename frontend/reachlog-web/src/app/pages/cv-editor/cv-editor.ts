import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CvService } from '../../core/services/cv.service';
import { OutreachService } from '../../core/services/outreach.service';
import { ToastService } from '../../core/services/toast.service';
import { CvSuggestion, CvImproveRequest } from '../../core/models/cv.model';
import { Outreach } from '../../core/models/outreach.model';
import { CvDocumentEditorComponent } from '../../shared/components/cv-document-editor/cv-document-editor';
import { SuggestionPanelComponent } from '../../shared/components/suggestion-panel/suggestion-panel';
import { SuggestionPopoverComponent } from '../../shared/components/suggestion-popover/suggestion-popover';
import { ModifyDialogComponent } from '../../shared/components/modify-dialog/modify-dialog';

type JobContextTab = 'paste' | 'outreach';

@Component({
  selector: 'app-cv-editor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CvDocumentEditorComponent,
    SuggestionPanelComponent,
    SuggestionPopoverComponent,
    ModifyDialogComponent,
  ],
  templateUrl: './cv-editor.html',
  styleUrl: './cv-editor.scss'
})
export class CvEditorComponent implements OnInit {
  @ViewChild(CvDocumentEditorComponent) cvEditor!: CvDocumentEditorComponent;

  cvText = '';
  loading = true;

  suggestions: CvSuggestion[] = [];
  hasImproved = false;
  improving = false;
  editorEditable = false;

  jobContextTab: JobContextTab = 'paste';
  jobDescription = '';
  interestedOutreaches: Outreach[] = [];
  selectedOutreachId: string | null = null;

  activeSuggestion: CvSuggestion | null = null;
  popoverPosition = { top: 0, left: 0 };

  modifySuggestion: CvSuggestion | null = null;

  exportingDocx = false;
  exportingPdf = false;

  constructor(
    private cvService: CvService,
    private outreachService: OutreachService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.cvService.get().subscribe({
      next: (data) => {
        if (!data.extractedText) {
          this.router.navigate(['/cv']);
          return;
        }
        this.cvText = data.extractedText;
        this.loading = false;
      },
      error: () => {
        this.router.navigate(['/cv']);
      }
    });

    this.outreachService.getAll().subscribe({
      next: (list) => {
        this.interestedOutreaches = list.filter(o => o.status === 'Interested');
      }
    });
  }

  get pendingCount(): number {
    return this.suggestions.filter(s => s.status === 'pending').length;
  }

  onImproveClicked(): void {
    const request: CvImproveRequest = {};

    if (this.jobContextTab === 'outreach' && this.selectedOutreachId) {
      request.outreachId = this.selectedOutreachId;
    } else if (this.jobDescription.trim()) {
      request.jobDescription = this.jobDescription.trim();
    }

    this.improving = true;
    this.suggestions = [];
    this.hasImproved = false;
    this.editorEditable = false;

    this.cvService.improveCv(request).subscribe({
      next: (result) => {
        this.suggestions = result.map(s => ({ ...s, status: 'pending' as const }));
        this.hasImproved = true;
        this.improving = false;
        if (result.length === 0) {
          this.editorEditable = true;
        }
      },
      error: () => {
        this.toastService.error('Failed to get suggestions. Please try again.');
        this.improving = false;
        this.editorEditable = true;
      }
    });
  }

  onAccept(id: string): void {
    this.cvEditor.applySuggestion(id);
    this.removeSuggestion(id);
    this.activeSuggestion = null;
  }

  onReject(id: string): void {
    this.cvEditor.rejectSuggestion(id);
    this.removeSuggestion(id);
    this.activeSuggestion = null;
  }

  onModifyOpen(id: string): void {
    this.modifySuggestion = this.suggestions.find(s => s.id === id) ?? null;
    this.activeSuggestion = null;
  }

  onModifySave(event: { id: string; text: string }): void {
    this.cvEditor.applySuggestion(event.id, event.text);
    this.removeSuggestion(event.id);
    this.modifySuggestion = null;
  }

  onModifyCancel(): void {
    this.modifySuggestion = null;
  }

  onSuggestionClicked(id: string, event?: MouseEvent): void {
    const suggestion = this.suggestions.find(s => s.id === id);
    if (!suggestion) return;

    this.activeSuggestion = suggestion;

    const el = document.querySelector(`[data-suggestion-id="${id}"]`);
    if (el) {
      const rect = el.getBoundingClientRect();
      const popoverWidth = 340;
      const popoverHeight = 220;
      let left = rect.left;
      let top = rect.bottom + 8;

      if (left + popoverWidth > window.innerWidth - 16) {
        left = window.innerWidth - popoverWidth - 16;
      }
      if (top + popoverHeight > window.innerHeight - 16) {
        top = rect.top - popoverHeight - 8;
      }

      this.popoverPosition = { top, left };
    }
  }

  onScrollTo(id: string): void {
    this.cvEditor.scrollToSuggestion(id);
    this.onSuggestionClicked(id);
  }

  onPopoverClose(): void {
    this.activeSuggestion = null;
  }

  private removeSuggestion(id: string): void {
    this.suggestions = this.suggestions.filter(s => s.id !== id);
    if (this.pendingCount === 0 && this.hasImproved) {
      this.editorEditable = true;
    }
  }

  onExportDocx(): void {
    if (this.exportingDocx) return;
    this.exportingDocx = true;
    const text = this.cvEditor.getFullText();
    this.cvService.exportCvAsDocx(text).subscribe({
      next: (blob) => {
        this.triggerDownload(blob, 'cv-edited.docx');
        this.exportingDocx = false;
      },
      error: () => {
        this.toastService.error('Export failed.');
        this.exportingDocx = false;
      }
    });
  }

  onExportPdf(): void {
    if (this.exportingPdf) return;
    this.exportingPdf = true;
    const text = this.cvEditor.getFullText();
    this.cvService.exportCvAsPdf(text).subscribe({
      next: (blob) => {
        this.triggerDownload(blob, 'cv-edited.pdf');
        this.exportingPdf = false;
      },
      error: () => {
        this.toastService.error('Export failed.');
        this.exportingPdf = false;
      }
    });
  }

  private triggerDownload(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }

  goToUpload(): void { this.router.navigate(['/cv']); }
  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToJobs(): void { this.router.navigate(['/jobs']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}
