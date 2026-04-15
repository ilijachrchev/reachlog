import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { OutreachService } from '../../core/services/outreach.service';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-edit-outreach',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './edit-outreach.html',
  styleUrl: './edit-outreach.scss'
})
export class EditOutreachComponent implements OnInit {
  form: FormGroup;
  error: string = '';
  loading: boolean = false;
  loadingData: boolean = true;
  outreachId: string = '';

  constructor(
    private fb: FormBuilder,
    private outreachService: OutreachService,
    private router: Router,
    private route: ActivatedRoute,
    private toastService: ToastService
  ) {
    this.form = this.fb.group({
      companyName: ['', Validators.required],
      contactName: [''],
      contactEmail: ['', Validators.email],
      role: [''],
      channel: ['Email', Validators.required],
      rawMessage: [''],
      notes: [''],
      sentAt: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.outreachId = this.route.snapshot.paramMap.get('id')!;
    this.outreachService.getById(this.outreachId).subscribe({
      next: (o) => {
        this.form.patchValue({
          companyName: o.companyName,
          contactName: o.contactName,
          contactEmail: o.contactEmail,
          role: o.role,
          channel: o.channel,
          rawMessage: o.rawMessage,
          notes: o.notes,
          sentAt: new Date(o.sentAt).toISOString().split('T')[0]
        });
        this.loadingData = false;
      },
      error: () => {
        this.toastService.error('Failed to load outreach.');
        this.router.navigate(['/kanban']);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const value = this.form.value;
    this.outreachService.update(this.outreachId, {
      ...value,
      sentAt: new Date(value.sentAt).toISOString()
    }).subscribe({
      next: () => {
        this.toastService.success('Outreach updated successfully!');
        setTimeout(() => this.router.navigate(['/kanban']), 800);
      },
      error: () => {
        this.toastService.error('Failed to update outreach.');
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