import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ScrapedJob {
  id: string;
  title: string;
  company: string;
  location: string;
  country: string;
  isRemote: boolean;
  jobBoard: string;
  externalUrl: string;
  description: string | null;
  salaryMin: number | null;
  salaryMax: number | null;
  currency: string | null;
  jobType: string | null;
  wave: number;
  postedAt: string | null;
  scrapedAt: string;
  isImported: boolean;
  importedOutreachId: string | null;
  matchScore: number | null;
  missingSkills: string[];
}

export interface ScraperInfo {
  lastScrapedAt: string | null;
  totalJobsInFeed: number;
  pendingRequests: number;
}

export interface UserJobPreference {
  country: string;
  city: string;
  jobType: string;
  keywords: string;
}

@Injectable({
  providedIn: 'root'
})
export class ScraperService {
  private readonly apiUrl = 'http://localhost:5155/api/scraper';

  constructor(private http: HttpClient) {}

  runScraper(): Observable<{ totalFound: number }> {
    return this.http.post<{ totalFound: number }>(`${this.apiUrl}/run`, {});
  }

  getJobs(filters?: { jobType?: string; remoteOnly?: boolean }): Observable<ScrapedJob[]> {
    const params: Record<string, string> = {};
    if (filters?.jobType) params['jobType'] = filters.jobType;
    if (filters?.remoteOnly) params['remoteOnly'] = 'true';
    return this.http.get<ScrapedJob[]>(`${this.apiUrl}/jobs`, { params });
  }

  importJob(jobId: string): Observable<ScrapedJob> {
    return this.http.post<ScrapedJob>(`${this.apiUrl}/jobs/${jobId}/import`, {});
  }

  getInfo(): Observable<ScraperInfo> {
    return this.http.get<ScraperInfo>(`${this.apiUrl}/info`);
  }

  getPreference(): Observable<UserJobPreference | null> {
    return this.http.get<UserJobPreference | null>(`${this.apiUrl}/preference`);
  }

  savePreference(pref: UserJobPreference): Observable<UserJobPreference> {
    return this.http.post<UserJobPreference>(`${this.apiUrl}/preference`, pref);
  }

  requestScrape(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/request`, {});
  }
}
