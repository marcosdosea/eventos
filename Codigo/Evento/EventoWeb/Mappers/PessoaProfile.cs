using AutoMapper;
using Core;
using Core.DTO;
using EventoWeb.Models;
using Microsoft.AspNetCore.Http;

namespace EventoWeb.Mappers;

public class PessoaProfile : Profile
{
    public PessoaProfile()
    {
        CreateMap<PessoaModel, Pessoa>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
            .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => src.NomeCracha))
            .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf))
            .ForMember(dest => dest.Sexo, opt => opt.MapFrom(src => src.Sexo))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Telefone1, opt => opt.MapFrom(src => src.Telefone1))
            .ForMember(dest => dest.Telefone2, opt => opt.MapFrom(src => src.Telefone2))
            .ForMember(dest => dest.Cep, opt => opt.MapFrom(src => src.Cep))
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado))
            .ForMember(dest => dest.Cidade, opt => opt.MapFrom(src => src.Cidade))
            .ForMember(dest => dest.Bairro, opt => opt.MapFrom(src => src.Bairro))
            .ForMember(dest => dest.Rua, opt => opt.MapFrom(src => src.Rua))
            .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Numero))
            .ForMember(dest => dest.Complemento, opt => opt.MapFrom(src => src.Complemento))
            .ForMember(dest => dest.Foto, opt => opt.MapFrom(src => src.Foto != null ? FormFileToByteArray(src.Foto) : null));

        CreateMap<Pessoa, PessoaModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
            .ForMember(dest => dest.NomeCracha, opt => opt.MapFrom(src => src.NomeCracha))
            .ForMember(dest => dest.Cpf, opt => opt.MapFrom(src => src.Cpf))
            .ForMember(dest => dest.Sexo, opt => opt.MapFrom(src => src.Sexo))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Telefone1, opt => opt.MapFrom(src => src.Telefone1))
            .ForMember(dest => dest.Telefone2, opt => opt.MapFrom(src => src.Telefone2))
            .ForMember(dest => dest.Cep, opt => opt.MapFrom(src => src.Cep))
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado))
            .ForMember(dest => dest.Cidade, opt => opt.MapFrom(src => src.Cidade))
            .ForMember(dest => dest.Bairro, opt => opt.MapFrom(src => src.Bairro))
            .ForMember(dest => dest.Rua, opt => opt.MapFrom(src => src.Rua))
            .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Numero))
            .ForMember(dest => dest.Complemento, opt => opt.MapFrom(src => src.Complemento))
            .ForMember(dest => dest.Foto, opt => opt.Ignore()) // Ignorar a propriedade Foto no mapeamento reverso
            .ForMember(dest => dest.FotoBase64, opt => opt.MapFrom(src => src.Foto != null ? Convert.ToBase64String(src.Foto) : null));

        CreateMap<Pessoa, ParticipanteDTO>();
        CreateMap<PessoaSimpleDTO, ParticipanteDTO>();
        CreateMap<PessoaSimpleDTO, PessoaModel>();
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
