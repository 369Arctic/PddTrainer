import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_URL;

export type SubmitAttemptDto = {
  type: number;
  themeId?: number;
  ticketId?: number;
  examModeId?: string;
  startedAt: string;
  answers: {
    questionId: number;
    selectedAnswerId: number;
  }[];
};

export const submitAttempt = async (dto: SubmitAttemptDto) => {
  const response = await axios.post(
    `${API_BASE_URL}/api/attempts/submit`,
    dto,
    {
      withCredentials: true
    }
  );

  return response.data;
};