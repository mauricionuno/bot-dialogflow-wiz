using AutoMapper;
using System.Threading.Tasks;
using Wiz.ChatBot.API.Services.Interfaces;
using Wiz.ChatBot.API.ViewModels;
using Wiz.ChatBot.Domain.Interfaces.Services;

namespace Wiz.ChatBot.API.Services
{
    public class FuncionarioService : IFuncionarioService
    {
        private readonly IFuncionarioWizService _funcionarioWizService;
        private readonly IEquipeGestorWizService _equipeGestorWizService;
        private readonly IMapper _mapper;

        public FuncionarioService(IFuncionarioWizService funcionarioWizService, IEquipeGestorWizService equipeGestorWizService, IMapper mapper)
        {
            _funcionarioWizService = funcionarioWizService;
            _equipeGestorWizService = equipeGestorWizService;
            _mapper = mapper;
        }

        public async Task<FuncionarioViewModel> GetFuncionario(string cpf)
        {
            var funcionario = _mapper.Map<FuncionarioViewModel>(await _funcionarioWizService.GetFuncionario(cpf));
            if (funcionario != null)
            {
                return funcionario;
            }
            return null;
        }
    }
}
