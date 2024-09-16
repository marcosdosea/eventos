using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Util;
using Core.DTO;

namespace EventoWeb.Models
{
    public class ModeloCertificadoModel
    {
        [Display(Name = "Código")]
        [Required]
        [Key]
        public uint Id { get; set; }

        [Display(Name = "Logotipo superior")]
        [Required(ErrorMessage = "Insira um logotipo")]
        [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]
        public IFormFile LogotipoSuperior { get; set; } = null!;

        [Display(Name = "Logotipo superior")]
        [BindNever]
        public string? LogotipoSuperiorBase64 { get; set; } = null!;

        [Display(Name = "Texto antes participante")]
        [Required]
        public string TextoAntesParticipante { get; set; } = null!;

        [Display(Name = "Texto antes evento")]
        [Required]
        public string TextoAntesEvento {  get; set; } = null!;

        [Display(Name = "Texto antes carga horária")]
        [Required]
        public string TextoAntesCargaHoraria { get; set; } = null!;

        [Display(Name = "Assinatura 1 texto")]
        [Required]
        public string Assinatura1Texto {  get; set; } = null!;

        [Display(Name = "Assinatura 1")]
        [Required(ErrorMessage = "Insira uma assinatura")]
        [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]
        public IFormFile Assinatura1 { get; set; } = null!;

        [Display(Name = "Assinatura 1")]
        [BindNever]
        public string? Assinatura1Base64 {  get; set; }

        [Display(Name = "Assinatura 2 texto")]
        public string? Assinatura2Texto { get; set; }

        [Display(Name = "Assinatura 2")]
        [ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]
        public IFormFile Assinatura2 { get; set; }

        [Display(Name = "Assinatura 2")]
        public string? Assinatura2Base64 { get; set; }

        [Required]
        public uint IdEvento {  get; set; }

        public virtual EventoModel IdEventoNavigation { get; set; } = null!;
    }
}
