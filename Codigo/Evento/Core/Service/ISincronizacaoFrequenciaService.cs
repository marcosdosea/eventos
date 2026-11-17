using Core.DTO;

namespace Core.Service;

/// <summary>
/// Contrato de serviço responsável pela ingestão idempotente de leituras de frequência
/// enviadas pelo aplicativo Bipou e pelo recálculo da frequência final das inscrições.
/// </summary>
public interface ISincronizacaoFrequenciaService
{
    /// <summary>
    /// Processa um lote de leituras de entrada/saída, garantindo idempotência por leituraId
    /// e atualizando a frequência final dos participantes afetados.
    /// </summary>
    Task<BipouLoteFrequenciaResultadoDTO> SincronizarLoteAsync(
        IReadOnlyCollection<BipouLeituraFrequenciaDTO> leituras,
        IReadOnlyCollection<uint> eventosPermitidosIds,
        CancellationToken cancellationToken = default);
}

