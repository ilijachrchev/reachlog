import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Outreach, CreateOutreachRequest, UpdateStatusRequest } from '../models/outreach.model';

export interface ScoreResult {
  matchScore: number;
  missingSkills: string[];
}

@Injectable({
  providedIn: 'root'
})
export class OutreachService {
  private readonly apiUrl = 'http://localhost:5155/api/outreach';

  constructor(private http: HttpClient) {}

  getAll(): Observable<Outreach[]> {
    return this.http.get<Outreach[]>(this.apiUrl);
  }

  getById(id: string): Observable<Outreach> {
    return this.http.get<Outreach>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateOutreachRequest): Observable<Outreach> {
    return this.http.post<Outreach>(this.apiUrl, request);
  }

  updateStatus(id: string, request: UpdateStatusRequest): Observable<Outreach> {
    return this.http.patch<Outreach>(`${this.apiUrl}/${id}/status`, request);
  }

  score(id: string): Observable<ScoreResult> {
    return this.http.post<ScoreResult>(`${this.apiUrl}/${id}/score`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}