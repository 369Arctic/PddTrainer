import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";

export default function TicketResultPage() {
    const navigate = useNavigate();
    const { id } = useParams();
    const location = useLocation();

    const answers = location.state?.answers ?? {};
    const answersText = location.state?.answersText ?? {};

    const [ticket, setTicket] = useState<any>(null);
    const [currentWrongIndex, setCurrentWrongIndex] = useState(0);

    useEffect(() => {
        fetch(`https://localhost:7269/api/tickets/${id}`)
            .then((r) => r.json())
            .then((data) => {
                data.questions = data.questions.map((q: any, idx: number) => ({
                    ...q,
                    orderNumber: idx + 1
                }));
                setTicket(data);
            })
            .catch((err) => console.error(err));
    }, [id]);

    if (!ticket) return <div style={{ padding: 30 }}>Загрузка билета...</div>;

    const wrongQuestions = ticket.questions.filter((q: any) => answers[q.id] !== true);

    if (wrongQuestions.length === 0)
        return (
            <div style={{ padding: 30, fontSize: 22 }}>
                Поздравляем! Все ответы верные.
                <div style={{ marginTop: 20 }}>
                    <button onClick={() => navigate("/")} style={mainBtn}>
                        На главную
                    </button>
                </div>
            </div>
        );

    const currentQuestion = wrongQuestions[currentWrongIndex];
    const userAnswer = answersText[currentQuestion.id] ?? "—";
    const correctAnswer =
        currentQuestion.answerOptions?.find((a: any) => a.isCorrect)?.text ?? "—";

    return (
        <div style={page}>
            <h1 style={title}>Ошибки в билете {ticket.title}</h1>

            <div
                style={{
                    display: "flex",
                    flexWrap: "wrap",
                    gap: 6,
                    justifyContent: "center",
                    marginBottom: 30
                }}
            >
                {wrongQuestions.map((q: any, idx: number) => {
                    const isCurrent = idx === currentWrongIndex;
                    return (
                        <button
                            key={q.id}
                            onClick={() => setCurrentWrongIndex(idx)}
                            style={{
                                width: 36,
                                height: 36,
                                borderRadius: "50%",
                                border: isCurrent ? "2px solid #ff9800" : "none",
                                backgroundColor: isCurrent ? "#333" : "#f44336",
                                color: "#fff",
                                cursor: "pointer",
                                fontWeight: isCurrent ? "bold" : "normal",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                transition: "all 0.3s"
                            }}
                        >
                            {q.orderNumber}
                        </button>
                    );
                })}
            </div>

            {/* Текущий вопрос */}
            <div style={questionCard}>
                <h2 style={questionTitle}>Вопрос {currentQuestion.orderNumber}</h2>

                {currentQuestion.imageUrl && (
                    <img
                        src={
                            currentQuestion.imageUrl.startsWith("http")
                                ? currentQuestion.imageUrl
                                : `https://localhost:7269${currentQuestion.imageUrl}`
                        }
                        alt=""
                        style={questionImage}
                    />
                )}

                <p style={questionText}>{currentQuestion.text}</p>

                <div style={{ marginTop: 20 }}>
                    <p style={row}>
                        <span style={{ ...label, color: "#e74c3c" }}>Ваш ответ:</span>
                        <span style={value}>{userAnswer}</span>
                    </p>

                    <p style={row}>
                        <span style={{ ...label, color: "#2ecc71" }}>Правильный ответ:</span>
                        <span style={value}>{correctAnswer}</span>
                    </p>

                    {currentQuestion.explanation && (
                        <div style={explanation}>{currentQuestion.explanation}</div>
                    )}
                </div>
            </div>

            {/* Кнопки навигации */}
            <div style={btnRow}>
                <button
                    onClick={() => navigate(`/ticket/${id}`)}
                    style={mainBtn}
                >
                    Перерешать билет
                </button>

                <button
                    onClick={() => navigate("/")}
                    style={{ ...mainBtn, background: "#666" }}
                >
                    На главную
                </button>
            </div>
        </div>
    );
}

/* Стили */
const page: React.CSSProperties = {
    padding: "30px",
    maxWidth: "900px",
    margin: "0 auto",
    fontFamily: "Inter, Arial, sans-serif"
};

const title: React.CSSProperties = {
    textAlign: "center",
    marginBottom: 30,
    fontSize: 32,
    fontWeight: 700
};

const questionCard: React.CSSProperties = {
    background: "white",
    borderRadius: 14,
    padding: 25,
    boxShadow: "0 4px 14px rgba(0,0,0,0.1)",
    marginBottom: 40
};

const questionTitle: React.CSSProperties = {
    margin: 0,
    marginBottom: 15,
    fontSize: 26,
    fontWeight: 700
};

const questionImage: React.CSSProperties = {
    maxWidth: "100%",
    borderRadius: 12,
    marginBottom: 20
};

const questionText: React.CSSProperties = {
    fontSize: 20,
    fontWeight: 500,
    marginBottom: 20
};

const row: React.CSSProperties = {
    display: "flex",
    gap: 10,
    alignItems: "baseline",
    marginBottom: 8
};

const label: React.CSSProperties = {
    fontWeight: 500,
    fontSize: 18,
};

const value: React.CSSProperties = {
    fontWeight: 500,
    fontSize: 18,
    color: "#111"
};

const explanation: React.CSSProperties = {
    marginTop: 18,
    background: "#eef6ff",
    padding: 15,
    borderRadius: 10,
    fontSize: 16,
    lineHeight: "1.5"
};

const btnRow: React.CSSProperties = {
    display: "flex",
    justifyContent: "center",
    gap: 20,
    marginTop: 30
};

const mainBtn: React.CSSProperties = {
    padding: "14px 24px",
    background: "#1877ff",
    color: "white",
    border: "none",
    borderRadius: 10,
    cursor: "pointer",
    fontSize: 18,
    fontWeight: 600
};
