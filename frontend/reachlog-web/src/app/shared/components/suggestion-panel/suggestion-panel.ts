import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CvSuggestion } from '../../../core/models/cv.model';

interface SuggestionGroup {
  section: string;
  suggestions: CvSuggestion[];
  collapsed: boolean;
}

@Component({
  selector: 'app-suggestion-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './suggestion-panel.html',
  styleUrl: './suggestion-panel.scss'
})
export class SuggestionPanelComponent {
  @Input() set suggestions(value: CvSuggestion[]) {
    this._suggestions = value;
    this.buildGroups(value);
  }
  get suggestions(): CvSuggestion[] { return this._suggestions; }

  @Input() hasImproved = false;

  @Output() accept = new EventEmitter<string>();
  @Output() reject = new EventEmitter<string>();
  @Output() modify = new EventEmitter<string>();
  @Output() scrollTo = new EventEmitter<string>();

  private _suggestions: CvSuggestion[] = [];
  groups: SuggestionGroup[] = [];

  get pendingCount(): number {
    return this._suggestions.filter(s => s.status === 'pending').length;
  }

  private buildGroups(suggestions: CvSuggestion[]): void {
    const pending = suggestions.filter(s => s.status === 'pending');
    const map = new Map<string, CvSuggestion[]>();
    for (const s of pending) {
      const arr = map.get(s.section) ?? [];
      arr.push(s);
      map.set(s.section, arr);
    }
    const existing = new Map(this.groups.map(g => [g.section, g.collapsed]));
    this.groups = Array.from(map.entries()).map(([section, sArr]) => ({
      section,
      suggestions: sArr,
      collapsed: existing.get(section) ?? false,
    }));
  }

  toggleGroup(group: SuggestionGroup): void {
    group.collapsed = !group.collapsed;
  }

  truncate(text: string, max = 40): string {
    return text.length > max ? text.slice(0, max) + '…' : text;
  }

  typeLabel(type: string): string {
    return { impact: 'Impact', keyword: 'Keyword', clarity: 'Clarity', quantify: 'Quantify' }[type] ?? type;
  }

  onCardClick(id: string): void {
    this.scrollTo.emit(id);
  }
}
