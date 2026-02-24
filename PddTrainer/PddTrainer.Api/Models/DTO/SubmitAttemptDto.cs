namespace PddTrainer.Api.Models.DTO
{
    public class SubmitAttemptDto
    {
        public AttemptType Type { get; set; }
        public int? ThemeId { get; set; }
        public int? TicketId { get; set; }
        public string? ExamModeId { get; set; }
        public DateTime StartedAt { get; set; }
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }
}
