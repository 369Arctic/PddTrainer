namespace PddTrainer.Api.Models
{
    public class Theme
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public List<Question> Questions { get; set; } = new List<Question>();
    }
}
