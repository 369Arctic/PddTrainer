namespace PddTrainer.Api.Models.DTO
{
    public class ExamDto
    {
        public string Id {  get; set; }
        public string Title { get; set; }
        public int TimeMinutes { get; set; }
        public int? MaxMistakes { get; set; }
        public int? ExtraQuestionsForMistakes { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
