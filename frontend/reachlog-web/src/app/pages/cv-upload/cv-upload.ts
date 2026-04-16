import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { CvService, CvInfo } from '../../core/services/cv.service';
import { OutreachService } from '../../core/services/outreach.service';
import { ToastService } from '../../core/services/toast.service';
import { CvBlock } from '../../core/models/cv.model';

@Component({
  selector: 'app-cv-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cv-upload.html',
  styleUrl: './cv-upload.scss'
})
export class CvUploadComponent implements OnInit, OnDestroy {
  cvInfo: CvInfo | null = null;
  loading = true;
  uploading = false;
  error: string | null = null;
  success: string | null = null;
  selectedFile: File | null = null;
  dragOver = false;
  filePreviewUrl: SafeResourceUrl | null = null;
  private rawPreviewUrl: string | null = null;

  blocks: CvBlock[] = [];
  blocksLoading = false;
  suggestAllLoading = false;
  jobDescription = '';
  useJobSelector = false;
  interestedOutreaches: any[] = [];
  selectedOutreachId: string | null = null;

  constructor(
    private cvService: CvService,
    private outreachService: OutreachService,
    private router: Router,
    private toastService: ToastService,
    private http: HttpClient,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.cvService.get().subscribe({
      next: (data) => {
        this.cvInfo = data;
        this.loading = false;
        this.loadPreview();
        this.loadBlocks();
      },
      error: () => { this.cvInfo = null; this.loading = false; }
    });
    this.loadInterestedOutreaches();
  }

  ngOnDestroy(): void {
    if (this.rawPreviewUrl) URL.revokeObjectURL(this.rawPreviewUrl);
  }

  private loadPreview(): void {
    this.http.get(this.cvService.getFileUrl(), { responseType: 'blob' }).subscribe({
      next: (blob) => {
        if (this.rawPreviewUrl) URL.revokeObjectURL(this.rawPreviewUrl);
        this.rawPreviewUrl = URL.createObjectURL(blob);
        this.filePreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.rawPreviewUrl);
      }
    });
  }

  loadBlocks(): void {
    this.blocksLoading = true;
    this.cvService.getBlocks().subscribe({
      next: (blocks) => {
        this.blocks = blocks;
        this.blocksLoading = false;
      },
      error: () => { this.blocksLoading = false; }
    });
  }

  loadInterestedOutreaches(): void {
    this.outreachService.getAll().subscribe({
      next: (outreaches) => {
        this.interestedOutreaches = outreaches.filter(o => o.status === 'Interested');
      }
    });
  }

  suggestAll(): void {
    this.suggestAllLoading = true;
    this.cvService.getSuggestions(this.blocks, this.getJobDescription()).subscribe({
      next: (suggestions) => {
        for (const s of suggestions) {
          const block = this.blocks.find(b => b.id === s.blockId);
          if (block) {
            block.suggestion = s.suggestedContent;
            block.showDiff = true;
          }
        }
        this.suggestAllLoading = false;
      },
      error: () => { this.suggestAllLoading = false; }
    });
  }

  suggestBlock(block: CvBlock): void {
    block.loading = true;
    this.cvService.getSuggestions([block], this.getJobDescription()).subscribe({
      next: (suggestions) => {
        const s = suggestions.find(r => r.blockId === block.id);
        if (s) {
          block.suggestion = s.suggestedContent;
          block.showDiff = true;
        }
        block.loading = false;
      },
      error: () => { block.loading = false; }
    });
  }

  acceptSuggestion(block: CvBlock): void {
    block.content = block.suggestion!;
    block.suggestion = undefined;
    block.showDiff = false;
  }

  rejectSuggestion(block: CvBlock): void {
    block.suggestion = undefined;
    block.showDiff = false;
  }

  exportDocx(): void {
    this.cvService.exportCvAsDocx(this.blocks).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'cv-edited.docx';
        a.click();
        URL.revokeObjectURL(url);
      }
    });
  }

  exportPdf(): void {
    this.cvService.exportCvAsPdf(this.blocks).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'cv-edited.pdf';
        a.click();
        URL.revokeObjectURL(url);
      }
    });
  }

  getJobDescription(): string {
    if (this.useJobSelector && this.selectedOutreachId !== null) {
      const outreach = this.interestedOutreaches.find(o => o.id === this.selectedOutreachId);
      if (outreach) return `${outreach.role} at ${outreach.companyName}`;
    }
    return this.jobDescription;
  }

  isPdf(): boolean {
    return this.cvInfo?.contentType === 'application/pdf';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.setFile(input.files[0]);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = true;
  }

  onDragLeave(): void {
    this.dragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
    const file = event.dataTransfer?.files[0];
    if (file) this.setFile(file);
  }

  setFile(file: File): void {
    const allowed = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    if (!allowed.includes(file.type)) {
      this.error = 'Only PDF and Word (.docx) files are supported.';
      return;
    }
    this.selectedFile = file;
    this.error = null;
  }

  upload(): void {
    if (!this.selectedFile) return;
    this.uploading = true;
    this.error = null;
    this.success = null;

    this.cvService.upload(this.selectedFile).subscribe({
      next: (data) => {
        this.cvInfo = data;
        this.uploading = false;
        this.toastService.success('CV uploaded successfully.');
        this.selectedFile = null;
        this.loadPreview();
        this.loadBlocks();
      },
      error: () => {
        this.toastService.error('Upload failed. Please try again.');
        this.uploading = false;
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric'
    });
  }

  formatCount(count: number): string {
    return count.toLocaleString();
  }

  goToKanban(): void { this.router.navigate(['/kanban']); }
  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToInbox(): void { this.router.navigate(['/inbox']); }
  goToPrepare(): void { this.router.navigate(['/prepare']); }
  goToJobs(): void { this.router.navigate(['/jobs']); }
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}
