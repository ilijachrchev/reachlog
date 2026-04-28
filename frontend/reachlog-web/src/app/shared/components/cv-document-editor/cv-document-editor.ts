import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
  OnChanges,
  SimpleChanges,
  ElementRef,
  ViewChild,
  PLATFORM_ID,
  Inject,
  AfterViewInit,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Editor } from '@tiptap/core';
import { StarterKit } from '@tiptap/starter-kit';
import { CvSuggestion } from '../../../core/models/cv.model';
import { parseCvToTiptapDoc } from '../../utils/cv-parser';
import { SuggestionMark } from './suggestion-mark';

@Component({
  selector: 'app-cv-document-editor',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv-document-editor.html',
  styleUrl: './cv-document-editor.scss'
})
export class CvDocumentEditorComponent implements AfterViewInit, OnDestroy, OnChanges {
  @Input() initialContent = '';
  @Input() suggestions: CvSuggestion[] = [];
  @Input() editable = true;

  @Output() suggestionClicked = new EventEmitter<string>();
  @Output() contentChanged = new EventEmitter<string>();

  @ViewChild('editorContainer') editorContainer!: ElementRef<HTMLDivElement>;

  private editor: Editor | null = null;
  private clickHandler: ((e: MouseEvent) => void) | null = null;
  private isBrowser: boolean;

  constructor(@Inject(PLATFORM_ID) platformId: object) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser) return;
    this.initEditor();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.editor) return;
    if (changes['suggestions']) {
      this.applyMarkings();
    }
    if (changes['editable']) {
      this.editor.setEditable(this.editable);
    }
  }

  ngOnDestroy(): void {
    if (this.editor) {
      if (this.clickHandler) {
        this.editorContainer.nativeElement.removeEventListener('click', this.clickHandler);
      }
      this.editor.destroy();
    }
  }

  private initEditor(): void {
    const doc = parseCvToTiptapDoc(this.initialContent);

    this.editor = new Editor({
      element: this.editorContainer.nativeElement,
      extensions: [StarterKit, SuggestionMark],
      content: doc as any,
      editable: this.editable,
      onUpdate: ({ editor }) => {
        this.contentChanged.emit(editor.getText({ blockSeparator: '\n' }));
      },
    });

    this.clickHandler = (e: MouseEvent) => {
      const target = e.target as HTMLElement;
      const mark = target.closest('[data-suggestion-id]') as HTMLElement | null;
      if (mark?.dataset['suggestionId']) {
        this.suggestionClicked.emit(mark.dataset['suggestionId']);
      }
    };
    this.editorContainer.nativeElement.addEventListener('click', this.clickHandler);

    if (this.suggestions.length > 0) {
      this.applyMarkings();
    }
  }

  private applyMarkings(): void {
    
    if (!this.editor) {
      return;
    }

    const pending = this.suggestions.filter(s => s.status === 'pending');
    
    const schema = this.editor.schema;
    const markType = schema.marks['suggestion'];
    
    if (!markType) {
      return;
    }

    const tr = this.editor.state.tr;
    tr.removeMark(0, this.editor.state.doc.content.size, markType);

    const doc = this.editor.state.doc;
    let appliedCount = 0;
    let failedCount = 0;
    
    for (const suggestion of pending) {
      const pos = this.findTextPosition(doc, suggestion.originalText);
      if (pos) {
        tr.addMark(pos.from, pos.to, markType.create({
          id: suggestion.id,
          type: suggestion.type,
          status: suggestion.status,
        }));
        appliedCount++;
      } else {
        failedCount++;
      }
    }

    
    this.editor.view.dispatch(tr);
    
    setTimeout(() => {
      let markCount = 0;
      this.editor!.state.doc.descendants((node: any) => {
        if (node.isText && node.marks) {
          if (node.marks.some((m: any) => m.type.name === 'suggestion')) {
            markCount++;
          }
        }
        return undefined;
      });
    }, 100);
  }

  private findTextPosition(doc: any, searchText: string): { from: number; to: number } | null {
    const textNodes: { pos: number; text: string }[] = [];

    (doc as any).descendants((node: any, pos: number) => {
      if (node.isText && node.text) {
        textNodes.push({ pos, text: node.text as string });
      }
    });

    let fullText = '';
    const posMap: number[] = [];
    for (let n = 0; n < textNodes.length; n++) {
      const { pos, text } = textNodes[n];
      if (n > 0) {
        fullText += ' ';
        posMap.push(-1);
      }
      for (let i = 0; i < text.length; i++) {
        posMap.push(pos + i);
      }
      fullText += text;
    }

    const normalize = (s: string) => s.replace(/\s+/g, ' ').trim();
    const normalizedFull = normalize(fullText);
    const normalizedSearch = normalize(searchText);

    const normalizedIdx = normalizedFull.indexOf(normalizedSearch);
    if (normalizedIdx === -1) return null;

    let origIdx = 0;
    let normIdx = 0;
    let startInOriginal = -1;
    let endInOriginal = -1;

    while (origIdx < fullText.length && normIdx < normalizedFull.length) {
      if (normIdx === normalizedIdx && startInOriginal === -1) {
        startInOriginal = origIdx;
      }
      if (normIdx === normalizedIdx + normalizedSearch.length) {
        endInOriginal = origIdx;
        break;
      }

      const origChar = fullText[origIdx];
      const normChar = normalizedFull[normIdx];

      if (/\s/.test(origChar)) {
        origIdx++;
        while (origIdx < fullText.length && /\s/.test(fullText[origIdx])) origIdx++;
        if (normChar === ' ') normIdx++;
      } else if (origChar === normChar) {
        origIdx++;
        normIdx++;
      } else {
        return null;
      }
    }

    if (endInOriginal === -1 && normIdx >= normalizedIdx + normalizedSearch.length) {
      endInOriginal = origIdx;
    }

    if (startInOriginal === -1 || endInOriginal === -1) return null;
    while (endInOriginal > startInOriginal && /\s/.test(fullText[endInOriginal - 1])) {
      endInOriginal--;
    }

    let from = posMap[startInOriginal];
    while (from === -1 && startInOriginal < posMap.length - 1) {
      startInOriginal++;
      from = posMap[startInOriginal];
    }

    let toIdx = endInOriginal - 1;
    let toPos = posMap[toIdx];
    while (toPos === -1 && toIdx > 0) {
      toIdx--;
      toPos = posMap[toIdx];
    }

    if (from === -1 || toPos === -1) return null;

    return { from, to: toPos + 1 };
  }

  applySuggestion(id: string, overrideText?: string): void {
  
  if (!this.editor) {
    return;
  }
  
  const suggestion = this.suggestions.find(s => s.id === id);
  if (!suggestion) {
    return;
  }

  const text = overrideText ?? suggestion.suggestedText;
  
  const schema = this.editor.schema;

  let markFrom = -1;
  let markTo = -1;
  let foundNodes: any[] = [];

  this.editor.state.doc.descendants((node: any, pos: number) => {
    if (node.isText && node.marks) {
      const hasMark = (node.marks as any[]).some(
        (m: any) => m.type.name === 'suggestion' && m.attrs['id'] === id
      );
      if (hasMark) {
        const nodeEnd = pos + (node.nodeSize as number);
        foundNodes.push({ pos, nodeEnd, text: node.text });
        if (markFrom === -1) markFrom = pos;
        markTo = nodeEnd;
      }
    }
    return undefined;
  });


  if (markFrom !== -1 && markTo !== -1) {
    const tr = this.editor.state.tr;
    tr.replaceWith(markFrom, markTo, schema.text(text));
    this.editor.view.dispatch(tr);
    
    setTimeout(() => {
      const fullText = this.editor!.getText({ blockSeparator: '\n' });
    }, 100);
  }
}

  rejectSuggestion(id: string): void {
    if (!this.editor) return;
    const markType = this.editor.schema.marks['suggestion'];
    if (!markType) return;

    let markFrom = -1;
    let markTo = -1;

    this.editor.state.doc.descendants((node: any, pos: number) => {
      if (node.isText && node.marks) {
        const hasMark = (node.marks as any[]).some(
          (m: any) => m.type.name === 'suggestion' && m.attrs['id'] === id
        );
        if (hasMark) {
          const nodeEnd = pos + (node.nodeSize as number);
          if (markFrom === -1) markFrom = pos;
          markTo = nodeEnd;
        }
      }
      return undefined;
    });

    if (markFrom !== -1 && markTo !== -1) {
      const tr = this.editor.state.tr;
      tr.removeMark(markFrom, markTo, markType);
      this.editor.view.dispatch(tr);
    }
  }

  getFullText(): string {
    if (!this.editor) return '';
    return this.editor.getText({ blockSeparator: '\n' });
  }

  scrollToSuggestion(id: string): void {
    const el = this.editorContainer.nativeElement.querySelector(`[data-suggestion-id="${id}"]`);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }
}
