namespace Service;

/// <summary>Calcula o percentual de presença a partir de intervalos fechados.</summary>
public static class CalculadoraFrequencia
{
    public static decimal Calcular(
        DateTime? inicio, DateTime? fim, decimal cargaHoraria,
        IEnumerable<(DateTime Entrada, DateTime? Saida)> participacoes)
    {
        if (!inicio.HasValue || !fim.HasValue || fim <= inicio || cargaHoraria <= 0)
            return 0m;

        var periodos = participacoes
            .Where(p => p.Saida.HasValue && p.Saida.Value > p.Entrada)
            .Select(p => (
                Inicio: p.Entrada < inicio.Value ? inicio.Value : p.Entrada,
                Fim: p.Saida!.Value > fim.Value ? fim.Value : p.Saida.Value))
            .Where(p => p.Fim > p.Inicio)
            .OrderBy(p => p.Inicio)
            .ThenBy(p => p.Fim)
            .ToList();

        if (periodos.Count == 0) return 0m;

        long ticks = 0;
        var atual = periodos[0];
        foreach (var proximo in periodos.Skip(1))
        {
            if (proximo.Inicio <= atual.Fim)
            {
                if (proximo.Fim > atual.Fim) atual.Fim = proximo.Fim;
            }
            else
            {
                ticks += (atual.Fim - atual.Inicio).Ticks;
                atual = proximo;
            }
        }
        ticks += (atual.Fim - atual.Inicio).Ticks;

        var percentual = (decimal)ticks * 100m / (cargaHoraria * (decimal)TimeSpan.TicksPerHour);
        return Math.Round(Math.Min(100m, percentual), 2, MidpointRounding.AwayFromZero);
    }
}
