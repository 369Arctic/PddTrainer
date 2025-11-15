namespace PddTrainer.Api.Models.DTO
{
    public class TicketDto
    {
        public int? Id { get; set; }
        public string Title { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
