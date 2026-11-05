using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests;

[TestClass]
public class CalculadoraFrequenciaTests
{
    private static readonly DateTime Inicio = new(2026, 11, 5, 8, 0, 0);
    private static readonly DateTime Fim = Inicio.AddHours(8);

    [TestMethod]
    public void CalculaProporcaoDaCargaHoraria()
    {
        var presencas = new[] { (Inicio, (DateTime?)Inicio.AddHours(3)) };
        Assert.AreEqual(75m, CalculadoraFrequencia.Calcular(Inicio, Fim, 4, presencas));
    }

    [TestMethod]
    public void UnificaSobreposicoesERecortaPelosLimites()
    {
        var presencas = new[] {
            (Inicio.AddHours(-1), (DateTime?)Inicio.AddHours(2)),
            (Inicio.AddHours(1), (DateTime?)Inicio.AddHours(3)),
            (Inicio.AddHours(7), (DateTime?)Fim.AddHours(2))
        };
        Assert.AreEqual(50m, CalculadoraFrequencia.Calcular(Inicio, Fim, 8, presencas));
    }

    [TestMethod]
    public void IgnoraIntervalosAbertosInvalidosEOcorridoForaDoEvento()
    {
        var presencas = new[] {
            (Inicio, (DateTime?)null), (Inicio.AddHours(2), (DateTime?)Inicio.AddHours(1)),
            (Fim.AddHours(1), (DateTime?)Fim.AddHours(2))
        };
        Assert.AreEqual(0m, CalculadoraFrequencia.Calcular(Inicio, Fim, 4, presencas));
    }

    [TestMethod]
    public void LimitaA100PorCentoETrataParametrosInvalidos()
    {
        var presencas = new[] { (Inicio, (DateTime?)Fim) };
        Assert.AreEqual(100m, CalculadoraFrequencia.Calcular(Inicio, Fim, 4, presencas));
        Assert.AreEqual(0m, CalculadoraFrequencia.Calcular(Inicio, Fim, 0, presencas));
        Assert.AreEqual(0m, CalculadoraFrequencia.Calcular(null, Fim, 4, presencas));
        Assert.AreEqual(0m, CalculadoraFrequencia.Calcular(Fim, Inicio, 4, presencas));
    }
}
