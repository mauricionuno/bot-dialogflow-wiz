using AutoMapper;
using System.Diagnostics.CodeAnalysis;
using Wiz.ChatBot.API.ViewModels;
using Wiz.ChatBot.Domain.Models;

namespace Wiz.ChatBot.API.AutoMapper
{
    [ExcludeFromCodeCoverage]
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<FuncionarioModel, FuncionarioViewModel>().ReverseMap();
        }
    }
}
