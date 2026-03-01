export const AttemptType = {
  Theme: 1,
  Ticket: 2,
  Exam: 3
} as const;

export type AttemptType =
  typeof AttemptType[keyof typeof AttemptType];