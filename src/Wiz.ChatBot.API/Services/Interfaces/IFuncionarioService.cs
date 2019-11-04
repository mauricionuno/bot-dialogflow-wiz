using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wiz.ChatBot.API.ViewModels;

namespace Wiz.ChatBot.API.Services.Interfaces
{
    public interface IFuncionarioService
    {
        Task<FuncionarioViewModel> GetFuncionario(string cpf);
    }
}
