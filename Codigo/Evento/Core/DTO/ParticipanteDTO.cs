namespace Core.DTO
{
    public class ParticipanteDTO
    {
        public uint Id { get; set; } // Identificador unico

        public string Cpf { get; set; } = null!; // Identificador do participante (CPF)

        public string Nome { get; set; } = null!; // Nome do participante

        public string Sexo { get; set; } = null!; // Sexo do participante (M, F, N)

        public string Email { get; set; } = null!; // Email do participante

        public string? Telefone1 { get; set; } // Telefone principal

        public string? Telefone2 { get; set; } // Telefone secundario

        // Campos adicionais especificos para participante
        public bool IsActive { get; set; } // Indica se o participante esta ativo

        public DateTime? RegistrationDate { get; set; } // Data de registro do participante

        public DateTime? LastLogin { get; set; } // ultimo login do participante
    }
}