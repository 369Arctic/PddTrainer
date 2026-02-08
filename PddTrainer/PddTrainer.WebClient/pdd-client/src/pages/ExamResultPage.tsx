import React from "react";
import { useLocation, useNavigate } from "react-router-dom";

const ExamResultPage: React.FC = () => {
  const { state } = useLocation();
  const navigate = useNavigate();

  if (!state) return <div>Нет данных экзамена</div>;

  const { exam, mistakes, failed } = state;

  const passed =
    !failed &&
    (exam.maxMistakes === undefined || mistakes <= exam.maxMistakes);

  return (
    <div style={{ maxWidth: 600, margin: "0 auto", padding: 20 }}>
      <h2>{exam.title}</h2>

      <h3 style={{ color: passed ? "green" : "red" }}>
        {passed ? "Экзамен сдан" : "Экзамен не сдан"}
      </h3>

      <p>Ошибок допущено: {mistakes}</p>

      <button
        onClick={() => navigate("/")}
        style={{
          marginTop: 20,
          padding: "10px 16px",
          borderRadius: 6,
          border: "none",
          backgroundColor: "#007bff",
          color: "white",
          cursor: "pointer"
        }}
      >
        Вернуться к билетам
      </button>
    </div>
  );
};

export default ExamResultPage;
