using PddTrainer.Api.Models.DTO;
using PddTrainer.Api.Models;
using AutoMapper;

namespace PddTrainer.Api.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Ticket, TicketDto>().ReverseMap();
            CreateMap<Question, QuestionDto>().ReverseMap();
            CreateMap<AnswerOption, AnswerOptionDto>().ReverseMap();
            CreateMap<Theme, ThemeDto>().ReverseMap();
        }
    }
}
