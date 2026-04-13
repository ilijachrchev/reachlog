import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ParseResult } from '../models/outreach.model';

@Injectable({
  providedIn: 'root'
})
export class ParseService {
  private readonly apiUrl = 'http://localhost:5155/api/parse';

  constructor(private http: HttpClient) {}

  parse(rawText: string): Observable<ParseResult> {
    return this.http.post<ParseResult>(this.apiUrl, { rawMessage: rawText });
  }
}