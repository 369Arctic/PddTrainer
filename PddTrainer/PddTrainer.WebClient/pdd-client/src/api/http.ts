import axios from "axios";

export const http = axios.create({
  baseURL: "https://localhost:7269/api",
  headers: {
    "Content-Type": "application/json",
  },
});