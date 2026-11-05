using Core;
using Microsoft.EntityFrameworkCore;

namespace Service;

/// <summary>Atualiza os resultados das inscrições sem fazer SaveChanges.</summary>
internal static class AtualizadorFrequencia
{
    private const int Participante = 4;

    public static void Evento(EventoContext context, Evento evento, uint? pessoa = null)
    {
        var inscricoes = context.Inscricaopessoaeventos
            .Where(i => i.IdEvento == evento.Id && i.IdPapel == Participante && i.Status != "C" && (!pessoa.HasValue || i.IdPessoa == pessoa.Value)).ToList();
        var presencas = context.Participacaopessoaeventos.AsNoTracking()
            .Where(p => p.IdEvento == evento.Id && (!pessoa.HasValue || p.IdPessoa == pessoa.Value)).ToList()
            .GroupBy(p => p.IdPessoa).ToDictionary(g => g.Key, g => g.Select(p => (p.Entrada, p.Saida)).ToList());
        foreach (var inscricao in inscricoes)
        {
            presencas.TryGetValue(inscricao.IdPessoa, out var periodos);
            inscricao.FrequenciaFinal = CalculadoraFrequencia.Calcular(evento.DataInicio, evento.DataFim, evento.CargaHoraria, periodos ?? new());
        }
    }

    public static void Subevento(EventoContext context, Subevento subevento, uint? pessoa = null)
    {
        var inscricoes = context.Inscricaopessoasubeventos
            .Where(i => i.IdSubEvento == subevento.Id && i.IdPapel == Participante && i.Status != "C" && (!pessoa.HasValue || i.IdPessoa == pessoa.Value)).ToList();
        var presencas = context.Participacaopessoasubeventos.AsNoTracking()
            .Where(p => p.IdSubEvento == subevento.Id && (!pessoa.HasValue || p.IdPessoa == pessoa.Value)).ToList()
            .GroupBy(p => p.IdPessoa).ToDictionary(g => g.Key, g => g.Select(p => (p.Entrada, p.Saida)).ToList());
        foreach (var inscricao in inscricoes)
        {
            presencas.TryGetValue(inscricao.IdPessoa, out var periodos);
            inscricao.FrequenciaFinal = CalculadoraFrequencia.Calcular(subevento.DataInicio, subevento.DataFim, subevento.CargaHoraria, periodos ?? new());
        }
    }

    public static void EventoSeFinalizado(EventoContext context, uint idEvento, uint idPessoa)
    {
        var evento = context.Eventos.Find(idEvento);
        if (evento?.Status == "F") Evento(context, evento, idPessoa);
    }

    public static void SubeventoSeFinalizado(EventoContext context, uint idSubevento, uint idPessoa)
    {
        var subevento = context.Subeventos.Find(idSubevento);
        if (subevento == null) return;
        var evento = context.Eventos.Find(subevento.IdEvento);
        if (subevento.Status == "F" || evento?.Status == "F") Subevento(context, subevento, idPessoa);
    }

    // A participação e a frequência são confirmadas na mesma transação relacional.
    public static void SalvarParticipacao(EventoContext context, Action atualizar)
    {
        using var tx = context.Database.IsRelational() && context.Database.CurrentTransaction == null
            ? context.Database.BeginTransaction() : null;
        context.SaveChanges();
        atualizar();
        context.SaveChanges();
        tx?.Commit();
    }

    public static async Task SalvarParticipacaoAsync(EventoContext context, Action atualizar)
    {
        await using var tx = context.Database.IsRelational() && context.Database.CurrentTransaction == null
            ? await context.Database.BeginTransactionAsync() : null;
        await context.SaveChangesAsync();
        atualizar();
        await context.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
    }
}
