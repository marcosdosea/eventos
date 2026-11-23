import 'package:bipou_frontend/features/participants/views/participant_qr_screen.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/services/api_service.dart';
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
    super.key,
  });

  final CredenciamentoGateway credenciamento;
  final QrCodeShareGateway qrCodeShare;
  final PortariaSession? sessaoAtiva;
  final LocalParticipantLookup? localParticipants;

  @override
  State<ParticipantsScreen> createState() => _ParticipantsScreenState();
}

final class _ParticipantsScreenState extends State<ParticipantsScreen> {
  final TextEditingController _searchController = TextEditingController();
  late Future<_ParticipantsLoadResult> _participantesFuture;
  String _searchTerm = '';

  @override
  void initState() {
    super.initState();
    _participantesFuture = _carregarParticipantes();
  }

  Future<_ParticipantsLoadResult> _carregarParticipantes() async {
    final sessao = widget.sessaoAtiva;
    final lookup = widget.localParticipants;
    if (sessao != null && lookup != null) {
      final locais = await lookup.listByContext(
        eventoId: sessao.eventoId,
        subeventoId: sessao.subeventoId,
      );
      if (locais.isNotEmpty) {
        return _ParticipantsLoadResult(
          participantes: locais
              .map((item) => item.toParticipanteResponse())
              .toList(),
          origemLocal: true,
        );
      }
    }

    final remotos = await widget.credenciamento.listarParticipantes();
    return _ParticipantsLoadResult(participantes: remotos, origemLocal: false);
  }

  void _recarregar() {
    setState(() {
      _participantesFuture = _carregarParticipantes();
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final sessao = widget.sessaoAtiva;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Participantes'),
        actions: <Widget>[
          IconButton(
            tooltip: 'Atualizar',
            onPressed: _recarregar,
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
                onRetry: _recarregar,
              );
            }

            final loaded = snapshot.data!;
            final participantes = _ordenarParticipantes(loaded.participantes);
            if (participantes.isEmpty) {
              return const _EmptyState();
            }

            final filtrados = _filtrarParticipantes(participantes);

            return RefreshIndicator(
              onRefresh: () async => _recarregar(),
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
                      contextoLabel: loaded.origemLocal && sessao != null
                          ? '${sessao.contextoBanner} (cache local)'
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
                      loaded.origemLocal
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
  });

  final List<ParticipanteResponse> participantes;
  final bool origemLocal;
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

final class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

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
          ],
        ),
      ),
    );
  }
}
