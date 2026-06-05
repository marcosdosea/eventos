using AutoMapper;
using Core;
using Core.DTO;
using EventoWeb.Models;

namespace EventoWeb.Mappers
{
    /// <summary>
    /// Mapeia Pessoa ↔ PessoaModel e Pessoa ↔ PessoaSimpleDTO.
    /// ColaboradorDTO e ParticipanteDTO foram eliminados (eram duplicatas de PessoaSimpleDTO).
    /// </summary>
    public class PessoaProfile : Profile
    {
        public PessoaProfile()
        {
            // PessoaModel → Pessoa
            CreateMap<PessoaModel, Pessoa>()
                .ForMember(dest => dest.Foto,
                    opt => opt.MapFrom(src =>
                        src.Foto != null ? FormFileToByteArray(src.Foto) : null));

            // Pessoa → PessoaModel
            CreateMap<Pessoa, PessoaModel>()
                .ForMember(dest => dest.Foto, opt => opt.Ignore())
                .ForMember(dest => dest.FotoBase64,
                    opt => opt.MapFrom(src =>
                        src.Foto != null ? Convert.ToBase64String(src.Foto) : null))
                .ForMember(dest => dest.Estados, opt => opt.Ignore());

            // Pessoa → PessoaDTO
            CreateMap<Pessoa, PessoaDTO>();

            // Pessoa → PessoaSimpleDTO
            // (substitui ColaboradorDTO e ParticipanteDTO — eram idênticos)
            CreateMap<Pessoa, PessoaSimpleDTO>();

            // PessoaSimpleDTO → PessoaModel (usado em listas de seleção)
            CreateMap<PessoaSimpleDTO, PessoaModel>()
                .ForMember(dest => dest.Foto, opt => opt.Ignore())
                .ForMember(dest => dest.Estados, opt => opt.Ignore());
        }

        private static byte[]? FormFileToByteArray(IFormFile formFile)
        {
            if (formFile == null) return null;
            using var ms = new MemoryStream();
            formFile.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
