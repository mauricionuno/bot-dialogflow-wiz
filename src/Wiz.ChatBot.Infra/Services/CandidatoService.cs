using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Wiz.ChatBot.Domain.Interfaces.Services;

namespace Wiz.ChatBot.Infra.Services
{
    public class CandidatoService : ICandidatoService
    {
        private readonly IConfiguration _configuration;
        private HttpClient _httpClient;

        public CandidatoService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }
    }
}
