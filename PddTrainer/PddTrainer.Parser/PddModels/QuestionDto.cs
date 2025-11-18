using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PddTrainer.Parser.PddModels
{
    public class QuestionDto
    {
        public string Text { get; set; }
        public string? ImageUrl { get; set; }
        public string? Explanation { get; set; }
        public List<AnswerOptionDto> AnswerOptions { get; set; } = new();
    }
}
