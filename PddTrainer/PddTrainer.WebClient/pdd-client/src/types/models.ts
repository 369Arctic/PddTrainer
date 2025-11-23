export interface AnswerOption {
  id: number;
  text: string;
  isCorrect: boolean;
  questionId: number;
}

export interface Question {
  id: number;
  text: string;
  imageUrl?: string | null;
  explanation?: string | null;
  ticketId: number;
  answerOptions: AnswerOption[];
}

export interface Ticket {
  id: number;
  title: string;
  questions: Question[];
}