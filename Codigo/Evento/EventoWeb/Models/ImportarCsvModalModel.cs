namespace EventoWeb.Models
{
    public class ImportarCsvModalModel
    {
        public uint IdEvento { get; set; }
        public uint? IdSubEvento { get; set; }
        public string ActionRetorno { get; set; } = "Index";
        public string Titulo { get; set; } = "Importar Planilha de Participantes";
        public string Descricao { get; set; } = "Selecione o arquivo de planilha (formatos .csv, .xlsx ou .xls, como o relatório do SIGAA) para importar os participantes e registrar a presença.";
        public string MensagemCarregamento { get; set; } = "Processando planilha e registrando presença, aguarde...";
    }
}
