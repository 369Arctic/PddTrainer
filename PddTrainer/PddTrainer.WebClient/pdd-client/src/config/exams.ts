// Конфигурация всех типов экзаменов
export type ExamMode = {
  id: string;
  title: string;
  totalQuestions: number; // кол-во вопросов в экзамене.
  timeMinutes: number; // ограничение по времени.
  maxMistakes?: number; // ограничение на ошибки.
  extraQuestionsForMistakes?: number; // доп. вопросы при ошибках.
  useAllQuestions?: boolean; // для марафона.
};

export const examModes: ExamMode[] = [
  {
    id: "autoSchool",
    title: "Экзамен автошколы",
    totalQuestions: 60,
    timeMinutes: 40,
    maxMistakes: 2,
    extraQuestionsForMistakes: 5
  },
  {
    id: "marathon",
    title: "Марафон",
    totalQuestions: 800,
    timeMinutes: 200,
    useAllQuestions: true
  },
  {
    id: "hundred",
    title: "100 вопросов",
    totalQuestions: 100,
    timeMinutes: 60
  },
  {
    id: "fifty",
    title: "50 вопросов",
    totalQuestions: 50,
    timeMinutes: 60
  },
  {
    id: "gibdd",
    title: "Экзамен ГИБДД",
    totalQuestions: 20,
    timeMinutes: 20,
    maxMistakes: 2,
    extraQuestionsForMistakes: 5
  }
];
