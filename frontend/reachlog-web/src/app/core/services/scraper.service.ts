import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ScrapedJob {
  id: string;
  userId: string;
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

export interface ScraperStatus {
  isRunning: boolean;
  wave: number;
  totalFound: number;
  currentBoard: string | null;
  message: string | null;
}

export interface ScrapeRequest {
  countries?: string[];
  keywords?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ScraperService {
  private readonly apiUrl = 'http://localhost:5155/api/scraper';

  constructor(private http: HttpClient) {}

  runScraper(request?: ScrapeRequest): Observable<{ totalFound: number }> {
    return this.http.post<{ totalFound: number }>(`${this.apiUrl}/run`, request ?? {});
  }

  getJobs(filters?: { jobType?: string; remoteOnly?: boolean; minScore?: number }): Observable<ScrapedJob[]> {
    const params: Record<string, string> = {};
    if (filters?.jobType) params['jobType'] = filters.jobType;
    if (filters?.remoteOnly) params['remoteOnly'] = 'true';
    if (filters?.minScore !== undefined && filters.minScore !== null) params['minScore'] = String(filters.minScore);
    return this.http.get<ScrapedJob[]>(`${this.apiUrl}/jobs`, { params });
  }

  importJob(jobId: string): Observable<ScrapedJob> {
    return this.http.post<ScrapedJob>(`${this.apiUrl}/jobs/${jobId}/import`, {});
  }

  getStatus(): Observable<ScraperStatus> {
    return this.http.get<ScraperStatus>(`${this.apiUrl}/status`);
  }
}
