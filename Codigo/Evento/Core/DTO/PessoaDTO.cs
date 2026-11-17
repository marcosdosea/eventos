namespace Core.DTO;

public class PessoaDTO
{
    public uint Id { get; set; }
    
    public string Nome { get; set; } = null!;
    
    public string NomeCracha { get; set; } = null!;
    
    public string Sexo { get; set; } = null!;
    
    public string Email { get; set; } = null!;

    public string? Telefone1 { get; set; }

    public string? Telefone2 { get; set; }

}

public class PessoaSimpleDTO
{
    public string Cpf { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Telefone1 { get; set; }
}

public class PessoaInscritaDto
{
    public string Cpf { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? TipoParticipacao { get; set; }

    public string? TipoParticipação
    {
        get => TipoParticipacao;
        set => TipoParticipacao = value;
    }
}

