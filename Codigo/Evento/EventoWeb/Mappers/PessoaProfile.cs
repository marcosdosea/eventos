using AutoMapper;
using EventoWeb.Models;
using Core;

namespace EventoWeb.Mappers;

public class PessoaProfile : Profile
{
    public PessoaProfile()
    {
        CreateMap<PessoaModel, Pessoa>()
        .ForMember(dest => dest.Foto, opt => opt.MapFrom(src => src.Foto != null ? FormFileToByteArray(src.Foto) : null))
        .ReverseMap()
        .ForMember(dest => dest.Foto, opt => opt.MapFrom(src => ByteArrayToFormFile(src.Foto, "foto.png")))
        .ForMember(dest => dest.FotoBase64, opt => opt.MapFrom(src => Convert.ToBase64String(src.Foto)));

    }

    private static IFormFile ByteArrayToFormFile(byte[] byteArray, string fileName)
    {
        var stream = new MemoryStream(byteArray);
        return new FormFile(stream, 0, byteArray.Length, null, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private static byte[] FormFileToByteArray(IFormFile formFile)
    {
        using (var memoryStream = new MemoryStream())
        {
            formFile.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
