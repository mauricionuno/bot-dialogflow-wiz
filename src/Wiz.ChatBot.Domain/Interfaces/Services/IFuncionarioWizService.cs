using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wiz.ChatBot.Domain.Models;

namespace Wiz.ChatBot.Domain.Interfaces.Services
{
    public interface IFuncionarioWizService
    {
        Task<FuncionarioModel> GetFuncionario(string cpf);
    }
}
