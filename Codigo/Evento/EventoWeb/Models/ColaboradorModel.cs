using Core;
using Core.DTO;

namespace EventoWeb.Models;

public class ColaboradorModel
{
    public PessoaModel Colaborador { get; set; }
    public int Id { get; set; }
    public string Nome { get; set; }
    public string NomeCracha { get; set; }
    public string Cpf { get; set; }
    public string Sexo { get; set; }
    public string Email { get; set; }
    public string Telefone1 { get; set; }
    public string Telefone2 { get; set; }
    public string Cep { get; set; }
    public string Estado { get; set; }
    public string Cidade { get; set; }
    public string Bairro { get; set; }
    public string Rua { get; set; }
    public string Numero { get; set; }
    public string Complemento { get; set; }

    public IEnumerable<PessoaSimpleDTO>? Colaboradores { get; set; }
}