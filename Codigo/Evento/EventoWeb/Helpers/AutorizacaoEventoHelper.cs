using Core.Service;
using System.Security.Claims;

namespace EventoWeb.Helpers
{
    /// <summary>
    /// Centraliza a regra IDOR de gestão por evento:
    /// ADMINISTRADOR libera geral; GESTOR precisa de vínculo ativo
    /// via IInscricaoService.GetGestorInEvent no evento informado.
    /// </summary>
    public static class AutorizacaoEventoHelper
    {
        public static bool IsAutorizado(ClaimsPrincipal? user, IInscricaoService inscricaoService, uint idEvento)
        {
            if (user == null)
                return false;
            if (user.IsInRole("ADMINISTRADOR"))
                return true;
            var username = user.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return false;
            return inscricaoService.GetGestorInEvent(username, idEvento) != null;
        }
    }
}
