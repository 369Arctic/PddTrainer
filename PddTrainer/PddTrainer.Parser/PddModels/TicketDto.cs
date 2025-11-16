using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PddTrainer.Parser.PddModels
{
    public class TicketDto
    {
        public string Title { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
