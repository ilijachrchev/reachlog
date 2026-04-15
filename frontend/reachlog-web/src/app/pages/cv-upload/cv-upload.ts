import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { CvService, CvInfo } from '../../core/services/cv.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-cv-upload',
  standalone: true,
  imports: [CommonModule],
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

  constructor(
    private cvService: CvService,
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
      },
      error: () => { this.cvInfo = null; this.loading = false; }
    });
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
