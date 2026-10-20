using Microsoft.Data.Sqlite;

namespace Service.Tests
{
    /// <summary>
    /// Schema SQLite equivalente ao modelo do EventoContext.
    ///
    /// Não é possível usar <c>EnsureCreated</c>/migrations neste contexto porque o
    /// modelo fixa tipos exclusivos do MySQL (enum('A','C','S'), blob, decimal unsigned)
    /// e as migrations do projeto são MySQL. O schema abaixo reproduz as tabelas,
    /// colunas e chaves estrangeiras relevantes, todas com ON DELETE RESTRICT,
    /// reproduzindo o comportamento do banco de produção.
    ///
    /// O teste <c>ModeloEvento_UsaDeleteRestrictNasFkCriticas</c> garante que o
    /// modelo EF continue coerente com o schema usado aqui.
    /// </summary>
    public static class SqliteSchemaEvento
    {
        public static void Criar(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = Script;
            command.ExecuteNonQuery();
        }

        private const string Script = @"
PRAGMA foreign_keys = ON;

CREATE TABLE evento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome TEXT NOT NULL,
    descricao TEXT NULL,
    dataInicio datetime NULL,
    dataFim datetime NULL,
    inscricaoGratuita INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'C',
    dataInicioInscricao datetime NULL,
    dataFimInscricao datetime NULL,
    valorInscricao decimal(10,2) NOT NULL DEFAULT 0,
    website TEXT NULL,
    emailEvento TEXT NULL,
    eventoPublico INTEGER NOT NULL DEFAULT 1,
    cep TEXT NULL,
    estado TEXT NULL,
    cidade TEXT NULL,
    bairro TEXT NULL,
    rua TEXT NULL,
    numero TEXT NULL,
    complemento TEXT NULL,
    possuiCertificado INTEGER NOT NULL DEFAULT 0,
    frequenciaMinimaCertificado decimal(10,2) NOT NULL DEFAULT 0,
    idTipoEvento INTEGER NULL,
    vagasOfertadas INTEGER NOT NULL DEFAULT 0,
    vagasReservadas INTEGER NOT NULL DEFAULT 0,
    vagasDisponiveis INTEGER NOT NULL DEFAULT 0,
    tempoMinutosReserva INTEGER NOT NULL DEFAULT 15,
    cargaHoraria INTEGER NOT NULL DEFAULT 0,
    imagemPortal BLOB NULL,
    CONSTRAINT fk_Evento_EstadosBrasil1 FOREIGN KEY (estado) REFERENCES estadosbrasil (estado) ON DELETE RESTRICT,
    CONSTRAINT fk_Evento_TipoEvento1 FOREIGN KEY (idTipoEvento) REFERENCES tipoevento (id) ON DELETE RESTRICT
);

CREATE TABLE pessoa (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome TEXT NOT NULL,
    nomeCracha TEXT NULL,
    cpf TEXT NOT NULL,
    sexo TEXT NOT NULL DEFAULT 'N',
    cep TEXT NULL,
    rua TEXT NULL,
    bairro TEXT NULL,
    cidade TEXT NULL,
    estado TEXT NULL,
    numero TEXT NULL,
    complemento TEXT NULL,
    email TEXT NOT NULL,
    telefone1 TEXT NULL,
    telefone2 TEXT NULL,
    foto BLOB NULL,
    CONSTRAINT cpf_UNIQUE UNIQUE (cpf),
    CONSTRAINT fk_Pessoa_EstadosBrasil1 FOREIGN KEY (estado) REFERENCES estadosbrasil (estado) ON DELETE RESTRICT
);

CREATE TABLE papel (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome TEXT NOT NULL
);

CREATE TABLE tipoevento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome TEXT NOT NULL
);

CREATE TABLE estadosbrasil (
    estado TEXT NOT NULL PRIMARY KEY,
    nome TEXT NOT NULL
);

CREATE TABLE areainteresse (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome TEXT NOT NULL
);

CREATE TABLE areainteresseevento (
    idAreaInteresse INTEGER NOT NULL,
    idEvento INTEGER NOT NULL,
    PRIMARY KEY (idAreaInteresse, idEvento),
    CONSTRAINT fk_AreaInteresseEvento_Areainteresse1 FOREIGN KEY (idAreaInteresse) REFERENCES areainteresse (id) ON DELETE RESTRICT,
    CONSTRAINT fk_AreaInteresseEvento_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT
);

CREATE TABLE subevento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idEvento INTEGER NOT NULL,
    nome TEXT NOT NULL,
    descricao TEXT NOT NULL,
    dataInicio datetime NOT NULL,
    dataFim datetime NOT NULL,
    inscricaoGratuita INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'C',
    dataInicioInscricao datetime NOT NULL,
    dataFimInscricao datetime NOT NULL,
    valorInscricao decimal(10,2) NOT NULL DEFAULT 0,
    possuiCertificado INTEGER NOT NULL DEFAULT 0,
    frequenciaMinimaCertificado decimal(10,2) NOT NULL DEFAULT 0,
    vagasOfertadas INTEGER NOT NULL DEFAULT 0,
    vagasReservadas INTEGER NOT NULL DEFAULT 0,
    vagasDisponiveis INTEGER NOT NULL DEFAULT 0,
    cargaHoraria INTEGER NOT NULL DEFAULT 0,
    idTipoEvento INTEGER NOT NULL,
    CONSTRAINT fk_SubEvento_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT,
    CONSTRAINT fk_SubEvento_TipoEvento1 FOREIGN KEY (idTipoEvento) REFERENCES tipoevento (id) ON DELETE RESTRICT
);

CREATE TABLE tipoinscricao (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idEvento INTEGER NOT NULL,
    nome TEXT NOT NULL,
    descricao TEXT NOT NULL,
    valor decimal(10,2) NOT NULL DEFAULT 0,
    dataInicio datetime NOT NULL,
    datafim datetime NOT NULL,
    usadaEvento INTEGER NOT NULL DEFAULT 0,
    usadaSubevento INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT fk_TipoInscricao_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT
);

CREATE TABLE tipoinscricaosubevento (
    idTipoInscricao INTEGER NOT NULL,
    idSubEvento INTEGER NOT NULL,
    PRIMARY KEY (idTipoInscricao, idSubEvento),
    CONSTRAINT fk_TipoInscricaoSubEvento_TipoInscricao1 FOREIGN KEY (idTipoInscricao) REFERENCES tipoinscricao (id) ON DELETE RESTRICT,
    CONSTRAINT fk_TipoInscricaoSubEvento_SubEvento1 FOREIGN KEY (idSubEvento) REFERENCES subevento (id) ON DELETE RESTRICT
);

CREATE TABLE inscricaopessoaevento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idPessoa INTEGER NOT NULL,
    idEvento INTEGER NOT NULL,
    idPapel INTEGER NOT NULL,
    idTipoInscricao INTEGER NULL,
    dataInscricao datetime NOT NULL,
    valorTotal decimal(10,2) NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'S',
    frequenciaFinal decimal(10,2) NOT NULL DEFAULT 0,
    nomeCracha TEXT NULL,
    CONSTRAINT fk_PessoaEvento_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT,
    CONSTRAINT fk_PessoaEvento_Papel1 FOREIGN KEY (idPapel) REFERENCES papel (id) ON DELETE RESTRICT,
    CONSTRAINT fk_PessoaEvento_Pessoa FOREIGN KEY (idPessoa) REFERENCES pessoa (id) ON DELETE RESTRICT,
    CONSTRAINT fk_PessoaEvento_TipoInscricao1 FOREIGN KEY (idTipoInscricao) REFERENCES tipoinscricao (id) ON DELETE RESTRICT
);

CREATE TABLE inscricaopessoasubevento (
    idPessoa INTEGER NOT NULL,
    idSubEvento INTEGER NOT NULL,
    idPapel INTEGER NOT NULL,
    idTipoInscricao INTEGER NULL,
    dataInscricao datetime NOT NULL,
    valor decimal(10,2) NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'S',
    frequenciaFinal decimal(10,2) NOT NULL DEFAULT 0,
    PRIMARY KEY (idPessoa, idSubEvento),
    CONSTRAINT fk_InscricaoPessoaSubEvento_Papel1 FOREIGN KEY (idPapel) REFERENCES papel (id) ON DELETE RESTRICT,
    CONSTRAINT fk_InscricaoPessoaSubEvento_Pessoa1 FOREIGN KEY (idPessoa) REFERENCES pessoa (id) ON DELETE RESTRICT,
    CONSTRAINT fk_InscricaoPessoaSubEvento_SubEvento1 FOREIGN KEY (idSubEvento) REFERENCES subevento (id) ON DELETE RESTRICT,
    CONSTRAINT fk_InscricaoPessoaSubEvento_TipoInscricao1 FOREIGN KEY (idTipoInscricao) REFERENCES tipoinscricao (id) ON DELETE RESTRICT
);

CREATE TABLE pagamento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idInscricaoPessoaEvento INTEGER NOT NULL,
    valor decimal(10,2) NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'S',
    forma TEXT NOT NULL DEFAULT 'P',
    codigoPagamento TEXT NULL,
    dataPagamento datetime NOT NULL,
    dataSolicitacao datetime NOT NULL,
    CONSTRAINT fk_Pagamento_InscricaoPessoaEvento1 FOREIGN KEY (idInscricaoPessoaEvento) REFERENCES inscricaopessoaevento (id) ON DELETE RESTRICT
);

CREATE TABLE participacaopessoaevento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idPessoa INTEGER NOT NULL,
    idEvento INTEGER NOT NULL,
    entrada datetime NOT NULL,
    saida datetime NULL,
    CONSTRAINT fk_PessoaEvento1_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT,
    CONSTRAINT fk_PessoaEvento1_Pessoa1 FOREIGN KEY (idPessoa) REFERENCES pessoa (id) ON DELETE RESTRICT
);

CREATE TABLE participacaopessoasubevento (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idPessoa INTEGER NOT NULL,
    idSubEvento INTEGER NOT NULL,
    entrada datetime NOT NULL,
    saida datetime NULL,
    CONSTRAINT fk_PessoaSubEvento_Pessoa2 FOREIGN KEY (idPessoa) REFERENCES pessoa (id) ON DELETE RESTRICT,
    CONSTRAINT fk_PessoaSubEvento_SubEvento2 FOREIGN KEY (idSubEvento) REFERENCES subevento (id) ON DELETE RESTRICT
);

CREATE TABLE modelocracha (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idEvento INTEGER NOT NULL,
    logotipo BLOB NULL,
    qrcode BLOB NULL,
    texto TEXT NULL,
    CONSTRAINT fk_ModeloCracha_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT
);

CREATE TABLE modelocertificado (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    idEvento INTEGER NOT NULL,
    assinatura1 BLOB NULL,
    assinatura1Texto TEXT NULL,
    assinatura2 BLOB NULL,
    assinatura2Texto TEXT NULL,
    logotipoSuperior BLOB NULL,
    textoAntesCargaHoraria TEXT NULL,
    textoAntesEvento TEXT NULL,
    textoAntesParticipante TEXT NULL,
    CONSTRAINT fk_ModeloCertificado_Evento1 FOREIGN KEY (idEvento) REFERENCES evento (id) ON DELETE RESTRICT
);

CREATE TABLE pessoaareainteresse (
    idPessoa INTEGER NOT NULL,
    idAreaInteresse INTEGER NOT NULL,
    todosEstados INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (idPessoa, idAreaInteresse),
    CONSTRAINT fk_PessoaAreaInteresse_AreaInteresse1 FOREIGN KEY (idAreaInteresse) REFERENCES areainteresse (id) ON DELETE RESTRICT,
    CONSTRAINT fk_PessoaAreaInteresse_Pessoa1 FOREIGN KEY (idPessoa) REFERENCES pessoa (id) ON DELETE RESTRICT
);

CREATE TABLE estadosbrasilpessoaareainteresse (
    idPessoaPessoaAreaInteresse INTEGER NOT NULL,
    idAreaInteressePessoaAreaInteresse INTEGER NOT NULL,
    estadoEstadosBrasils TEXT NOT NULL,
    PRIMARY KEY (idPessoaPessoaAreaInteresse, idAreaInteressePessoaAreaInteresse),
    CONSTRAINT fk_EstadosBrasilPessoaAreaInteresse_EstadosBrasil1 FOREIGN KEY (estadoEstadosBrasils) REFERENCES estadosbrasil (estado) ON DELETE RESTRICT
);
";
    }
}
