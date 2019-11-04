using System.Collections.Generic;

namespace Wiz.ChatBot.Domain.Models
{
    public class EquipeGestorModel
    {
        public string cpfgestor { get; set; }
        public string gestor { get; set; }
        public string nomeequipe { get; set; }
        public List<EquipeModel> Equipe { get; set; }
    }
}
