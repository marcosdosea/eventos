using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventoWeb.Models
{
    public class PessoaCreateModel
    {
        public PessoaModel? Pessoa{ get; set; }
        public SelectList? Estados { get; set; }
    }
}