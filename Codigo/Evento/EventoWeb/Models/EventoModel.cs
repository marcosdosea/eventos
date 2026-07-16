using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Util;

namespace EventoWeb.Models
{
	public class EventoModel
	{
		[Display(Name = "Código")]
		[Required(ErrorMessage = "Código do Evento é obrigatório")]
		[Key]
		public uint Id { get; set; }

		[Display(Name = "Nome")]
		[Required(ErrorMessage = "Nome do Evento é obrigatório")]
		public string Nome { get; set; } = null!;

		[Display(Name = "Data Inicio")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
		[DataInicio(nameof(DataFim))]
		public DateTime? DataInicio { get; set; } = DateTime.Today;

        [Display(Name = "Data Fim")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
		public DateTime? DataFim { get; set; } = DateTime.Today;

		[Display(Name = "Descrição")]
		public string? Descricao { get; set; }

		[Display(Name = "Gratuito")]
		public sbyte InscricaoGratuita { get; set; }

		[Display(Name = "Status")]
		[Required(ErrorMessage = "Status do Evento é obrigatório")]
		public string Status { get; set; } = null!;


		[Required(ErrorMessage = "A data e hora inicial de inscrição são obrigatórias.")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
		[DataInicio(nameof(DataFimInscricao))]
		public DateTime? DataInicioInscricao { get; set; } = DateTime.Today;

		[Required(ErrorMessage = "A data e hora final de inscrição são obrigatórias.")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
		public DateTime? DataFimInscricao { get; set; } = DateTime.Today.AddDays(7).AddHours(23).AddMinutes(59);

        [Display(Name = "Menor Valor de Inscrição", Prompt = "R$ 00,00")]
        [Range(0.00, 999999, ErrorMessage = "O valor da inscrição deve ser zero ou maior que zero.")]
        public decimal ValorInscricao { get; set; }

        [Display(Name = "Website")]
		public string? Website { get; set; }

		[Display(Name = "E-mail Evento")]
		public string? EmailEvento { get; set; }

		[Display(Name = "Publicar Portal")]
		public sbyte EventoPublico { get; set; }

		[Display(Name = "CEP")]
		[RegularExpression(@"^\d{8}$", ErrorMessage = "O CEP deve estar no formato 00000000.")]
		[StringLength(8, MinimumLength = 8, ErrorMessage = "O campo CEP deve ter 8 caracteres")]
		public string? Cep { get; set; }

		[Display(Name = "Estado")]
		public string? Estado { get; set; }

		[Display(Name = "Cidade")]
		public string? Cidade { get; set; }

		[Display(Name = "Bairro")]
		public string? Bairro { get; set; }

		[Display(Name = "Rua")]
		public string? Rua { get; set; }

		[Display(Name = "Numero")]
		public string? Numero { get; set; }

		[Display(Name = "Complemento")]
		public string? Complemento { get; set; }

		[Display(Name = "Há Certificação?")]
		[Required(ErrorMessage = "Informe se há Certificação")]
		public sbyte PossuiCertificado { get; set; }

		[Display(Name = "Frequência Minima Emissão Certificado")]
        [Range(0.00, 100, ErrorMessage = "A frequencia minima deve ser entre 0 e 100.")]
        public decimal FrequenciaMinimaCertificado { get; set; }

		[Display(Name = "ID do Tipo do Evento")]
		[Required(ErrorMessage = "Informe o Tipo do Evento")]
		public uint IdTipoEvento { get; set; }

		[Display(Name = "Vagas Ofertadas")]
		[Required(ErrorMessage = "Informe a quantidade de Vagas Ofertadas para este evento")]
		public int VagasOfertadas { get; set; }

		[Display(Name = "Vagas Reservadas")]
		[Required(ErrorMessage = "Informe a quantidade de Vagas Reservadas")]
		public int VagasReservadas { get; set; }

		[Display(Name = "Vagas Disponíveis")]
		[Required(ErrorMessage = "Informe a quantidade de Vagas Disponíveis")]
		public int VagasDisponiveis { get; set; }

		[Display(Name = "Nome do Tipo do Evento")]
		public string? NomeTipoEvento { get; set; }

		[Display(Name = "ID da Área de Interesse")]
		public uint IdAreaInteresse { get; set; }

		[Display(Name = "Nome da Área de Interesse")]
		public string? NomeAreaInteresse { get; set; }

		[Display(Name = "Nome do Estado")]
		public string? NomeEstado { get; set; }

		[Display(Name = "Tempo de Reserva em Minutos")]
		[Required(ErrorMessage = "Informe o Tempo de Reserva de uma Vaga")]
		public int TempoMinutosReserva { get; set; }

		[Display(Name = "Carga Horária")]
		[Required(ErrorMessage = "Informe a Carga Horária do Evento")]
		public int CargaHoraria { get; set; }

		[Display(Name = "Áreas de Interesse")]
		public List<uint> IdAreaInteresses { get; set; } = new List<uint>();

		[Display(Name = "Foto")]
		[ImagemUpload(ErrorMessage = "A imagem deve estar nos formatos PNG, JPG, JPEG, TIF ou GIF e ter menos de 1 MB.")]
		public IFormFile? ImagemPortal { get; set; }

		[Display(Name = "Foto")]
		[BindNever]
		public string? ImagemPortalBase64 { get; set; }

		[ValidateNever] //remover as validações para os metodos post
		public SelectList Estados { get; set; }

		[ValidateNever]
		public SelectList TiposEventos { get; set; }
		
		[ValidateNever]
		public SelectList AreaInteresse { get; set; }
	}
}
