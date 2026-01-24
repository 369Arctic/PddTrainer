import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import TicketsListPage from "./pages/TicketsListPage";
import TicketPage from "./pages/TicketPage";
import TicketResultPage from "./pages/TicketResultPage";

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<TicketsListPage />} />
        <Route path="/ticket/:id" element={<TicketPage />} />
        <Route path="/theme/:themeId" element={<TicketPage />} />
        <Route path="/ticket/result" element={<TicketResultPage />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;