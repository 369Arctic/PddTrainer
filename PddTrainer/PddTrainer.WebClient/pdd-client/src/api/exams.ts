import axios from "axios";

const API_URL = "https://localhost:7269/api/exams";

export const getExamById = async (examId: string) => {
  const response = await axios.get(`${API_URL}/${examId}`);
  return response.data;
};
