import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CvBlock } from '../models/cv.model';

export interface CvInfo {
  fileName: string;
  uploadedAt: string;
  characterCount: number;
  contentType: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class CvService {
  private readonly apiUrl = 'http://localhost:5155/api/cv';

  constructor(private http: HttpClient) {}

  get(): Observable<CvInfo> {
    return this.http.get<CvInfo>(this.apiUrl);
  }

  upload(file: File): Observable<CvInfo> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<CvInfo>(this.apiUrl, formData);
  }

  getFileUrl(): string {
    return `${this.apiUrl}/file`;
  }

  getBlocks(): Observable<CvBlock[]> {
    return this.http.get<CvBlock[]>(`${this.apiUrl}/blocks`);
  }

  getSuggestions(blocks: CvBlock[], jobDescription?: string): Observable<{ blockId: string; suggestedContent: string }[]> {
    return this.http.post<{ blockId: string; suggestedContent: string }[]>(`${this.apiUrl}/suggest`, {
      blocks,
      jobDescription
    });
  }

  exportCvAsDocx(blocks: CvBlock[]): Observable<Blob> {
    return this.http.post<Blob>(`${this.apiUrl}/export/docx`, { blocks }, { responseType: 'blob' as 'json' });
  }

  exportCvAsPdf(blocks: CvBlock[]): Observable<Blob> {
    return this.http.post<Blob>(`${this.apiUrl}/export/pdf`, { blocks }, { responseType: 'blob' as 'json' });
  }
}
