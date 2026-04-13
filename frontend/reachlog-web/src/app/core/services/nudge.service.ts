import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Nudge {
  outreachId: string;
  companyName: string;
  role: string;
  sentAt: string;
  daysSinceSent: number;
  nudgeType: 'NoReply' | 'RecentRejection';
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class NudgeService {
  private readonly apiUrl = 'http://localhost:5155/api/nudges';

  constructor(private http: HttpClient) {}

  getNudges(): Observable<Nudge[]> {
    return this.http.get<Nudge[]>(this.apiUrl);
  }
}