namespace PddTrainer.Api.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string? ImageUrl { get; set; }
        public string? Explanation { get; set; }
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    }
}
