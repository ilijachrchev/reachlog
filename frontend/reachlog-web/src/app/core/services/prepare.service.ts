import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PrepareResult {
  cvSummary: string;
  coverLetter: string;
}

@Injectable({
  providedIn: 'root'
})
export class PrepareService {
  private readonly baseUrl = 'http://localhost:5155/api/prepare';

  constructor(private http: HttpClient) {}

  prepare(jobDescription: string): Observable<PrepareResult> {
    return this.http.post<PrepareResult>(this.baseUrl, { jobDescription });
  }
}
