namespace Core.DTO
{
    public class ColaboradorDTO
    {
        public uint Id { get; set; } // Identificador �nico

        public string Cpf { get; set; } = null!; // Identificador do colaborador

        public string Nome { get; set; } = null!; // Nome do colaborador

        public string NomeCracha { get; set; } = null!; // Nome para crach�

        public string Sexo { get; set; } = null!; // Sexo do colaborador

        public string Email { get; set; } = null!; // Email do colaborador

        public string? Telefone1 { get; set; } // Telefone principal

        public string? Telefone2 { get; set; } // Telefone secund�rio

        // Campos adicionais espec�ficos para colaborador
        public bool IsActive { get; set; } // Indica se o colaborador est� ativo

        public DateTime? RegistrationDate { get; set; } // Data de registro do colaborador

        public DateTime? LastLogin { get; set; } // �ltimo login do colaborador
    }
}