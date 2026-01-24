import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import { getAllTickets } from "../api/tickets";
import { getAllThemes } from "../api/themes";

import { examModes } from "../config/exams";

import "./TicketsListPage.css";

import type { Ticket, Theme } from "../types/models";
import type { ExamMode } from "../config/exams";

type TabKey = "themes" | "tickets" | "exam";

const TicketsListPage: React.FC = () => {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [themes, setThemes] = useState<Theme[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>("themes");

  const navigate = useNavigate();

  useEffect(() => {
    const loadData = async () => {
      try {
        const [ticketsData, themesData] = await Promise.all([
          getAllTickets(),
          getAllThemes(),
        ]);

        setTickets(ticketsData);
        setThemes(themesData);
      } catch (e) {
        console.error(e);
        setError("Ошибка при загрузке данных");
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  if (loading) return <div>Загрузка...</div>;
  if (error) return <div>{error}</div>;

  // TODO: перевести на ресурсы или константы
  return (
    <div className="page">
      <h1>Билеты ПДД</h1>

      <div className="tabs">
        <TabButton
          label="По темам"
          active={activeTab === "themes"}
          onClick={() => setActiveTab("themes")}
        />
        <TabButton
          label="По номерам"
          active={activeTab === "tickets"}
          onClick={() => setActiveTab("tickets")}
        />
        <TabButton
          label="Экзамен"
          active={activeTab === "exam"}
          onClick={() => setActiveTab("exam")}
        />
      </div>

      {/* Вкладка "По темам" */}
      {activeTab === "themes" && (
        <div className="grid-themes">
          {themes.map((theme) => (
            <div
              key={theme.id}
              className="card"
              onClick={() => navigate(`/theme/${theme.id}`)}>
              <div className="card-title">
                {theme.title}
              </div>

              <div className="card-subtitle">
                Перейти к вопросам темы
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Вкладка "По номерам" */}
      {activeTab === "tickets" && (
  <div className="grid-tickets">
    {tickets.map((ticket) => (
      <div
        key={ticket.id}
        className="card"
        onClick={() => navigate(`/ticket/${ticket.id}`)}
      >
        <div className="card-title">
           {ticket.title}
        </div>
        <div className="card-sutbtitle">
          {ticket.questions.length} вопросов
        </div>
      </div>
    ))}
  </div>
)}

      {/* Вкладка "Экзамен" */}
    {activeTab === "exam" && (
      <div className="grid-exams">
        {examModes.map((exam) => (
          <ExamTile
            key={exam.id}
            exam={exam}
            onStart={(exam) =>
              navigate(`/exam/${exam.id}`, { state: { exam } })
            }
          />
        ))}
      </div>
    )}
  </div>
  );
};

type TabButtonProps = {
  label: string;
  active: boolean;
  onClick: () => void;
};

const TabButton: React.FC<TabButtonProps> = ({
  label,
  active,
  onClick,
}) => {
  return (
    <button
      onClick={onClick}
      className={`tab-button ${active ? "active" : ""}`}
    >
      {label}
    </button>
  );
};

const ExamTile: React.FC<{ exam: ExamMode; onStart: (exam: ExamMode) => void}> = ({ exam, onStart}) => {
  return (
    <div
      className="card"
      style={{ cursor: "pointer", textAlign: "center", padding: 100 }}
      onClick={() => onStart(exam)}
    >
    <div className="card-title">{exam.title}</div>
    <div className="card-subtitle">{exam.totalQuestions} случайных вопросов</div>
    <div className="card-subtitle">{exam.timeMinutes} минут</div>
    {exam.maxMistakes !== undefined && (
      <div className="card-subtitle"> Не более {exam.maxMistakes} ошибок</div>
    )}
    </div>
);
};

export default TicketsListPage;
