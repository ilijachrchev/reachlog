import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OutreachService } from '../../core/services/outreach.service';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-create-outreach',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './create-outreach.html',
  styleUrl: './create-outreach.scss'
})
export class CreateOutreachComponent {
  form: FormGroup;
  error: string = '';
  loading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private outreachService: OutreachService,
    private router: Router,
    private toastService: ToastService
  ) {
    this.form = this.fb.group({
      companyName: ['', Validators.required],
      contactName: [''],
      contactEmail: ['', Validators.email],
      role: [''],
      channel: ['Email', Validators.required],
      rawMessage: [''],
      sentAt: [new Date().toISOString().split('T')[0], Validators.required]
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const value = this.form.value;
    this.outreachService.create({
      ...value,
      sentAt: new Date(value.sentAt).toISOString()
    }).subscribe({
      next: () => {
        this.toastService.success('Outreach created successfully!');
        setTimeout(() => this.router.navigate(['/kanban']), 800);
      },
      error: () => {
        this.toastService.error('Failed to create outreach.');
        this.loading = false;
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
  goToAccount(): void { this.router.navigate(['/account']); }
}