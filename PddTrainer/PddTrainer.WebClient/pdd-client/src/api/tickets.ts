import { http } from "./http";
import type { Ticket } from "../types/models";

// Получить список всех билетов
export async function getAllTickets(): Promise<Ticket[]> {
  const response = await http.get<Ticket[]>("/Tickets");
  return response.data;
}

// Получить один билет по ID
export async function getTicketById(id: number): Promise<Ticket> {
  const response = await http.get<Ticket>(`/Tickets/${id}`);
  return response.data;
}
