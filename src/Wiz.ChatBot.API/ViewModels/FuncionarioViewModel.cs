using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wiz.ChatBot.API.ViewModels
{
    public class FuncionarioViewModel
    {
        public string nome { get; set; }
        public string sexo { get; set; }
        public string matricula { get; set; }
        public string cpf { get; set; }
        public string datainicio { get; set; }
        public string padrinho { get; set; }
        public string emailpadrinho { get; set; }
        public List<string> ferramentas { get; set; }
        public string localdetrabalho { get; set; }
        public string unidade { get; set; }
        public string email { get; set; }
        public string cpfgestor { get; set; }
        public string gestor { get; set; }
    }
}
