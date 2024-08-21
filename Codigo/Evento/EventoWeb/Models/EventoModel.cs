using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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

		[Display(Name = "Data Inicial")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
		public DateTime? DataInicio { get; set; } = DateTime.MinValue;

		[Display(Name = "Data Final")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
		public DateTime? DataFim { get; set; } = DateTime.MinValue;

		[Display(Name = "Descrição")]
		public string? Descricao { get; set; }

		[Display(Name = "Inscrição Gratuita")]
		public sbyte InscricaoGratuita { get; set; }

		[Display(Name = "Status")]
		[Required(ErrorMessage = "Status do Evento é obrigatório")]
		public string Status { get; set; } = null!;

		[Display(Name = "Data Inicial de Inscrição")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
		public DateTime? DataInicioInscricao { get; set; }

		[Display(Name = "Data Final de Inscrição")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
		public DateTime? DataFimInscricao { get; set; }

		[Display(Name = "Valor da Inscrição", Prompt = "R$ 00.00")]
		[Range(0.00, double.MaxValue, ErrorMessage = "O valor da inscrição deve ser zero ou maior que zero.")]
		[RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Por favor, insira no máximo duas casas decimais e use '.' como separador decimal.")]
		public decimal ValorInscricao { get; set; }

		[Display(Name = "Website")]
		public string? Website { get; set; }

		[Display(Name = "e-mail")]
		public string? EmailEvento { get; set; }

		[Display(Name = "Evento Publico")]
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

		[Display(Name = "Frequência Minima para Receber a Certificação")]
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
		
		public SelectList Estados { get; set; }
		
		public SelectList TiposEventos { get; set; }
		
		public SelectList AreaInteresse { get; set; }
	}
}
