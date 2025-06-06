using Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class ParticipanteModel
    {
        public PessoaModel Participante { get; set; }

        public ParticipanteModel()
        {
            Participante = new PessoaModel();
        }
    }
} 