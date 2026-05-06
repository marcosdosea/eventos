namespace EventoWeb.Models
{
    public class GestaoAdministradorModel
    {
        public PessoaModel Administrador { get; set; } = null!;
        public List<PessoaModel> ListaAdministradores { get; set; } = null!;
    }
}
