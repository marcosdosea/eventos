namespace Core.DTO;

public record BipouEventoDTO(uint IdEvento, string Nome, DateTime? DataInicio, DateTime? DataFim);

public record BipouSubeventoDTO(uint IdSubEvento, uint IdEvento, string Nome, DateTime DataInicio, DateTime DataFim);

public record BipouParticipanteDTO(uint IdPessoa, string Nome, string NomeCracha, string Cpf,
    string StatusInscricao, IReadOnlyList<uint> SubeventosInscritos);

public record BipouCargaDTO(int Versao, BipouEventoDTO Evento, IReadOnlyList<BipouSubeventoDTO> Subeventos,
    IReadOnlyList<BipouParticipanteDTO> Participantes);

public record BipouLeituraFrequenciaDTO(
    string LeituraId,
    uint IdPessoa,
    uint IdEvento,
    uint? IdSubEvento,
    string TipoAcao,
    DateTimeOffset DataHoraLida,
    string? DispositivoId);

public record BipouLoteFrequenciaRequestDTO(IReadOnlyList<BipouLeituraFrequenciaDTO> Leituras);

public record BipouLeituraErroDTO(string LeituraId, string Codigo, string Mensagem);

public record BipouLoteFrequenciaResultadoDTO(
    IReadOnlyList<string> Confirmados,
    IReadOnlyList<BipouLeituraErroDTO> Rejeitados);
