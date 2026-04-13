import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SkillGap {
  skill: string;
  count: number;
}

export interface Analytics {
  totalOutreaches: number;
  byStatus: Record<string, number>;
  byChannel: Record<string, number>;
  openRate: number;
  replyRate: number;
  averageMatchScore: number | null;
  topMissingSkills: SkillGap[];
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private readonly apiUrl = 'http://localhost:5155/api/analytics';

  constructor(private http: HttpClient) {}

  getAnalytics(): Observable<Analytics> {
    return this.http.get<Analytics>(this.apiUrl);
  }
}