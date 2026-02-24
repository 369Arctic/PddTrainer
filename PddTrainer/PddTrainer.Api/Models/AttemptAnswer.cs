namespace PddTrainer.Api.Models
{
    public class AttemptAnswer
    {
        public int Id { get; set; }
        public int AttemptId { get; set; }
        public Attempt Attempt { get; set; } = null;
        public int QuestionId { get; set; }
        public Question Question { get; set; } = null;
        public int SelectedAnswerId { get; set; }
        public bool IsCorrect { get; set; }
    }
}
