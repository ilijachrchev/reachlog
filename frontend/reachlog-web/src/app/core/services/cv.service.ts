import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CvSuggestion, CvImproveRequest } from '../models/cv.model';

export interface CvInfo {
  id: string;
  fileName: string;
  uploadedAt: string;
  characterCount: number;
  extractedText?: string;
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

  improveCv(request: CvImproveRequest): Observable<CvSuggestion[]> {
    return this.http.post<CvSuggestion[]>(`${this.apiUrl}/improve`, request);
  }

  exportCvAsDocx(fullText: string): Observable<Blob> {
    return this.http.post<Blob>(`${this.apiUrl}/export/docx`, { fullText }, { responseType: 'blob' as 'json' });
  }

  exportCvAsPdf(fullText: string): Observable<Blob> {
    return this.http.post<Blob>(`${this.apiUrl}/export/pdf`, { fullText }, { responseType: 'blob' as 'json' });
  }
}
