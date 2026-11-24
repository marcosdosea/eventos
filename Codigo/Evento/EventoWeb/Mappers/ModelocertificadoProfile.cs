using AutoMapper;
using Core;
using EventoWeb.Models;
using System;
using System.Globalization;

namespace EventoWeb.Mappers
{
    public class ModelocertificadoProfile : Profile
    {
        public ModelocertificadoProfile()
        {
            CreateMap<Modelocertificado, ModelocertificadoModel>()
                .ForMember(dest => dest.NomeEvento, opt => opt.MapFrom(src =>
                    src.IdEventoNavigation != null ? src.IdEventoNavigation.Nome : string.Empty))
                .ForMember(dest => dest.Codigo, opt => opt.MapFrom(src =>
                    src.TextoAntesParticipante ?? string.Empty))
                .ForMember(dest => dest.DataEmissao, opt => opt.MapFrom(src =>
                    ParseDataEmissao(src.Assinatura1Texto)))
                .ForMember(dest => dest.Eventos, opt => opt.Ignore());

            CreateMap<ModelocertificadoModel, Modelocertificado>()
                .ForMember(dest => dest.IdEventoNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.LogotipoSuperior, opt => opt.MapFrom(_ => Array.Empty<byte>()))
                .ForMember(dest => dest.Assinatura1, opt => opt.MapFrom(_ => Array.Empty<byte>()))
                .ForMember(dest => dest.Assinatura2, opt => opt.Ignore())
                .ForMember(dest => dest.Assinatura2Texto, opt => opt.Ignore())
                .ForMember(dest => dest.TextoAntesParticipante, opt => opt.MapFrom(src =>
                    NormalizarTexto(src.Codigo, "Certificamos que", 500)))
                .ForMember(dest => dest.TextoAntesEvento, opt => opt.MapFrom(_ => "participou do evento"))
                .ForMember(dest => dest.TextoAntesCargaHoraria, opt => opt.MapFrom(_ => "com carga horária de"))
                .ForMember(dest => dest.Assinatura1Texto, opt => opt.MapFrom(src =>
                    src.DataEmissao.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
        }

        private static DateTime ParseDataEmissao(string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor) &&
                DateTime.TryParseExact(valor.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            {
                return data;
            }
            return DateTime.Today;
        }

        private static string NormalizarTexto(string? texto, string padrao, int maxLen)
        {
            var limpo = string.IsNullOrWhiteSpace(texto) ? padrao : texto.Trim();
            return limpo.Length > maxLen ? limpo.Substring(0, maxLen) : limpo;
        }
    }
}
