import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CvService, CvInfo } from '../../core/services/cv.service';

@Component({
  selector: 'app-cv-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv-upload.html',
  styleUrl: './cv-upload.scss'
})
export class CvUploadComponent implements OnInit {
  cvInfo: CvInfo | null = null;
  loading = true;
  uploading = false;
  error: string | null = null;
  success: string | null = null;
  selectedFile: File | null = null;
  dragOver = false;

  constructor(private cvService: CvService, private router: Router) {}

  ngOnInit(): void {
    this.cvService.get().subscribe({
      next: (data) => { this.cvInfo = data; this.loading = false; },
      error: () => { this.cvInfo = null; this.loading = false; }
    });
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
        this.success = 'CV uploaded successfully.';
        this.selectedFile = null;
      },
      error: () => {
        this.error = 'Upload failed. Please try again.';
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
  goToAnalytics(): void { this.router.navigate(['/analytics']); }
  goToNew(): void { this.router.navigate(['/outreach/new']); }
}