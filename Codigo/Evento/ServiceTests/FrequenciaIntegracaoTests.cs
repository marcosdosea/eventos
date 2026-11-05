using Core;
using Core.Service;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Service.Tests;

[TestClass]
public class FrequenciaIntegracaoTests
{
    private SqliteConnection conexao = null!;
    private EventoContext contexto = null!;
    private static readonly DateTime Inicio = new(2026, 11, 5, 8, 0, 0);

    [TestInitialize]
    public void Preparar()
    {
        conexao = new SqliteConnection("Filename=:memory:"); conexao.Open();
        SqliteSchemaEvento.Criar(conexao);
        contexto = new EventoContext(new DbContextOptionsBuilder<EventoContext>().UseSqlite(conexao).Options);
        contexto.Papels.Add(new Papel { Id = 4, Nome = "Participante" });
        contexto.Tipoeventos.Add(new Tipoevento { Id = 1, Nome = "Oficina" });
        contexto.Pessoas.AddRange(
            new Pessoa { Id = 1, Nome = "Ana", NomeCracha = "Ana", Cpf = "11111111111", Email = "ana@example.test" },
            new Pessoa { Id = 2, Nome = "Bia", NomeCracha = "Bia", Cpf = "22222222222", Email = "bia@example.test" });
        contexto.Eventos.AddRange(
            new Evento { Id = 1, Nome = "Evento", Status = "A", DataInicio = Inicio, DataFim = Inicio.AddHours(4), CargaHoraria = 4 },
            new Evento { Id = 2, Nome = "Outro", Status = "F", DataInicio = Inicio, DataFim = Inicio.AddHours(4), CargaHoraria = 4 });
        contexto.Subeventos.Add(new Subevento { Id = 1, IdEvento = 1, Nome = "Sub", Descricao = "Sub", Status = "A", DataInicio = Inicio, DataFim = Inicio.AddHours(2), DataInicioInscricao = Inicio.AddDays(-2), DataFimInscricao = Inicio.AddDays(-1), CargaHoraria = 2, IdTipoEvento = 1 });
        contexto.Inscricaopessoaeventos.AddRange(
            new Inscricaopessoaevento { Id = 1, IdEvento = 1, IdPessoa = 1, IdPapel = 4, Status = "A", DataInscricao = Inicio },
            new Inscricaopessoaevento { Id = 2, IdEvento = 1, IdPessoa = 2, IdPapel = 4, Status = "C", DataInscricao = Inicio, FrequenciaFinal = 12 },
            new Inscricaopessoaevento { Id = 3, IdEvento = 2, IdPessoa = 1, IdPapel = 4, Status = "A", DataInscricao = Inicio, FrequenciaFinal = 27 });
        contexto.Inscricaopessoasubeventos.Add(new Inscricaopessoasubevento { IdPessoa = 1, IdSubEvento = 1, IdPapel = 4, Status = "A", DataInscricao = Inicio });
        contexto.Participacaopessoaeventos.Add(new Participacaopessoaevento { Id = 1, IdPessoa = 1, IdEvento = 1, Entrada = Inicio, Saida = Inicio.AddHours(3) });
        contexto.Participacaopessoasubeventos.Add(new Participacaopessoasubevento { Id = 1, IdPessoa = 1, IdSubEvento = 1, Entrada = Inicio, Saida = Inicio.AddHours(1) });
        contexto.SaveChanges(); contexto.ChangeTracker.Clear();
    }

    [TestCleanup]
    public void Limpar() { contexto.Dispose(); conexao.Dispose(); }

    [TestMethod]
    public void FinalizarEvento_CalculaEventoESubevento_SemAlterarCanceladosOuOutrosEventos()
    {
        var servico = new EventoService(contexto);
        var evento = servico.Get(1); evento.Status = "F";
        Assert.IsTrue(servico.Edit(evento, new List<uint>()));
        contexto.ChangeTracker.Clear();
        Assert.AreEqual(75m, contexto.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);
        Assert.AreEqual(50m, contexto.Inscricaopessoasubeventos.Find((uint)1, (uint)1)!.FrequenciaFinal);
        Assert.AreEqual(12m, contexto.Inscricaopessoaeventos.Find((uint)2)!.FrequenciaFinal);
        Assert.AreEqual(27m, contexto.Inscricaopessoaeventos.Find((uint)3)!.FrequenciaFinal);
        evento = servico.Get(1); Assert.IsTrue(servico.Edit(evento, new List<uint>()));
        contexto.ChangeTracker.Clear();
        Assert.AreEqual(75m, contexto.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);
    }

    [TestMethod]
    public async Task AlterarOuExcluirPresencaFinalizada_RecalculaEventoESubevento()
    {
        var evento = contexto.Eventos.Find((uint)1)!; evento.Status = "F"; contexto.SaveChanges();
        var presencaEvento = await contexto.Participacaopessoaeventos.FindAsync((uint)1);
        presencaEvento!.Saida = Inicio.AddHours(2);
        var service = new ParticipacaoPessoaEventoService(contexto);
        Assert.IsTrue(await service.UpdateAsync(presencaEvento));
        contexto.ChangeTracker.Clear();
        Assert.AreEqual(50m, contexto.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);
        Assert.IsTrue(await service.DeleteAsync(1));
        contexto.ChangeTracker.Clear();
        Assert.AreEqual(0m, contexto.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);
        var serviceSub = new ParticipacaoPessoaSubEventoService(contexto);
        var presencaSub = contexto.Participacaopessoasubeventos.Find((uint)1)!;
        presencaSub.Saida = Inicio.AddHours(2); serviceSub.Update(presencaSub);
        contexto.ChangeTracker.Clear();
        Assert.AreEqual(100m, contexto.Inscricaopessoasubeventos.Find((uint)1,(uint)1)!.FrequenciaFinal);
        serviceSub.Delete(1); contexto.ChangeTracker.Clear();
        Assert.AreEqual(0m, contexto.Inscricaopessoasubeventos.Find((uint)1,(uint)1)!.FrequenciaFinal);
    }

    [TestMethod]
    public void FinalizarSubevento_CalculaSemFinalizarEvento()
    {
        var subevento = contexto.Subeventos.Find((uint)1)!; subevento.Status = "F";
        new SubeventoService(contexto).Edit(subevento); contexto.ChangeTracker.Clear();
        Assert.AreEqual(50m, contexto.Inscricaopessoasubeventos.Find((uint)1,(uint)1)!.FrequenciaFinal);
        Assert.AreEqual(0m, contexto.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);
    }
    [TestMethod]
    public async Task FalhaAoAtualizarFrequencia_DesfazAlteracaoDaParticipacao()
    {
        var evento = contexto.Eventos.Find((uint)1)!; evento.Status = "F"; contexto.SaveChanges();
        using (var command = conexao.CreateCommand())
        {
            command.CommandText = "CREATE TRIGGER falha_frequencia BEFORE UPDATE ON inscricaopessoaevento BEGIN SELECT RAISE(ABORT, 'falha simulada'); END;";
            command.ExecuteNonQuery();
        }
        var entity = (await contexto.Participacaopessoaeventos.FindAsync((uint)1))!;
        entity.Saida = Inicio.AddHours(2);
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => new ParticipacaoPessoaEventoService(contexto).UpdateAsync(entity));
        contexto.ChangeTracker.Clear();
        Assert.AreEqual(Inicio.AddHours(3), contexto.Participacaopessoaeventos.Find((uint)1)!.Saida);
        Assert.AreEqual(0m, contexto.Inscricaopessoaeventos.Find((uint)1)!.FrequenciaFinal);
    }

}
