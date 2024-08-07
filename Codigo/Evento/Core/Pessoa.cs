using System;
using System.Collections.Generic;

namespace Core;

public partial class Pessoa
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public string NomeCracha { get; set; } = null!;

    public string Cpf { get; set; } = null!;

    /// <summary>
    /// M - Masculino
    /// F - Feminino
    /// N - Não Informado
    /// </summary>
    public string Sexo { get; set; } = null!;

    public string? Cep { get; set; }

    public string? Rua { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? Estado { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string Email { get; set; } = null!;

    public string? Telefone1 { get; set; }

    public string? Telefone2 { get; set; }

    public byte[]? Foto { get; set; }

    public virtual Estadosbrasil? EstadoNavigation { get; set; }

    public virtual ICollection<Inscricaopessoaevento> Inscricaopessoaeventos { get; set; } = new List<Inscricaopessoaevento>();

    public virtual ICollection<Inscricaopessoasubevento> Inscricaopessoasubeventos { get; set; } = new List<Inscricaopessoasubevento>();

    public virtual ICollection<Participacaopessoaevento> Participacaopessoaeventos { get; set; } = new List<Participacaopessoaevento>();

    public virtual ICollection<Participacaopessoasubevento> Participacaopessoasubeventos { get; set; } = new List<Participacaopessoasubevento>();

    public virtual ICollection<Pessoaareainteresse> Pessoaareainteresses { get; set; } = new List<Pessoaareainteresse>();
}
