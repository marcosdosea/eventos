using AutoMapper;
using Core;
using EventoWeb.Models;

namespace EventoWeb.Mappers;

public class EventoProfile : Profile
{
	public EventoProfile()
	{
		CreateMap<EventoModel, Evento>()
		.ForMember(dest => dest.IdAreaInteresses, opt => opt.Ignore())
		.ForMember(dest => dest.ImagemPortal, opt => opt.MapFrom(src => src.ImagemPortal != null ? FormFileToByteArray(src.ImagemPortal) : null))
		.ReverseMap()
        .ForMember(dest => dest.ImagemPortal, opt => opt.Ignore())
        .ForMember(dest => dest.ImagemPortalBase64, opt => opt.MapFrom(src => src.ImagemPortal != null ? Convert.ToBase64String(src.ImagemPortal) : string.Empty));

    }

	private static IFormFile ByteArrayToFormFile(byte[] byteArray, string fileName)
    {
        if (byteArray == null || byteArray.Length == 0)
        {
            var emptyStream = new MemoryStream();
            return new FormFile(emptyStream, 0, 0, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }

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

