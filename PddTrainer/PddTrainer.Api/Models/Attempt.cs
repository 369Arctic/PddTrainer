namespace PddTrainer.Api.Models
{
    public class Attempt
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public AttemptType Type { get; set; }

        // Заполняется если Type = Theme
        public int? ThemeId { get; set; }
        public Theme? Theme { get; set; }

        // Заполняется если Type = Ticket
        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        // Заполняется если Type = Exam
        public string? ExamModeId { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int Mistakes { get; set; }
        public bool Passed { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }

        public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
    }
}
