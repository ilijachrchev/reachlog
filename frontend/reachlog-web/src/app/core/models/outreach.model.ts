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
}

export interface CreateOutreachRequest {
  companyName: string;
  contactName: string;
  contactEmail: string;
  role: string;
  channel: string;
  rawMessage: string;
  sentAt: string;
}

export interface UpdateStatusRequest {
  status: string;
}