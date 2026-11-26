using Core.DTO;

namespace Core.Service;

public interface INotificacaoEventoService
{
    bool EventoElegivelParaNotificacao(Evento? evento);

    IReadOnlyList<DestinatarioNotificacaoEventoDTO> ObterDestinatariosElegiveis(uint idEvento);

    Task<ResultadoNotificacaoEventoDTO> NotificarNovoEventoAsync(
        uint idEvento,
        string linkEvento,
        bool forcarMesmoSeJaDivulgado = false,
        CancellationToken cancellationToken = default);

    Task<ResultadoNotificacaoEventoDTO> ReenviarFalhasAsync(
        uint idEvento,
        string linkEvento,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResultadoNotificacaoEventoDTO>> ReenviarTodasFalhasAsync(
        Func<uint, string> gerarLinkEvento,
        CancellationToken cancellationToken = default);

    ResumoNotificacaoEventoDTO ObterResumoPorEvento(uint idEvento);
}
