import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getTicketById } from "../api/tickets";
import { getTicketByThemeId} from "../api/tickets";
import { useNavigate } from "react-router-dom";
import type { Ticket, Question, AnswerOption } from "../types/models";

const TicketPage: React.FC = () => {
    const navigate = useNavigate();
    const { id, themeId } = useParams();
    const [ticket, setTicket] = useState<Ticket | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const [currentQuestionIndex, setCurrentQuestionIndex] = useState<number>(0);
    const [answers, setAnswers] = useState<{ [key: number]: boolean }>({});
    const [selectedAnswerId, setSelectedAnswerId] = useState<number | null>(null);
    const [answersText, setAnswersText] = useState<{ [key: number]: string }>({});
    const [showHint, setShowHint] = useState<boolean>(false);

    useEffect(() => {
        const fetchTicket = async () => {
            try {
                var data = null;
                if (id) {
                    data = await getTicketById(Number(id));
                } else if (themeId) {
                    data = await getTicketByThemeId(Number(themeId));
                }
                if (!data){
                    setError("Билет не найден");
                    return;
                }

                // Добавляем orderNumber
                data.questions = data.questions.map((q: any, idx: number) => ({
                    ...q,
                    orderNumber: idx + 1
                }));

                setTicket(data);

            } catch (err) {
                console.error(err);
                setError("Ошибка при загрузке билета");
            } finally {
                setLoading(false);
            }
        };
        fetchTicket();
    }, [id, themeId]);

    useEffect(() => {
        if (!loading && ticket) {
            const question = ticket.questions[currentQuestionIndex];

            if (!question) {
                navigate(`/ticket/${ticket.id}/result`, {
                    state: { answers, answersText }
                });
            }
        }
    }, [loading, ticket, currentQuestionIndex, answers, navigate]);

    if (loading) return <div>Загрузка билета...</div>;
    if (error) return <div>{error}</div>;
    if (!ticket) return <div>Билет не найден</div>;

    const question: Question | undefined = ticket.questions[currentQuestionIndex];

    // === Статистика после прохождения всех вопросов ===
    if (!question) return null;

    const handleAnswerClick = (answer: AnswerOption) => {
        if (answers[question.id] !== undefined) return;

        setSelectedAnswerId(answer.id);
        setAnswersText(prev => ({ ...prev, [question.id]: answer.text }));
        const isCorrect = answer.isCorrect;
        setAnswers(prev => ({ ...prev, [question.id]: isCorrect }));

        if (isCorrect) {
            setShowHint(false);
            setTimeout(() => goToNextQuestion(), 500);
        } else {
            setShowHint(true);
        }
    };

    const goToNextQuestion = () => {
        // скрыть подсказку и снять выделение выбранного варианта
        setShowHint(false);
        setSelectedAnswerId(null);
        setCurrentQuestionIndex(prev => prev + 1);
    };


    const handleQuestionJump = (index: number) => {
        setCurrentQuestionIndex(index);
        setSelectedAnswerId(null);
        setShowHint(false);
    };

    return (
        <div style={{ padding: "20px", maxWidth: "700px", margin: "0 auto" }}>
            <h2>{ticket.title}</h2>

            {/* Нумерация вопросов */}
            <div style={{ display: "flex", flexWrap: "wrap", gap: "5px", marginBottom: "20px" }}>
                {ticket.questions.map((q, idx) => {
                    const answered = answers[q.id];
                    const isCurrent = currentQuestionIndex === idx;

                    let bgColor = "#eee";
                    let color = "#000";

                    if (answered === true) {
                        bgColor = "#4caf50";
                        color = "#fff";
                    } else if (answered === false) {
                        bgColor = "#f44336";
                        color = "#fff";
                    }

                    if (isCurrent) {
                        bgColor = "#333";
                        color = "#fff";
                    }

                    return (
                        <button
                            key={q.id}
                            onClick={() => handleQuestionJump(idx)}
                            style={{
                                width: 36,
                                height: 36,
                                borderRadius: "50%",
                                border: isCurrent ? "2px solid #ff9800" : "none",
                                backgroundColor: bgColor,
                                color: color,
                                cursor: "pointer",
                                transition: "all 0.3s",
                                fontWeight: isCurrent ? "bold" : "normal",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                            }}
                        >
                            {idx + 1}
                        </button>
                    );
                })}
            </div>

            {/* Вопрос */}
            <p>{question.text}</p>
            {question.imageUrl && (
                <img
                    src={`https://localhost:7269${question.imageUrl}`}
                    alt="Вопрос"
                    style={{ maxWidth: "100%", display: "block", margin: "10px 0" }}
                />
            )}

            {/* Варианты ответа */}
            <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
                {question.answerOptions.map((answer) => {
                    const isAnswered = answers[question.id] !== undefined;
                    let backgroundColor = "white";

                    if (isAnswered) {
                        if (answer.isCorrect) backgroundColor = "#d4edda";
                        else if (selectedAnswerId === answer.id) backgroundColor = "#f8d7da";
                    }

                    return (
                        <button
                            key={answer.id}
                            onClick={() => handleAnswerClick(answer)}
                            disabled={isAnswered}
                            style={{
                                padding: "12px",
                                border: "1px solid #ccc",
                                borderRadius: "8px",
                                cursor: isAnswered ? "default" : "pointer",
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

            {/* Подсказка и кнопки */}
            <div style={{ marginTop: "10px", display: "flex", flexDirection: "column", gap: "8px" }}>
                <div style={{ display: "flex", gap: "10px" }}>
                    <button
                        onClick={() => setShowHint(prev => !prev)}
                        style={{
                            padding: "6px 10px",
                            cursor: "pointer",
                            border: "1px solid #ccc",
                            borderRadius: "4px",
                            backgroundColor: "#f0f0f0"
                        }}
                    >
                        Подсказка
                    </button>

                    <button
                        onClick={goToNextQuestion}
                        style={{
                            padding: "6px 10px",
                            cursor: "pointer",
                            border: "1px solid #ccc",
                            borderRadius: "4px",
                            backgroundColor: "#007bff",
                            color: "white"
                        }}
                    >
                        Далее
                    </button>
                </div>

                {question.explanation && (
                    <div
                        style={{
                            minHeight: "60px",
                            padding: "10px",
                            backgroundColor: "#fff3cd",
                            border: "1px solid #ffeeba",
                            borderRadius: "6px",
                            display: showHint ? "block" : "none",
                        }}
                    >
                        {question.explanation}
                    </div>
                )}
            </div>
        </div>
    );
};

export default TicketPage;
