using AutoMapper;
using EventoWeb.Models;
using Core;
using Core.DTO;

namespace EventoWeb.Mappers;

public class PessoaProfile : Profile
{
    public PessoaProfile()
    {
        CreateMap<PessoaModel, Pessoa>()
            .ForMember(dest => dest.Foto, opt => opt.MapFrom(src => src.Foto != null ? FormFileToByteArray(src.Foto) : null))
            .ReverseMap()
            .ForMember(dest => dest.Foto, opt => opt.MapFrom(src => ByteArrayToFormFile(src.Foto, "foto.png")))
            .ForMember(dest => dest.FotoBase64, opt => opt.MapFrom(src => src.Foto != null ? Convert.ToBase64String(src.Foto) : null));

        CreateMap<PessoaSimpleDTO, PessoaModel>()
            .ForMember(dest => dest.Foto, opt => opt.Ignore())
            .ForMember(dest => dest.FotoBase64, opt => opt.Ignore());

        CreateMap<ColaboradorDTO, PessoaModel>()
            .ForMember(dest => dest.Foto, opt => opt.Ignore())
            .ForMember(dest => dest.FotoBase64, opt => opt.Ignore());
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
