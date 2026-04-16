export interface CvBlock {
  id: string;
  type: string;
  title: string;
  content: string;
  suggestion?: string;
  showDiff?: boolean;
  loading?: boolean;
}
