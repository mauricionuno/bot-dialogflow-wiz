using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wiz.ChatBot.Domain.Models;

namespace Wiz.ChatBot.Domain.Interfaces.Services
{
    public interface IEquipeGestorWizService
    {
        Task<EquipeGestorModel> GetEquipeGestor(string cpf);
    }
}
