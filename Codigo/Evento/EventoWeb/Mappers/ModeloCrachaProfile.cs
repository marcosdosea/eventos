using AutoMapper;
using Core;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

public class ModeloCrachaProfile : Profile
{
    public ModeloCrachaProfile()
    {
        CreateMap<ModelocrachaModel, Modelocracha>()
        .ForMember(dest => dest.Logotipo, opt => opt.MapFrom(src => src.Logotipo != null ? FormFileToByteArray(src.Logotipo) : null))
        .ReverseMap()
        .ForMember(dest => dest.Logotipo, opt => opt.MapFrom(src => ByteArrayToFormFile(src.Logotipo, "logotipo.png")))
        .ForMember(dest => dest.LogotipoBase64, opt => opt.MapFrom(src => Convert.ToBase64String(src.Logotipo)));

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