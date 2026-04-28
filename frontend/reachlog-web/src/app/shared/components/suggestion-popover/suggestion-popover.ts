import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CvSuggestion } from '../../../core/models/cv.model';

@Component({
  selector: 'app-suggestion-popover',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './suggestion-popover.html',
  styleUrl: './suggestion-popover.scss'
})
export class SuggestionPopoverComponent {
  @Input() suggestion: CvSuggestion | null = null;
  @Input() position: { top: number; left: number } = { top: 0, left: 0 };

  @Output() accept = new EventEmitter<string>();
  @Output() reject = new EventEmitter<string>();
  @Output() modify = new EventEmitter<string>();
  @Output() close = new EventEmitter<void>();

  typeLabel(type: string): string {
    return { impact: 'Impact', keyword: 'Keyword', clarity: 'Clarity', quantify: 'Quantify' }[type] ?? type;
  }

  onBackdropClick(): void {
    this.close.emit();
  }
}
