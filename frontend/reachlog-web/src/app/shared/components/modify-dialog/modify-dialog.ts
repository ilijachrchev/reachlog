import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CvSuggestion } from '../../../core/models/cv.model';

@Component({
  selector: 'app-modify-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './modify-dialog.html',
  styleUrl: './modify-dialog.scss'
})
export class ModifyDialogComponent implements OnChanges {
  @Input() suggestion: CvSuggestion | null = null;

  @Output() save = new EventEmitter<{ id: string; text: string }>();
  @Output() cancel = new EventEmitter<void>();

  editedText = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['suggestion'] && this.suggestion) {
      this.editedText = this.suggestion.suggestedText;
    }
  }

  onSave(): void {
    if (this.suggestion) {
      this.save.emit({ id: this.suggestion.id, text: this.editedText });
    }
  }
}
