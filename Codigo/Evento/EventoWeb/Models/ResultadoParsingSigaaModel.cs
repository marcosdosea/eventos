using Core.DTO;

namespace EventoWeb.Models
{
    /// <summary>
    /// Modelo de apresentação de erro sintático ou recusa de linha da planilha na camada MVC.
    /// </summary>
    public class ErroParsingSigaaDto
    {
        public int Linha { get; set; }

        public int NumeroLinha
        {
            get => Linha;
            set => Linha = value;
        }

        public string? ValorBruto { get; set; }

        public string MotivoRecusa { get; set; } = string.Empty;

        public string Mensagem
        {
            get => MotivoRecusa;
            set => MotivoRecusa = value;
        }
    }

    public class ErroParsingSigaaDTO : ErroParsingSigaaDto { }

    /// <summary>
    /// Modelo consolidado da camada MVC para transporte de dados importados com sucesso
    /// e erros de processamento para exibição na View / feedback ao usuário.
    /// </summary>
    public class ResultadoParsingSigaaDto
    {
        public List<PessoaInscritaDto> ListaSucesso { get; set; } = new();

        public List<ErroParsingSigaaDto> ListaErros { get; set; } = new();

        public bool Sucesso => ListaErros.Count == 0;

        public int TotalRegistros => ListaSucesso.Count + ListaErros.Count;
    }

    public class ResultadoParsingSigaaDTO : ResultadoParsingSigaaDto { }
}

