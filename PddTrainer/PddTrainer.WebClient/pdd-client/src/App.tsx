import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import TicketsListPage from "./pages/TicketsListPage";
import TicketPage from "./pages/TicketPage";

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<TicketsListPage />} />
        <Route path="/ticket/:id" element={<TicketPage />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;