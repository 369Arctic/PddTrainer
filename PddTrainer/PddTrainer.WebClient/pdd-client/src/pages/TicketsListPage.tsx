import React, { useEffect, useState } from "react";
import { getAllTickets } from "../api/tickets";
import type { Ticket } from "../types/models";
import { useNavigate } from "react-router-dom";

const TicketsListPage: React.FC = () => {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchTickets = async () => {
      try {
        const data = await getAllTickets();
        setTickets(data);
      } catch (err) {
        console.error(err);
        setError("Ошибка при загрузке билетов");
      } finally {
        setLoading(false);
      }
    };

    fetchTickets();
  }, []);

  if (loading) return <div>Загрузка билетов...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div style={{ padding: "20px" }}>
      <h1>Список билетов ПДД</h1>
      <div style={{ display: "flex", flexWrap: "wrap", gap: "10px" }}>
        {tickets.map((ticket) => (
          <div
            key={ticket.id}
            style={{
              border: "1px solid #ccc",
              borderRadius: "8px",
              padding: "10px",
              width: "200px",
              cursor: "pointer",
              boxShadow: "2px 2px 5px rgba(0,0,0,0.1)",
            }}
            onClick={() => navigate(`/ticket/${ticket.id}`)}
          >
            <h3>{ticket.title}</h3>
            <p>Вопросов: {ticket.questions.length}</p>
          </div>
        ))}
      </div>
    </div>
  );
};

export default TicketsListPage;