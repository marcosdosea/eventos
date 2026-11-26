import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';

typedef SessionManagerClock = DateTime Function();

/// Gerencia credenciais ativas do Boralá e coordena renovação automática de
/// tokens de sessão sem duplicar chamadas concorrentes.
final class BoralaSessionManager {
  BoralaSessionManager({
    required this.repository,
    required this.boralaApi,
    SessionManagerClock? clock,
  }) : _clock = clock ?? DateTime.now;

  final LocalParticipantRepository repository;
  final BoralaApiClient boralaApi;
  final SessionManagerClock _clock;

  Future<BoralaSessionCredentials?>? _inFlightRefresh;

  Future<BoralaSessionCredentials?> getValidCredentials() async {
    final sessao = await repository.loadSession();
    if (sessao == null) {
      return null;
    }

    final baseUrl = sessao.boralaBaseUrl?.trim();
    final refreshToken = sessao.refreshToken?.trim();
    if (sessao.isTokenExpirandoOuExpirado(agora: _clock()) &&
        baseUrl != null &&
        baseUrl.isNotEmpty &&
        refreshToken != null &&
        refreshToken.isNotEmpty) {
      try {
        final refreshed = await refreshActiveSession();
        if (refreshed != null) {
          return refreshed;
        }
      } on Object {
        // Se a renovação proativa falhar (ex.: offline momentâneo), devolve as
        // credenciais atuais para o fluxo de sincronização tratar adequadamente.
      }
    }

    return BoralaSessionCredentials(
      baseUrl: sessao.boralaBaseUrl,
      accessToken: sessao.accessToken,
      eventoId: sessao.eventoId,
      subeventoId: sessao.subeventoId,
    );
  }

  Future<BoralaSessionCredentials?> refreshActiveSession() {
    return _inFlightRefresh ??= _executarRefresh().whenComplete(() {
      _inFlightRefresh = null;
    });
  }

  Future<BoralaSessionCredentials?> _executarRefresh() async {
    final sessao = await repository.loadSession();
    if (sessao == null) {
      return null;
    }

    final baseUrl = sessao.boralaBaseUrl?.trim();
    final refreshToken = sessao.refreshToken?.trim();
    if (baseUrl == null ||
        baseUrl.isEmpty ||
        refreshToken == null ||
        refreshToken.isEmpty) {
      return null;
    }

    final novoToken = await boralaApi.renovarSessao(
      baseUrl: baseUrl,
      refreshToken: refreshToken,
    );
    final novoExpiresAt =
        novoToken.expiresIn != null && novoToken.expiresIn! > 0
        ? _clock().toUtc().add(Duration(seconds: novoToken.expiresIn!))
        : null;

    final PortariaSession sessaoAtualizada = sessao.copyWith(
      accessToken: novoToken.accessToken,
      refreshToken: novoToken.refreshToken ?? refreshToken,
      expiresAt: novoExpiresAt,
    );
    await repository.saveSession(sessaoAtualizada);

    return BoralaSessionCredentials(
      baseUrl: baseUrl,
      accessToken: novoToken.accessToken,
      eventoId: sessaoAtualizada.eventoId,
      subeventoId: sessaoAtualizada.subeventoId,
    );
  }
}
