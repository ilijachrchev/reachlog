export interface Outreach {
  id: string;
  companyName: string;
  contactName: string;
  contactEmail: string;
  role: string;
  channel: string;
  status: string;
  isOpened: boolean;
  sentAt: string;
  createdAt: string;
  matchScore: number | null;
  missingSkills: string[] | null;
  rawMessage: string | null;
  notes: string | null;
}

export interface CreateOutreachRequest {
  companyName: string;
  contactName: string;
  contactEmail: string;
  role: string;
  channel: string;
  rawMessage: string;
  sentAt: string | null;
  notes: string;
}

export interface UpdateOutreachRequest {
  companyName: string;
  contactName: string | null;
  contactEmail: string | null;
  role: string | null;
  channel: string;
  rawMessage: string | null;
  notes: string | null;
  sentAt: string;
}

export interface UpdateStatusRequest {
  status: string;
}

export interface ParseResult {
  companyName: string | null;
  contactName: string | null;
  contactEmail: string | null;
  role: string | null;
  channel: string | null;
  sentAt: string | null;
}