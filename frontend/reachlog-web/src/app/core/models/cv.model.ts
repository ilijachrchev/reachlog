export type SuggestionStatus = 'pending' | 'accepted' | 'rejected' | 'modified';
export type SuggestionType = 'impact' | 'keyword' | 'clarity' | 'quantify';

export interface CvSuggestion {
  id: string;
  section: string;
  type: SuggestionType;
  originalText: string;
  suggestedText: string;
  reason: string;
  status: SuggestionStatus;
  modifiedText?: string;
}

export interface CvImproveRequest {
  jobDescription?: string;
  outreachId?: string;
}
