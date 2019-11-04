using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Wiz.ChatBot.Domain.Interfaces.Services;
using Wiz.ChatBot.Domain.Models;

namespace Wiz.ChatBot.Infra.Services
{
    public class FuncionarioWizService : IFuncionarioWizService
    {
        private readonly IConfiguration _configuration;
        private HttpClient _httpClient;

        public FuncionarioWizService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<FuncionarioModel> GetFuncionario(string cpf)
        {
            var response = await _httpClient.GetAsync($"v2/5dbc62b2310000d6094c0fe3");
            var stringResponse = await response.Content.ReadAsStringAsync();
            var funcionario = JsonSerializer.Deserialize<ICollection<FuncionarioModel>>(stringResponse);

            return funcionario.Where(c=>c.cpf == cpf).FirstOrDefault();
        }
    }
}
