import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/features/participants/views/participant_qr_screen.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/borala_session_manager.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';

final class ParticipantsScreen extends StatefulWidget {
  const ParticipantsScreen({
    required this.credenciamento,
    required this.qrCodeShare,
    this.sessaoAtiva,
    this.localParticipants,
    this.boralaApi,
    this.boralaSessionManager,
    this.onConfigureSession,
    super.key,
  });

  final CredenciamentoGateway credenciamento;
  final QrCodeShareGateway qrCodeShare;
  final PortariaSession? sessaoAtiva;
  final LocalParticipantLookup? localParticipants;
  final BoralaApiClient? boralaApi;
  final BoralaSessionManager? boralaSessionManager;
  final Future<PortariaSession?> Function()? onConfigureSession;

  @override
  State<ParticipantsScreen> createState() => _ParticipantsScreenState();
}

final class _ParticipantsScreenState extends State<ParticipantsScreen> {
  final TextEditingController _searchController = TextEditingController();
  late Future<_ParticipantsLoadResult> _participantesFuture;
  PortariaSession? _sessaoAtiva;
  String _searchTerm = '';

  LocalParticipantRepository? get _localRepository {
    final lookup = widget.localParticipants;
    return lookup is LocalParticipantRepository ? lookup : null;
  }

  @override
  void initState() {
    super.initState();
    _sessaoAtiva = widget.sessaoAtiva;
    _participantesFuture = _carregarParticipantes();
  }

  Future<_ParticipantsLoadResult> _carregarParticipantes({
    bool forceRemote = false,
  }) async {
    var sessao = _sessaoAtiva;
    final repo = _localRepository;
    if (repo != null) {
      try {
        final persistida = await repo.loadSession();
        if (persistida != null) {
          sessao = persistida;
          _sessaoAtiva = persistida;
        }
      } on Object {
        // Segue sem sessao local persistida.
      }
    }

    final lookup = widget.localParticipants;
    if (sessao != null) {
      var sessaoCorrente = sessao;
      final locais = lookup != null
          ? await lookup.listByContext(
              eventoId: sessaoCorrente.eventoId,
              subeventoId: sessaoCorrente.subeventoId,
            )
          : const <LocalParticipant>[];

      if (!forceRemote && locais.isNotEmpty) {
        return _ParticipantsLoadResult(
          participantes: locais
              .map((item) => item.toParticipanteResponse())
              .toList(),
          origemLocal: true,
        );
      }

      final token = sessaoCorrente.accessToken?.trim();
      final possuiToken = token != null && token.isNotEmpty;
      if (possuiToken && widget.boralaApi != null) {
        var activeToken = token;
        var baseUrl =
            sessaoCorrente.boralaBaseUrl != null &&
                sessaoCorrente.boralaBaseUrl!.trim().isNotEmpty
            ? sessaoCorrente.boralaBaseUrl!.trim()
            : widget.credenciamento.apiBaseUrl;
        final sessionManager = widget.boralaSessionManager;
        if (sessionManager != null) {
          final credentials = await sessionManager.getValidCredentials();
          final validToken = credentials?.accessToken?.trim();
          if (validToken != null && validToken.isNotEmpty) {
            activeToken = validToken;
          }
          final validBaseUrl = credentials?.baseUrl?.trim();
          if (validBaseUrl != null && validBaseUrl.isNotEmpty) {
            baseUrl = validBaseUrl;
          }
          if (repo != null) {
            try {
              final atualizada = await repo.loadSession();
              if (atualizada != null) {
                sessaoCorrente = atualizada;
                _sessaoAtiva = atualizada;
              }
            } on Object {
              // Mantem sessaoCorrente em memoria caso o repositorio falhe.
            }
          }
        }
        try {
          BoralaParticipantesResult result;
          try {
            result = await widget.boralaApi!.listarParticipantes(
              baseUrl: baseUrl,
              accessToken: activeToken,
              eventoId: sessaoCorrente.eventoId,
            );
          } on ApiRequestException catch (error) {
            if (error.statusCode == 401 && sessionManager != null) {
              final refreshed = await sessionManager.refreshActiveSession();
              final refreshedToken = refreshed?.accessToken?.trim();
              if (refreshedToken != null && refreshedToken.isNotEmpty) {
                activeToken = refreshedToken;
                final refreshedBaseUrl = refreshed?.baseUrl?.trim();
                if (refreshedBaseUrl != null && refreshedBaseUrl.isNotEmpty) {
                  baseUrl = refreshedBaseUrl;
                }
                if (repo != null) {
                  try {
                    final atualizada = await repo.loadSession();
                    if (atualizada != null) {
                      sessaoCorrente = atualizada;
                      _sessaoAtiva = atualizada;
                    }
                  } on Object {
                    // Mantem sessaoCorrente em memoria caso o repositorio falhe.
                  }
                }
                result = await widget.boralaApi!.listarParticipantes(
                  baseUrl: baseUrl,
                  accessToken: activeToken,
                  eventoId: sessaoCorrente.eventoId,
                );
              } else {
                rethrow;
              }
            } else {
              rethrow;
            }
          }

          final List<LocalParticipant> todosLocais;
          if (result.notModified) {
            todosLocais = locais;
          } else {
            todosLocais = result.participantes
                .map(
                  (p) => LocalParticipant.fromBorala(
                    eventoId: sessaoCorrente.eventoId,
                    participante: p,
                  ),
                )
                .toList();
            if (repo != null) {
              await repo.replaceAllForEvent(
                eventoId: sessaoCorrente.eventoId,
                participants: todosLocais,
              );
              await repo.saveEventEtag(
                eventoId: sessaoCorrente.eventoId,
                etag: result.etag,
              );
            }
          }

          final subeventoId = sessaoCorrente.subeventoId;
          final filtradosContexto = subeventoId == null
              ? todosLocais
              : todosLocais
                    .where((p) => p.inscritoNoSubevento(subeventoId))
                    .toList();

          if (repo != null && !result.notModified) {
            final sessaoAtualizada = sessaoCorrente.copyWith(
              accessToken: activeToken,
              totalParticipantesCarregados: filtradosContexto.length,
              sincronizadoEm: DateTime.now().toUtc(),
            );
            await repo.saveSession(sessaoAtualizada);
            _sessaoAtiva = sessaoAtualizada;
          }

          return _ParticipantsLoadResult(
            participantes: filtradosContexto
                .map((item) => item.toParticipanteResponse())
                .toList(),
            origemLocal: result.notModified,
          );
        } on ApiUnavailableException {
          if (locais.isNotEmpty) {
            return _ParticipantsLoadResult(
              participantes: locais
                  .map((item) => item.toParticipanteResponse())
                  .toList(),
              origemLocal: true,
            );
          }
          rethrow;
        }
      }

      if (possuiToken) {
        final remotos = await widget.credenciamento.listarParticipantes();
        return _ParticipantsLoadResult(
          participantes: remotos,
          origemLocal: false,
        );
      }

      return _ParticipantsLoadResult(
        participantes: locais
            .map((item) => item.toParticipanteResponse())
            .toList(),
        origemLocal: true,
      );
    }

    if (widget.onConfigureSession != null &&
        lookup != null &&
        widget.credenciamento.apiBaseUrl.toLowerCase() ==
            ApiConfig.defaultBoralaApiBaseUrl.toLowerCase()) {
      return const _ParticipantsLoadResult.semSessao();
    }

    final remotos = await widget.credenciamento.listarParticipantes();
    return _ParticipantsLoadResult(participantes: remotos, origemLocal: false);
  }

  Future<void> _recarregar({bool forceRemote = true}) {
    final future = _carregarParticipantes(forceRemote: forceRemote);
    setState(() {
      _participantesFuture = future;
    });
    return future.then<void>((_) {}, onError: (Object _) {});
  }

  Future<void> _configurarSessao() async {
    final callback = widget.onConfigureSession;
    if (callback == null) {
      return;
    }

    final novaSessao = await callback();
    if (!mounted) {
      return;
    }

    if (novaSessao != null) {
      setState(() {
        _sessaoAtiva = novaSessao;
        _participantesFuture = _carregarParticipantes();
      });
    } else {
      setState(() {
        _participantesFuture = _carregarParticipantes();
      });
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final sessao = _sessaoAtiva;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Participantes'),
        actions: <Widget>[
          IconButton(
            tooltip: 'Atualizar',
            onPressed: () => _recarregar(forceRemote: true),
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: SafeArea(
        child: FutureBuilder<_ParticipantsLoadResult>(
          future: _participantesFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError) {
              return _ErrorState(
                message: _mensagemErro(snapshot.error),
                onRetry: () => _recarregar(forceRemote: true),
                onConfigureSession:
                    widget.onConfigureSession != null &&
                        _erroRequerSessao(snapshot.error)
                    ? _configurarSessao
                    : null,
              );
            }

            final loaded = snapshot.data!;
            if (loaded.requerConfiguracaoSessao) {
              return _ConfigureSessionState(onConfigure: _configurarSessao);
            }

            final participantes = _ordenarParticipantes(loaded.participantes);
            if (participantes.isEmpty) {
              return const _EmptyState();
            }

            final filtrados = _filtrarParticipantes(participantes);
            final sessaoAtual = _sessaoAtiva ?? sessao;

            return RefreshIndicator(
              onRefresh: () => _recarregar(forceRemote: true),
              child: ListView.separated(
                padding: const EdgeInsets.all(16),
                itemCount: filtrados.isEmpty ? 2 : filtrados.length + 1,
                separatorBuilder: (_, index) {
                  return SizedBox(height: index == 0 ? 14 : 8);
                },
                itemBuilder: (context, index) {
                  if (index == 0) {
                    return _ParticipantsSearchHeader(
                      controller: _searchController,
                      total: participantes.length,
                      filtered: filtrados.length,
                      contextoLabel: sessaoAtual != null
                          ? (loaded.origemLocal
                                ? '${sessaoAtual.contextoBanner} (cache local)'
                                : sessaoAtual.contextoBanner)
                          : null,
                      onChanged: (value) {
                        setState(() => _searchTerm = value.trim());
                      },
                    );
                  }

                  if (filtrados.isEmpty) {
                    return const _NoSearchResults();
                  }

                  final participante = filtrados[index - 1];
                  return ListTile(
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                      side: const BorderSide(color: Color(0xFFE4E7EC)),
                    ),
                    leading: const CircleAvatar(child: Icon(Icons.person)),
                    title: Text(
                      participante.nome,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    subtitle: Text(
                      (loaded.origemLocal || sessaoAtual != null) &&
                              participante.cpf.isNotEmpty
                          ? 'CPF: ${participante.cpf} • Toque para ver QR Code'
                          : 'Toque para visualizar o QR Code',
                    ),
                    trailing: IconButton(
                      tooltip: 'Ver QR Code',
                      onPressed: () => _abrirQrCode(participante),
                      icon: const Icon(Icons.qr_code_2),
                    ),
                    onTap: () => _abrirQrCode(participante),
                  );
                },
              ),
            );
          },
        ),
      ),
    );
  }

  List<ParticipanteResponse> _ordenarParticipantes(
    List<ParticipanteResponse> participantes,
  ) {
    return participantes.toList()
      ..sort((a, b) => a.nome.toLowerCase().compareTo(b.nome.toLowerCase()));
  }

  List<ParticipanteResponse> _filtrarParticipantes(
    List<ParticipanteResponse> participantes,
  ) {
    final term = _searchTerm.toLowerCase();
    if (term.isEmpty) {
      return participantes;
    }
    return participantes.where((participante) {
      return participante.nome.toLowerCase().contains(term) ||
          participante.cpf.contains(term);
    }).toList();
  }

  Future<void> _abrirQrCode(ParticipanteResponse participante) {
    return Navigator.of(context).push<void>(
      MaterialPageRoute<void>(
        builder: (_) => ParticipantQrScreen(
          participante: participante,
          qrCodeShare: widget.qrCodeShare,
        ),
      ),
    );
  }

  bool _erroRequerSessao(Object? error) {
    if (error is ApiRequestException) {
      return error.statusCode == 401 ||
          error.statusCode == 403 ||
          error.statusCode == 404;
    }
    return false;
  }

  String _mensagemErro(Object? error) {
    if (error is ApiRequestException) {
      return error.message;
    }
    if (error is ApiUnavailableException) {
      return error.message;
    }
    return 'Nao foi possivel carregar os participantes.';
  }
}

final class _ParticipantsLoadResult {
  const _ParticipantsLoadResult({
    required this.participantes,
    required this.origemLocal,
  }) : requerConfiguracaoSessao = false;

  const _ParticipantsLoadResult.semSessao()
    : participantes = const <ParticipanteResponse>[],
      origemLocal = false,
      requerConfiguracaoSessao = true;

  final List<ParticipanteResponse> participantes;
  final bool origemLocal;
  final bool requerConfiguracaoSessao;
}

final class _ParticipantsSearchHeader extends StatelessWidget {
  const _ParticipantsSearchHeader({
    required this.controller,
    required this.total,
    required this.filtered,
    required this.onChanged,
    this.contextoLabel,
  });

  final TextEditingController controller;
  final int total;
  final int filtered;
  final ValueChanged<String> onChanged;
  final String? contextoLabel;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        if (contextoLabel != null) ...<Widget>[
          DecoratedBox(
            decoration: BoxDecoration(
              color: const Color(0xFF16803C).withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: const Color(0xFF16803C).withValues(alpha: 0.35),
              ),
            ),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              child: Text(
                contextoLabel!,
                style: const TextStyle(
                  color: Color(0xFF16803C),
                  fontWeight: FontWeight.w700,
                  fontSize: 12,
                ),
              ),
            ),
          ),
          const SizedBox(height: 12),
        ],
        TextField(
          controller: controller,
          keyboardType: TextInputType.text,
          textInputAction: TextInputAction.search,
          decoration: const InputDecoration(
            labelText: 'Buscar participante',
            hintText: 'Nome ou CPF do participante',
            prefixIcon: Icon(Icons.search),
          ),
          onChanged: onChanged,
        ),
        const SizedBox(height: 10),
        Text(
          filtered == total
              ? '$total participante(s)'
              : '$filtered de $total participante(s)',
          style: Theme.of(context).textTheme.bodySmall,
        ),
      ],
    );
  }
}

final class _NoSearchResults extends StatelessWidget {
  const _NoSearchResults();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(vertical: 48),
      child: Center(
        child: Text(
          'Nenhum participante encontrado para essa busca.',
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}

final class _EmptyState extends StatelessWidget {
  const _EmptyState();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'Nenhum participante cadastrado.',
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}

final class _ConfigureSessionState extends StatelessWidget {
  const _ConfigureSessionState({required this.onConfigure});

  final VoidCallback onConfigure;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            const Icon(Icons.event_note, size: 48, color: Color(0xFF175CD3)),
            const SizedBox(height: 16),
            const Text(
              'Nenhuma sessão de portaria configurada.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            const Text(
              'Conecte-se ao Boralá e selecione um evento para listar os participantes.',
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              key: const Key('participants-configure-session-button'),
              onPressed: onConfigure,
              icon: const Icon(Icons.tune),
              label: const Text('Configurar Sessão de Portaria'),
            ),
          ],
        ),
      ),
    );
  }
}

final class _ErrorState extends StatelessWidget {
  const _ErrorState({
    required this.message,
    required this.onRetry,
    this.onConfigureSession,
  });

  final String message;
  final VoidCallback onRetry;
  final VoidCallback? onConfigureSession;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            const Icon(Icons.cloud_off, size: 48, color: Color(0xFFB42318)),
            const SizedBox(height: 16),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh),
              label: const Text('Tentar novamente'),
            ),
            if (onConfigureSession != null) ...<Widget>[
              const SizedBox(height: 10),
              FilledButton.icon(
                key: const Key('participants-error-configure-session-button'),
                onPressed: onConfigureSession,
                icon: const Icon(Icons.tune),
                label: const Text('Configurar Sessão de Portaria'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
