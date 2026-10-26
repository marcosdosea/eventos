using Core.DTO;
using System.ComponentModel.DataAnnotations;

namespace EventoWeb.Models
{
    public class EmitirCrachaModel
    {
        public uint IdEvento { get; set; }

        [Display(Name = "Evento Vinculado")]
        public string NomeEvento { get; set; } = string.Empty;

        public string? SubtituloEvento { get; set; }

        public uint? IdModelo { get; set; }

        [Display(Name = "Gabarito / Modelo do Crachá")]
        public string NomeModelo { get; set; } = "Modelo Padrão Vertical 9×13cm";

        public string? TextoModelo { get; set; }

        public string? LogotipoDataUrl { get; set; }

        public bool QrcodeHabilitado { get; set; } = true;

        public List<EventoSimpleDTO> EventosDisponiveis { get; set; } = new();

        public List<ModelocrachaItemDTO> ModelosDisponiveis { get; set; } = new();

        public List<ParticipanteCrachaItemModel> Participantes { get; set; } = new();

        public int TotalParticipantes => Participantes.Count;

        public int TotalAptos => Participantes.Count(p => p.StatusApto);

        public int TotalSelecionados => Participantes.Count(p => p.Selecionado);
    }

    public class ModelocrachaItemDTO
    {
        public uint Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Texto { get; set; }
        public bool TemLogotipo { get; set; }
        public sbyte Qrcode { get; set; }
    }

    public class ParticipanteCrachaItemModel
    {
        public uint IdInscricao { get; set; }
        public uint IdPessoa { get; set; }
        public string NomeCompleto { get; set; } = string.Empty;
        public string NomeCracha { get; set; } = string.Empty;
        public string? OrganizacaoOuDepto { get; set; }
        public string CpfPuro { get; set; } = string.Empty;
        public string CpfMascarado { get; set; } = string.Empty;
        public string Papel { get; set; } = "PARTICIPANTE";
        public string TipoInscricao { get; set; } = "Geral";
        public string CodigoCredencial { get; set; } = string.Empty;
        public string CodigoPresenca { get; set; } = string.Empty;
        public bool StatusApto { get; set; } = true;
        public string StatusDescricao { get; set; } = "Apto";
        public string? FotoDataUrl { get; set; }
        public string QrCodeBase64 { get; set; } = string.Empty;
        public bool Selecionado { get; set; } = true;
    }
}
