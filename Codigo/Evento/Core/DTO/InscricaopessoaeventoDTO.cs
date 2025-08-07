public class InscricaopessoaeventoDTO
{
    public uint Id { get; set; }
    public uint IdPessoa { get; set; }
    public uint IdEvento { get; set; }
    public int IdPapel { get; set; }
    public uint? IdTipoInscricao { get; set; }
    public DateTime DataInscricao { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = null!;
    public decimal FrequenciaFinal { get; set; }
    public string? NomeCracha { get; set; }
    // Adicione outros campos se precisar (ex: nomes de navegação)
}