using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Wiz.ChatBot.Domain.Interfaces.Services;
using Wiz.ChatBot.Domain.Models;

namespace Wiz.ChatBot.Infra.Services
{
    public class EquipeGestorWizService : IEquipeGestorWizService
    {
        private readonly IConfiguration _configuration;
        private HttpClient _httpClient;

        public EquipeGestorWizService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<EquipeGestorModel> GetEquipeGestor(string cpf)
        {
            var response = await _httpClient.GetAsync($"v2/5dbc3a6c31000011f44c0f2f");
            var stringResponse = await response.Content.ReadAsStringAsync();
            var equipeGestor = JsonSerializer.Deserialize<EquipeGestorModel>(stringResponse);

            return equipeGestor;
        }
    }
}
