using AutoMapper;
using Core;
using Core.DTO;
using EventoWeb.Models;

namespace EventoWeb.Mappers;

public class PessoaProfile : Profile
{
    public PessoaProfile()
    {
        CreateMap<PessoaModel, Pessoa>()
            /*.ForMember(dest => dest.Foto, opt => opt.MapFrom(src => src.Foto != null ? FormFileToByteArray(src.Foto) : null))
            .ReverseMap()
            .ForMember(dest => dest.Foto, opt => opt.MapFrom(src => ByteArrayToFormFile(src.Foto, "foto.png")))
            .ForMember(dest => dest.FotoBase64, opt => opt.MapFrom(src => src.Foto != null ? Convert.ToBase64String(src.Foto) : null));
            */
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
            .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => src.NomeCracha))
            .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf))
            .ForMember(dest => dest.Sexo, opt => opt.MapFrom(src => src.Sexo))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Telefone1, opt => opt.MapFrom(src => src.Telefone1))
            .ForMember(dest => dest.Telefone2, opt => opt.MapFrom(src => src.Telefone2))
            .ForMember(dest => dest.Foto, opt => opt.Ignore()) // Assuming Foto is handled separately or ignored during this mapping
            .ForMember(dest => dest.Cep, opt => opt.MapFrom(src => src.Cep)) // Ensure this line exists
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado)) // Ensure this line exists
            .ForMember(dest => dest.Cidade, opt => opt.MapFrom(src => src.Cidade)) // Ensure this line exists
            .ForMember(dest => dest.Bairro, opt => opt.MapFrom(src => src.Bairro)) // Ensure this line exists
            .ForMember(dest => dest.Rua, opt => opt.MapFrom(src => src.Rua)) // Ensure this line exists
            .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Numero)) // Ensure this line exists
            .ForMember(dest => dest.Complemento, opt => opt.MapFrom(src => src.Complemento)); // Ensure this line exists
        
        CreateMap<Pessoa, PessoaModel>(); // Also create the reverse map
        CreateMap<Pessoa, ParticipanteDTO>(); // Based on ParticipanteService.cs, Pessoa needs to be mapped to ParticipanteDTO
        CreateMap<PessoaSimpleDTO, ParticipanteDTO>();
        CreateMap<PessoaSimpleDTO, PessoaModel>();
    }

    private static IFormFile ByteArrayToFormFile(byte[] byteArray, string fileName)
    {
        if (byteArray == null) return null;
        var stream = new MemoryStream(byteArray);
        return new FormFile(stream, 0, byteArray.Length, null, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private static byte[] FormFileToByteArray(IFormFile formFile)
    {
        if (formFile == null) return null;
        using (var memoryStream = new MemoryStream())
        {
            formFile.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
