using System.Threading.Tasks;
using Wiz.ChatBot.Domain.Models.Services;

namespace Wiz.ChatBot.Domain.Interfaces.Services
{
    public interface IViaCEPService
    {
        Task<ViaCEP> GetByCEPAsync(string cep);
    }
}
