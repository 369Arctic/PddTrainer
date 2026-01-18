import { http } from "./http";
import type { Theme } from "../types/models";

// Получить список всех тем
export async function getAllThemes(): Promise<Theme[]> {
  const response = await http.get<Theme[]>("/Themes");
  return response.data;
}
