using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Core;

public partial class EventoContext : DbContext
{
    public EventoContext()
    {
    }

    public EventoContext(DbContextOptions<EventoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Areainteresse> Areainteresses { get; set; }

    public virtual DbSet<Estadosbrasil> Estadosbrasils { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<Inscricaopessoaevento> Inscricaopessoaeventos { get; set; }

    public virtual DbSet<Inscricaopessoasubevento> Inscricaopessoasubeventos { get; set; }

    public virtual DbSet<Modelocertificado> Modelocertificados { get; set; }

    public virtual DbSet<Modelocracha> Modelocrachas { get; set; }

    public virtual DbSet<Pagamento> Pagamentos { get; set; }

    public virtual DbSet<Papel> Papels { get; set; }

    public virtual DbSet<Participacaopessoaevento> Participacaopessoaeventos { get; set; }

    public virtual DbSet<Participacaopessoasubevento> Participacaopessoasubeventos { get; set; }

    public virtual DbSet<Pessoa> Pessoas { get; set; }

    public virtual DbSet<Pessoaareainteresse> Pessoaareainteresses { get; set; }

    public virtual DbSet<Subevento> Subeventos { get; set; }

    public virtual DbSet<Tipoevento> Tipoeventos { get; set; }

    public virtual DbSet<Tipoinscricao> Tipoinscricaos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Areainteresse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("areainteresse");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");

            entity.HasMany(d => d.IdEventos).WithMany(p => p.IdAreaInteresses)
                .UsingEntity<Dictionary<string, object>>(
                    "Areainteresseevento",
                    r => r.HasOne<Evento>().WithMany()
                        .HasForeignKey("IdEvento")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_AreaInteresseEvento_Evento1"),
                    l => l.HasOne<Areainteresse>().WithMany()
                        .HasForeignKey("IdAreaInteresse")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_AreaInteresseEvento_AreaInteresse1"),
                    j =>
                    {
                        j.HasKey("IdAreaInteresse", "IdEvento").HasName("PRIMARY");
                        j.ToTable("areainteresseevento");
                        j.HasIndex(new[] { "IdAreaInteresse" }, "fk_AreaInteresseEvento_AreaInteresse1_idx");
                        j.HasIndex(new[] { "IdEvento" }, "fk_AreaInteresseEvento_Evento1_idx");
                        j.IndexerProperty<uint>("IdAreaInteresse")
                            .HasColumnType("int(10) unsigned")
                            .HasColumnName("idAreaInteresse");
                        j.IndexerProperty<uint>("IdEvento")
                            .HasColumnType("int(10) unsigned")
                            .HasColumnName("idEvento");
                    });
        });

        modelBuilder.Entity<Estadosbrasil>(entity =>
        {
            entity.HasKey(e => e.Estado).HasName("PRIMARY");

            entity.ToTable("estadosbrasil");

            entity.Property(e => e.Estado)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("estado");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .HasColumnName("nome");

            entity.HasMany(d => d.Ids).WithMany(p => p.EstadoEstadosBrasils)
                .UsingEntity<Dictionary<string, object>>(
                    "Estadosbrasilpessoaareainteresse",
                    r => r.HasOne<Pessoaareainteresse>().WithMany()
                        .HasForeignKey("IdPessoaPessoaAreaInteresse", "IdAreaInteressePessoaAreaInteresse")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_EstadosBrasilPessoaAreaInteresse_PessoaAreaInteresse1"),
                    l => l.HasOne<Estadosbrasil>().WithMany()
                        .HasForeignKey("EstadoEstadosBrasil")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_EstadosBrasilPessoaAreaInteresse_EstadosBrasil1"),
                    j =>
                    {
                        j.HasKey("EstadoEstadosBrasil", "IdPessoaPessoaAreaInteresse", "IdAreaInteressePessoaAreaInteresse").HasName("PRIMARY");
                        j.ToTable("estadosbrasilpessoaareainteresse");
                        j.HasIndex(new[] { "EstadoEstadosBrasil" }, "fk_EstadosBrasilPessoaAreaInteresse_EstadosBrasil1_idx");
                        j.HasIndex(new[] { "IdPessoaPessoaAreaInteresse", "IdAreaInteressePessoaAreaInteresse" }, "fk_EstadosBrasilPessoaAreaInteresse_PessoaAreaInteresse1_idx");
                        j.IndexerProperty<string>("EstadoEstadosBrasil")
                            .HasMaxLength(2)
                            .IsFixedLength()
                            .HasColumnName("estadoEstadosBrasil");
                        j.IndexerProperty<uint>("IdPessoaPessoaAreaInteresse")
                            .HasColumnType("int(10) unsigned")
                            .HasColumnName("idPessoaPessoaAreaInteresse");
                        j.IndexerProperty<uint>("IdAreaInteressePessoaAreaInteresse")
                            .HasColumnType("int(10) unsigned")
                            .HasColumnName("idAreaInteressePessoaAreaInteresse");
                    });
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("evento");

            entity.HasIndex(e => e.Estado, "fk_Evento_EstadosBrasil1_idx");

            entity.HasIndex(e => e.IdTipoEvento, "fk_Evento_TipoEvento1_idx");

            entity.HasIndex(e => e.Id, "idEvento_UNIQUE").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Bairro)
                .HasMaxLength(50)
                .HasColumnName("bairro");
            entity.Property(e => e.CargaHoraria)
                .HasColumnType("int(11)")
                .HasColumnName("cargaHoraria");
            entity.Property(e => e.Cep)
                .HasMaxLength(8)
                .HasColumnName("cep");
            entity.Property(e => e.Cidade)
                .HasMaxLength(50)
                .HasColumnName("cidade");
            entity.Property(e => e.Complemento)
                .HasMaxLength(50)
                .HasColumnName("complemento");
            entity.Property(e => e.DataFim)
                .HasColumnType("datetime")
                .HasColumnName("dataFim");
            entity.Property(e => e.DataFimInscricao)
                .HasColumnType("datetime")
                .HasColumnName("dataFimInscricao");
            entity.Property(e => e.DataInicio)
                .HasColumnType("datetime")
                .HasColumnName("dataInicio");
            entity.Property(e => e.DataInicioInscricao)
                .HasColumnType("datetime")
                .HasColumnName("dataInicioInscricao");
            entity.Property(e => e.Descricao)
                .HasMaxLength(5000)
                .HasColumnName("descricao");
            entity.Property(e => e.EmailEvento)
                .HasMaxLength(100)
                .HasColumnName("emailEvento");
            entity.Property(e => e.Estado)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("estado");
            entity.Property(e => e.EventoPublico)
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("eventoPublico");
            entity.Property(e => e.FrequenciaMinimaCertificado)
                .HasPrecision(10)
                .HasColumnName("frequenciaMinimaCertificado");
            entity.Property(e => e.IdTipoEvento)
                .HasColumnType("int(11)")
                .HasColumnName("idTipoEvento");
            entity.Property(e => e.InscricaoGratuita)
                .HasColumnType("tinyint(4)")
                .HasColumnName("inscricaoGratuita");
            entity.Property(e => e.Nome)
                .HasMaxLength(200)
                .HasColumnName("nome");
            entity.Property(e => e.Numero)
                .HasMaxLength(50)
                .HasColumnName("numero");
            entity.Property(e => e.PossuiCertificado)
                .HasColumnType("tinyint(4)")
                .HasColumnName("possuiCertificado");
            entity.Property(e => e.Rua)
                .HasMaxLength(50)
                .HasColumnName("rua");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'C'")
                .HasComment("C- CADASTRO\nA- ATIVO\nI - INATIVO\nF- FINALIZADO\n ")
                .HasColumnType("enum('C','A','F')")
                .HasColumnName("status");
            entity.Property(e => e.TempoMinutosReserva)
                .HasDefaultValueSql("'15'")
                .HasColumnType("int(11)")
                .HasColumnName("tempoMinutosReserva");
            entity.Property(e => e.VagasDisponiveis)
                .HasColumnType("int(11)")
                .HasColumnName("vagasDisponiveis");
            entity.Property(e => e.VagasOfertadas)
                .HasColumnType("int(11)")
                .HasColumnName("vagasOfertadas");
            entity.Property(e => e.VagasReservadas)
                .HasColumnType("int(11)")
                .HasColumnName("vagasReservadas");
            entity.Property(e => e.ValorInscricao)
                .HasPrecision(10)
                .HasColumnName("valorInscricao");
            entity.Property(e => e.Website)
                .HasMaxLength(200)
                .HasColumnName("website");

            entity.HasOne(d => d.EstadoNavigation).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.Estado)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_Evento_EstadosBrasil1");

            entity.HasOne(d => d.IdTipoEventoNavigation).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.IdTipoEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_Evento_TipoEvento1");
        });

        modelBuilder.Entity<Inscricaopessoaevento>(entity =>
        {
            entity.HasKey(e => new { e.IdPessoa, e.IdEvento }).HasName("PRIMARY");

            entity.ToTable("inscricaopessoaevento");

            entity.HasIndex(e => e.IdEvento, "fk_PessoaEvento_Evento1_idx");

            entity.HasIndex(e => e.IdPapel, "fk_PessoaEvento_Papel1_idx");

            entity.HasIndex(e => e.IdPessoa, "fk_PessoaEvento_Pessoa_idx");

            entity.HasIndex(e => e.IdTipoInscricao, "fk_PessoaEvento_TipoInscricao1_idx");

            entity.Property(e => e.IdPessoa)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idPessoa");
            entity.Property(e => e.IdEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEvento");
            entity.Property(e => e.DataInscricao)
                .HasColumnType("datetime")
                .HasColumnName("dataInscricao");
            entity.Property(e => e.FrequenciaFinal)
                .HasPrecision(10)
                .HasColumnName("frequenciaFinal");
            entity.Property(e => e.IdPapel)
                .HasColumnType("int(11)")
                .HasColumnName("idPapel");
            entity.Property(e => e.IdTipoInscricao)
                .HasColumnType("int(11)")
                .HasColumnName("idTipoInscricao");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'S'")
                .HasComment("A - ATIVA\nC - CANCELADA\nS - SOLICITADA\n")
                .HasColumnType("enum('A','C','S')")
                .HasColumnName("status");
            entity.Property(e => e.ValorTotal)
                .HasPrecision(10)
                .HasColumnName("valorTotal");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Inscricaopessoaeventos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaEvento_Evento1");

            entity.HasOne(d => d.IdPapelNavigation).WithMany(p => p.Inscricaopessoaeventos)
                .HasForeignKey(d => d.IdPapel)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaEvento_Papel1");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.Inscricaopessoaeventos)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaEvento_Pessoa");

            entity.HasOne(d => d.IdTipoInscricaoNavigation).WithMany(p => p.Inscricaopessoaeventos)
                .HasForeignKey(d => d.IdTipoInscricao)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaEvento_TipoInscricao1");
        });

        modelBuilder.Entity<Inscricaopessoasubevento>(entity =>
        {
            entity.HasKey(e => new { e.IdPessoa, e.IdSubEvento }).HasName("PRIMARY");

            entity.ToTable("inscricaopessoasubevento");

            entity.HasIndex(e => e.IdPapel, "fk_InscricaoPessoaSubEvento_Papel1_idx");

            entity.HasIndex(e => e.IdTipoInscricao, "fk_InscricaoPessoaSubEvento_TipoInscricao1_idx");

            entity.HasIndex(e => e.IdPessoa, "fk_PessoaSubEvento_Pessoa1_idx");

            entity.HasIndex(e => e.IdSubEvento, "fk_PessoaSubEvento_SubEvento1_idx");

            entity.Property(e => e.IdPessoa)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idPessoa");
            entity.Property(e => e.IdSubEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idSubEvento");
            entity.Property(e => e.DataInscricao)
                .HasColumnType("datetime")
                .HasColumnName("dataInscricao");
            entity.Property(e => e.FrequenciaFinal)
                .HasPrecision(10)
                .HasColumnName("frequenciaFinal");
            entity.Property(e => e.IdPapel)
                .HasColumnType("int(11)")
                .HasColumnName("idPapel");
            entity.Property(e => e.IdTipoInscricao)
                .HasColumnType("int(11)")
                .HasColumnName("idTipoInscricao");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'S'")
                .HasComment("A - ATIVA\nC - CANCELADA\nS - SOLICITADA\n")
                .HasColumnType("enum('A','C','S')")
                .HasColumnName("status");
            entity.Property(e => e.Valor)
                .HasPrecision(10)
                .HasColumnName("valor");

            entity.HasOne(d => d.IdPapelNavigation).WithMany(p => p.Inscricaopessoasubeventos)
                .HasForeignKey(d => d.IdPapel)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_InscricaoPessoaSubEvento_Papel1");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.Inscricaopessoasubeventos)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaSubEvento_Pessoa1");

            entity.HasOne(d => d.IdSubEventoNavigation).WithMany(p => p.Inscricaopessoasubeventos)
                .HasForeignKey(d => d.IdSubEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaSubEvento_SubEvento1");

            entity.HasOne(d => d.IdTipoInscricaoNavigation).WithMany(p => p.Inscricaopessoasubeventos)
                .HasForeignKey(d => d.IdTipoInscricao)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_InscricaoPessoaSubEvento_TipoInscricao1");
        });

        modelBuilder.Entity<Modelocertificado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("modelocertificado");

            entity.HasIndex(e => e.IdEvento, "fk_ModeloCertificado_Evento1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Assinatura1)
                .HasColumnType("blob")
                .HasColumnName("assinatura1");
            entity.Property(e => e.Assinatura1Texto)
                .HasMaxLength(50)
                .HasColumnName("assinatura1Texto");
            entity.Property(e => e.Assinatura2)
                .HasColumnType("blob")
                .HasColumnName("assinatura2");
            entity.Property(e => e.Assinatura2Texto)
                .HasMaxLength(50)
                .HasColumnName("assinatura2Texto");
            entity.Property(e => e.IdEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEvento");
            entity.Property(e => e.LogotipoSuperior)
                .HasColumnType("blob")
                .HasColumnName("logotipoSuperior");
            entity.Property(e => e.TextoAntesCargaHoraria)
                .HasMaxLength(500)
                .HasColumnName("textoAntesCargaHoraria");
            entity.Property(e => e.TextoAntesEvento)
                .HasMaxLength(500)
                .HasColumnName("textoAntesEvento");
            entity.Property(e => e.TextoAntesParticipante)
                .HasMaxLength(500)
                .HasColumnName("textoAntesParticipante");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Modelocertificados)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ModeloCertificado_Evento1");
        });

        modelBuilder.Entity<Modelocracha>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("modelocracha");

            entity.HasIndex(e => e.IdEvento, "fk_ModeloCracha_Evento1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.IdEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEvento");
            entity.Property(e => e.Logotipo)
                .HasColumnType("blob")
                .HasColumnName("logotipo");
            entity.Property(e => e.Qrcode)
                .HasColumnType("tinyint(4)")
                .HasColumnName("qrcode");
            entity.Property(e => e.Texto)
                .HasMaxLength(200)
                .HasColumnName("texto");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Modelocrachas)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ModeloCracha_Evento1");
        });

        modelBuilder.Entity<Pagamento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pagamento");

            entity.HasIndex(e => new { e.IdPessoaInscricaoPessoaEvento, e.IdEventoInscricaoPessoaEvento }, "fk_Pagamento_InscricaoPessoaEvento1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CodigoPagamento)
                .HasMaxLength(500)
                .HasColumnName("codigoPagamento");
            entity.Property(e => e.DataPagamento)
                .HasColumnType("datetime")
                .HasColumnName("dataPagamento");
            entity.Property(e => e.DataSolicitacao)
                .HasColumnType("datetime")
                .HasColumnName("dataSolicitacao");
            entity.Property(e => e.Forma)
                .HasDefaultValueSql("'P'")
                .HasComment("D - DINHEIRO\nC- CARTAO\nB - BOLETO\nP - PIX")
                .HasColumnType("enum('D','C','B','P')")
                .HasColumnName("forma");
            entity.Property(e => e.IdEventoInscricaoPessoaEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEventoInscricaoPessoaEvento");
            entity.Property(e => e.IdPessoaInscricaoPessoaEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idPessoaInscricaoPessoaEvento");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'S'")
                .HasComment("S- SOLICITADO\nC- CONFIRMADO")
                .HasColumnType("enum('S','C')")
                .HasColumnName("status");
            entity.Property(e => e.Valor)
                .HasPrecision(10)
                .HasColumnName("valor");

            entity.HasOne(d => d.IdNavigation).WithMany(p => p.Pagamentos)
                .HasForeignKey(d => new { d.IdPessoaInscricaoPessoaEvento, d.IdEventoInscricaoPessoaEvento })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_Pagamento_InscricaoPessoaEvento1");
        });

        modelBuilder.Entity<Papel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("papel");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .HasColumnName("nome");
        });

        modelBuilder.Entity<Participacaopessoaevento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("participacaopessoaevento");

            entity.HasIndex(e => e.IdEvento, "fk_PessoaEvento1_Evento1_idx");

            entity.HasIndex(e => e.IdPessoa, "fk_PessoaEvento1_Pessoa1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Entrada)
                .HasColumnType("datetime")
                .HasColumnName("entrada");
            entity.Property(e => e.IdEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEvento");
            entity.Property(e => e.IdPessoa)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idPessoa");
            entity.Property(e => e.Saida)
                .HasColumnType("datetime")
                .HasColumnName("saida");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Participacaopessoaeventos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaEvento1_Evento1");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.Participacaopessoaeventos)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaEvento1_Pessoa1");
        });

        modelBuilder.Entity<Participacaopessoasubevento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("participacaopessoasubevento");

            entity.HasIndex(e => e.IdPessoa, "fk_PessoaSubEvento_Pessoa2_idx");

            entity.HasIndex(e => e.IdSubEvento, "fk_PessoaSubEvento_SubEvento2_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Entrada)
                .HasColumnType("datetime")
                .HasColumnName("entrada");
            entity.Property(e => e.IdPessoa)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idPessoa");
            entity.Property(e => e.IdSubEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idSubEvento");
            entity.Property(e => e.Saida)
                .HasColumnType("datetime")
                .HasColumnName("saida");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.Participacaopessoasubeventos)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaSubEvento_Pessoa2");

            entity.HasOne(d => d.IdSubEventoNavigation).WithMany(p => p.Participacaopessoasubeventos)
                .HasForeignKey(d => d.IdSubEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaSubEvento_SubEvento2");
        });

        modelBuilder.Entity<Pessoa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pessoa");

            entity.HasIndex(e => e.Cpf, "cpf_UNIQUE").IsUnique();

            entity.HasIndex(e => e.Estado, "fk_Pessoa_EstadosBrasil1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Bairro)
                .HasMaxLength(50)
                .HasColumnName("bairro");
            entity.Property(e => e.Cep)
                .HasMaxLength(8)
                .HasColumnName("cep");
            entity.Property(e => e.Cidade)
                .HasMaxLength(50)
                .HasColumnName("cidade");
            entity.Property(e => e.Complemento)
                .HasMaxLength(50)
                .HasColumnName("complemento");
            entity.Property(e => e.Cpf)
                .HasMaxLength(11)
                .HasColumnName("cpf");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.Estado)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("estado");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .HasColumnName("nome");
            entity.Property(e => e.NomeCracha)
                .HasMaxLength(20)
                .HasColumnName("nomeCracha");
            entity.Property(e => e.Numero)
                .HasMaxLength(10)
                .HasColumnName("numero");
            entity.Property(e => e.Rua)
                .HasMaxLength(50)
                .HasColumnName("rua");
            entity.Property(e => e.Sexo)
                .HasDefaultValueSql("'N'")
                .HasComment("M - Masculino\nF - Feminino\nN - Não Informado")
                .HasColumnType("enum('M','F','N')")
                .HasColumnName("sexo");
            entity.Property(e => e.Telefone1)
                .HasMaxLength(10)
                .HasColumnName("telefone1");
            entity.Property(e => e.Telefone2)
                .HasMaxLength(10)
                .HasColumnName("telefone2");

            entity.HasOne(d => d.EstadoNavigation).WithMany(p => p.Pessoas)
                .HasForeignKey(d => d.Estado)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_Pessoa_EstadosBrasil1");
        });

        modelBuilder.Entity<Pessoaareainteresse>(entity =>
        {
            entity.HasKey(e => new { e.IdPessoa, e.IdAreaInteresse }).HasName("PRIMARY");

            entity.ToTable("pessoaareainteresse");

            entity.HasIndex(e => e.IdAreaInteresse, "fk_PessoaAreaInteresse_AreaInteresse1_idx");

            entity.HasIndex(e => e.IdPessoa, "fk_PessoaAreaInteresse_Pessoa1_idx");

            entity.Property(e => e.IdPessoa)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idPessoa");
            entity.Property(e => e.IdAreaInteresse)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idAreaInteresse");
            entity.Property(e => e.TodosEstados)
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("todosEstados");

            entity.HasOne(d => d.IdAreaInteresseNavigation).WithMany(p => p.Pessoaareainteresses)
                .HasForeignKey(d => d.IdAreaInteresse)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaAreaInteresse_AreaInteresse1");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.Pessoaareainteresses)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_PessoaAreaInteresse_Pessoa1");
        });

        modelBuilder.Entity<Subevento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("subevento");

            entity.HasIndex(e => e.IdEvento, "fk_SubEvento_Evento1_idx");

            entity.HasIndex(e => e.IdTipoEvento, "fk_SubEvento_TipoEvento1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CargaHoraria)
                .HasColumnType("int(11)")
                .HasColumnName("cargaHoraria");
            entity.Property(e => e.DataFim)
                .HasColumnType("datetime")
                .HasColumnName("dataFim");
            entity.Property(e => e.DataFimInscricao)
                .HasColumnType("datetime")
                .HasColumnName("dataFimInscricao");
            entity.Property(e => e.DataInicio)
                .HasColumnType("datetime")
                .HasColumnName("dataInicio");
            entity.Property(e => e.DataInicioInscricao)
                .HasColumnType("datetime")
                .HasColumnName("dataInicioInscricao");
            entity.Property(e => e.Descricao)
                .HasMaxLength(5000)
                .HasColumnName("descricao");
            entity.Property(e => e.FrequenciaMinimaCertificado)
                .HasPrecision(10)
                .HasColumnName("frequenciaMinimaCertificado");
            entity.Property(e => e.IdEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEvento");
            entity.Property(e => e.IdTipoEvento)
                .HasColumnType("int(11)")
                .HasColumnName("idTipoEvento");
            entity.Property(e => e.InscricaoGratuita)
                .HasColumnType("tinyint(4)")
                .HasColumnName("inscricaoGratuita");
            entity.Property(e => e.Nome)
                .HasMaxLength(200)
                .HasColumnName("nome");
            entity.Property(e => e.PossuiCertificado)
                .HasColumnType("tinyint(4)")
                .HasColumnName("possuiCertificado");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'C'")
                .HasComment("C- CADASTRO\nA- ABERTO\nF- FINALIZADO\n ")
                .HasColumnType("enum('C','A','F')")
                .HasColumnName("status");
            entity.Property(e => e.VagasDisponiveis)
                .HasColumnType("int(11)")
                .HasColumnName("vagasDisponiveis");
            entity.Property(e => e.VagasOfertadas)
                .HasColumnType("int(11)")
                .HasColumnName("vagasOfertadas");
            entity.Property(e => e.VagasReservadas)
                .HasColumnType("int(11)")
                .HasColumnName("vagasReservadas");
            entity.Property(e => e.ValorInscricao)
                .HasPrecision(10)
                .HasColumnName("valorInscricao");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Subeventos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_SubEvento_Evento1");

            entity.HasOne(d => d.IdTipoEventoNavigation).WithMany(p => p.Subeventos)
                .HasForeignKey(d => d.IdTipoEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_SubEvento_TipoEvento1");
        });

        modelBuilder.Entity<Tipoevento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tipoevento");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .HasColumnName("nome");
        });

        modelBuilder.Entity<Tipoinscricao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tipoinscricao");

            entity.HasIndex(e => e.IdEvento, "fk_TipoInscricao_Evento1_idx");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.DataInicio)
                .HasColumnType("datetime")
                .HasColumnName("dataInicio");
            entity.Property(e => e.Datafim)
                .HasColumnType("datetime")
                .HasColumnName("datafim");
            entity.Property(e => e.Descricao)
                .HasMaxLength(200)
                .HasColumnName("descricao");
            entity.Property(e => e.IdEvento)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("idEvento");
            entity.Property(e => e.UsadaEvento)
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("usadaEvento");
            entity.Property(e => e.UsadaSubevento)
                .HasDefaultValueSql("'1'")
                .HasColumnType("tinyint(4)")
                .HasColumnName("usadaSubevento");
            entity.Property(e => e.Valor)
                .HasPrecision(10)
                .HasColumnName("valor");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.Tipoinscricaos)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_TipoInscricao_Evento1");

            entity.HasMany(d => d.IdSubEventos).WithMany(p => p.IdTipoInscricaos)
                .UsingEntity<Dictionary<string, object>>(
                    "Tipoinscricaosubevento",
                    r => r.HasOne<Subevento>().WithMany()
                        .HasForeignKey("IdSubEvento")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_TipoInscricaoSubEvento_SubEvento1"),
                    l => l.HasOne<Tipoinscricao>().WithMany()
                        .HasForeignKey("IdTipoInscricao")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_TipoInscricaoSubEvento_TipoInscricao1"),
                    j =>
                    {
                        j.HasKey("IdTipoInscricao", "IdSubEvento").HasName("PRIMARY");
                        j.ToTable("tipoinscricaosubevento");
                        j.HasIndex(new[] { "IdSubEvento" }, "fk_TipoInscricaoSubEvento_SubEvento1_idx");
                        j.HasIndex(new[] { "IdTipoInscricao" }, "fk_TipoInscricaoSubEvento_TipoInscricao1_idx");
                        j.IndexerProperty<int>("IdTipoInscricao")
                            .HasColumnType("int(11)")
                            .HasColumnName("idTipoInscricao");
                        j.IndexerProperty<uint>("IdSubEvento")
                            .HasColumnType("int(10) unsigned")
                            .HasColumnName("idSubEvento");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
