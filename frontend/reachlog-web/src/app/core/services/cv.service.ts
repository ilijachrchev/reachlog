import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CvInfo {
  fileName: string;
  uploadedAt: string;
  characterCount: number;
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
}