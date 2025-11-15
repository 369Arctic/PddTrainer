namespace PddTrainer.Api.Models.DTO
{
    public class QuestionDto
    {
        public int? Id { get; set; }
        public string Text { get; set; }
        public string? ImageUrl { get; set; }
        public string? Explanation { get; set; }
        public List<AnswerOptionDto> AnswerOptions { get; set; } = new();
    }
}
