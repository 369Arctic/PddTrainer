namespace PddTrainer.Api.Models
{
    public class ExamMode
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int TotalQuestions { get; set; }
        public int TimeMinutes { get; set; }
        public int? MaxMistakes { get; set; }
        public int? ExtraQuestionsForMistakes { get; set; }
    }

    public class ExamSettings
    {
        public List<ExamMode> Exams { get; set; } = new();
    }
}
