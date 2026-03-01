import React, { useCallback, useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getExamById } from "../api/exams";
import type { Question, AnswerOption } from "../types/models";

import "./TicketPage.css"

type ExamDto = {
  id: string;
  title: string;
  totalQuestions: number;
  timeMinutes: number;
  maxMistakes?: number;
  extraQuestionsForMistakes?: number;
  questions: Question[];
};

const API_BASE_URL = import.meta.env.VITE_API_URL;

const ExamPage: React.FC = () => {
  const { examId } = useParams<{ examId: string}>();
  const navigate = useNavigate();

  const [exam, setExam] = useState<ExamDto | null>(null);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<number, number>>({});
  const [mistakes, setMistakes] = useState(0);
  const [failed, setFailed] = useState(false);
  const [timeLeft, setTimeLeft] = useState<number>(0);
  const [loading, setLoading] = useState(true);

  // Загрузка экзамена.
  useEffect(() => {
    if (!examId) return;

    const loadExam = async () => {
      try {
        const data = await getExamById(examId);
        setExam(data);
        setTimeLeft(data.timeMinutes * 60);
      } finally{
        setLoading(false);
      }
    };

    loadExam();
  }, [examId]);


  // Завершение экзамена
  const finishExam = useCallback(
    (timeExpired = false) => {
      navigate("/exam/result", {
        state: {
          exam,
          answers,
          mistakes,
          failed: failed || timeExpired,
        },
      });
    },
    [navigate, exam, answers, mistakes, failed]
  );

  // Таймер
  useEffect(() => {
    if (loading || !exam) return;

    const timer = setInterval(() => {
      setTimeLeft(prev => {
        if (prev <= 1){
          clearInterval(timer);
          finishExam(true);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [loading, exam, finishExam]);

  // Автозавершение
  useEffect(() => {
    if (!exam) return;
    if (currentIndex >= exam.questions.length) {
      finishExam();
    }
  }, [currentIndex, exam, finishExam]);

  // Ответ на вопрос
  const submitAnswer = useCallback(
    (question: Question, answer: AnswerOption) => {
      setAnswers(prev => ({ ...prev, [question.id]: answer.id}));

      if (!answer.isCorrect && exam?.maxMistakes != undefined){
        setMistakes(prev => {
          const next = prev + 1;
          if (next > exam.maxMistakes!){
            setFailed(true);
          }
          return next;
        });
      }

      setCurrentIndex(prev => prev + 1);
    },
    [exam]);
  
  const handleAnswerClick = (answer: AnswerOption) => {
    if (!exam) return;

    const question = exam.questions[currentIndex];
    if (!question) return;
    if (answers[question.id] !== undefined) return;

    submitAnswer(question, answer);
  };

  // UI
  // TOOD - в ресурсы или константы.
  if (loading) return <div>Загрузка экзамена…</div>;
  if (!exam) return <div>Экзамен не найден</div>;

  const question = exam.questions[currentIndex];
  if (!question) return null;

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  return (
    <div className="exam-container">
      <div className="exam-header">
        <h2 className="exam-title">{exam.title}</h2>
        <div className="exam-timer">
          {minutes}: {seconds.toString().padStart(2, "0")}
        </div>
      </div>

      {/* Навигация по вопросам */}
      <div className="questions-navigation">
        {exam.questions.map((q, idx) => {
          const isCurrent = idx === currentIndex;
          const isAnswered = answers[q.id] !== undefined;
          let className = "";

          if (isCurrent) className = "answer-selected";
          else if (isAnswered) className = "answer-correct";

          return (
            <button
              key={q.id}
              onClick={() => setCurrentIndex(idx)}
              className={`exam-button exam-button-circle ${className}`}
            >
              {idx + 1}
            </button>
          );
        })}
      </div>

      <div className="question-card">
        <p className="question-text">{question.text}</p>

        {question.imageUrl && (
          <img
            src={`${API_BASE_URL}${question.imageUrl}`}
            alt="Вопрос"
            className="question-image"
        />
        )}

        <div className="answers-list">
          {question.answerOptions.map((answer) => {
            const isAnswered = answers[question.id] !== undefined;
            const isSelected = answers[question.id] === answer.id;

            let answerClass = "answer-option";
            if (isSelected){
              answerClass += " answer-selected";
            }

            return (
              <button
                key={answer.id}
                onClick={() => handleAnswerClick(answer)}
                disabled={isAnswered}
                className={answerClass}
              >
                {answer.text}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default ExamPage;