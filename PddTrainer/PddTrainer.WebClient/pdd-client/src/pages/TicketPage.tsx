import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getTicketById } from "../api/tickets";
import type { Ticket, Question, AnswerOption } from "../types/models";

const TicketPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [currentQuestionIndex, setCurrentQuestionIndex] = useState<number>(0);
  const [selectedAnswerId, setSelectedAnswerId] = useState<number | null>(null);
  const [showExplanation, setShowExplanation] = useState<boolean>(false);

  useEffect(() => {
    const fetchTicket = async () => {
      try {
        if (!id) return;
        const data = await getTicketById(Number(id));
        setTicket(data);
      } catch (err) {
        console.error(err);
        setError("Ошибка при загрузке билета");
      } finally {
        setLoading(false);
      }
    };
    fetchTicket();
  }, [id]);

  if (loading) return <div>Загрузка билета...</div>;
  if (error) return <div>{error}</div>;
  if (!ticket) return <div>Билет не найден</div>;

  const question: Question | undefined = ticket.questions[currentQuestionIndex];
  if (!question) return <div>Все вопросы пройдены! 🎉</div>;

  // Процент прогресса
  const progressPercent =
    ((currentQuestionIndex + (selectedAnswerId ? 1 : 0)) /
      ticket.questions.length) *
    100;

  const handleAnswerClick = (answer: AnswerOption) => {
    setSelectedAnswerId(answer.id);

    if (!answer.isCorrect) {
      setShowExplanation(true);
    } else {
      // Правильный ответ подсвечивается, через 800ms переходим к следующему вопросу
      setShowExplanation(false);
      setTimeout(() => {
        setCurrentQuestionIndex((prev) => prev + 1);
        setSelectedAnswerId(null);
      }, 800);
    }
  };

  return (
    <div style={{ padding: "20px", maxWidth: "700px", margin: "0 auto" }}>
      <h2>{ticket.title}</h2>
      <h4>
        Вопрос {currentQuestionIndex + 1} из {ticket.questions.length}
      </h4>

      {/* Прогресс бар */}
      <div
        style={{
          width: "100%",
          height: "10px",
          backgroundColor: "#eee",
          borderRadius: "5px",
          marginBottom: "15px",
          overflow: "hidden",
        }}
      >
        <div
          style={{
            width: `${progressPercent}%`,
            height: "100%",
            backgroundColor: "#4caf50",
            transition: "width 0.3s",
          }}
        />
      </div>

      <p>{question.text}</p>
      {question.imageUrl && (
        <img
          src={`https://localhost:7269${question.imageUrl}`}
          alt="Вопрос"
          style={{ maxWidth: "100%", display: "block", margin: "10px 0" }}
        />
      )}

      <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
        {question.answerOptions.map((answer) => {
          const isSelected = selectedAnswerId === answer.id;
          const isCorrect = answer.isCorrect;

          let backgroundColor = "white";
          if (isSelected) {
            backgroundColor = isCorrect ? "#d4edda" : "#f8d7da";
          }

          return (
            <button
              key={answer.id}
              onClick={() => handleAnswerClick(answer)}
              disabled={!!selectedAnswerId && !showExplanation}
              style={{
                padding: "12px",
                border: "1px solid #ccc",
                borderRadius: "8px",
                cursor: "pointer",
                backgroundColor,
                boxShadow: "1px 1px 5px rgba(0,0,0,0.1)",
                textAlign: "left",
                transition: "background-color 0.3s",
              }}
            >
              {answer.text}
            </button>
          );
        })}
      </div>

      {showExplanation && question.explanation && (
        <div
          style={{
            marginTop: "15px",
            padding: "12px",
            backgroundColor: "#fff3cd",
            border: "1px solid #ffeeba",
            borderRadius: "6px",
          }}
        >
          <strong>Подсказка:</strong> {question.explanation}
          <div style={{ marginTop: "8px" }}>
            <button
              onClick={() => {
                setShowExplanation(false);
                setSelectedAnswerId(null);
              }}
              style={{
                padding: "6px 10px",
                cursor: "pointer",
                border: "1px solid #ccc",
                borderRadius: "4px",
              }}
            >
              Продолжить
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default TicketPage;
