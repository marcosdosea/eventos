import 'dart:typed_data';

import 'package:bipou_frontend/features/event_selection/view_models/event_selection_view_model.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

typedef OfflineChargeFilePicker = Future<Uint8List?> Function();

final class EventSelectionScreen extends StatefulWidget {
  const EventSelectionScreen({
    required this.repository,
    required this.boralaApi,
    this.initialBoralaBaseUrl,
    this.filePicker,
    super.key,
  });

  final LocalParticipantRepository repository;
  final BoralaApiClient boralaApi;
  final String? initialBoralaBaseUrl;
  final OfflineChargeFilePicker? filePicker;

  @override
  State<EventSelectionScreen> createState() => _EventSelectionScreenState();
}

final class _EventSelectionScreenState extends State<EventSelectionScreen> {
  late final EventSelectionViewModel _viewModel;
  late final TextEditingController _urlController;
  final TextEditingController _usuarioController = TextEditingController();
  final TextEditingController _senhaController = TextEditingController();
  bool _ocultarSenha = true;
  bool _mostrarFormularioConexao = true;

  @override
  void initState() {
    super.initState();
    _viewModel = EventSelectionViewModel(
      repository: widget.repository,
      boralaApi: widget.boralaApi,
      initialBoralaBaseUrl: widget.initialBoralaBaseUrl,
    );
    _urlController = TextEditingController(text: _viewModel.boralaBaseUrl);
    _viewModel.addListener(_onViewModelChanged);
    _inicializar();
  }

  Future<void> _inicializar() async {
    await _viewModel.inicializar();
    if (!mounted) {
      return;
    }
    if (_urlController.text.isEmpty && _viewModel.boralaBaseUrl.isNotEmpty) {
      _urlController.text = _viewModel.boralaBaseUrl;
    }
    if (_usuarioController.text.isEmpty &&
        (_viewModel.operadorUsuario?.isNotEmpty ?? false)) {
      _usuarioController.text = _viewModel.operadorUsuario!;
    }
  }

  void _onViewModelChanged() {
    if (mounted) {
      setState(() {});
    }
  }

  @override
  void dispose() {
    _viewModel.removeListener(_onViewModelChanged);
    _viewModel.dispose();
    _urlController.dispose();
    _usuarioController.dispose();
    _senhaController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final eventoSelecionado = _viewModel.eventoSelecionado;
    final subeventos = _viewModel.subeventosDisponiveis;
    final subeventoSelecionado = _viewModel.subeventoSelecionado;

    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Configuração de Sessão de Portaria',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 16),
          child: FilledButton.icon(
            key: const Key('confirm-session-button'),
            onPressed: _viewModel.ocupado || !_viewModel.podeConfirmarSessao
                ? null
                : _confirmarSessao,
            icon: const Icon(Icons.check_circle),
            label: const Text('Confirmar Sessão de Portaria'),
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFF16803C),
              foregroundColor: Colors.white,
              minimumSize: const Size.fromHeight(56),
              textStyle: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ),
      ),
      body: SafeArea(
        child: _viewModel.carregandoInicial
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                children: <Widget>[
                  if (_viewModel.resumoContextoAtual != null)
                    _ContextSummaryCard(
                      contexto: _viewModel.resumoContextoAtual!,
                      resumoParticipantes:
                          _viewModel.resumoParticipantesAtual ?? '',
                      totalNoEvento: _viewModel.participantesNoEvento,
                      sincronizadoEm: _viewModel.ultimaSincronizacao,
                    ),
                  if (_viewModel.errorMessage != null) ...<Widget>[
                    const SizedBox(height: 12),
                    _FeedbackBanner(
                      message: _viewModel.errorMessage!,
                      isError: true,
                    ),
                  ],
                  if (_viewModel.statusMessage != null) ...<Widget>[
                    const SizedBox(height: 12),
                    _FeedbackBanner(
                      message: _viewModel.statusMessage!,
                      isError: false,
                    ),
                  ],
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: '1. Conexão com o Boralá',
                    subtitle: _viewModel.autenticado
                        ? 'Conectado como ${_viewModel.operadorUsuario}'
                        : 'Autentique-se para buscar eventos ou importe um pacote offline abaixo.',
                    trailing: IconButton(
                      tooltip: _mostrarFormularioConexao
                          ? 'Ocultar conexão'
                          : 'Expandir conexão',
                      onPressed: () {
                        setState(() {
                          _mostrarFormularioConexao =
                              !_mostrarFormularioConexao;
                        });
                      },
                      icon: Icon(
                        _mostrarFormularioConexao
                            ? Icons.expand_less
                            : Icons.expand_more,
                      ),
                    ),
                    child: _mostrarFormularioConexao
                        ? Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: <Widget>[
                              TextField(
                                key: const Key('borala-url-field'),
                                controller: _urlController,
                                keyboardType: TextInputType.url,
                                textInputAction: TextInputAction.next,
                                decoration: const InputDecoration(
                                  labelText: 'URL da API do Boralá',
                                  hintText: 'http://10.0.2.2:5000',
                                  prefixIcon: Icon(Icons.link),
                                ),
                              ),
                              const SizedBox(height: 12),
                              TextField(
                                key: const Key('borala-user-field'),
                                controller: _usuarioController,
                                keyboardType: TextInputType.text,
                                textInputAction: TextInputAction.next,
                                decoration: const InputDecoration(
                                  labelText: 'Usuário (CPF ou e-mail)',
                                  prefixIcon: Icon(Icons.person),
                                ),
                              ),
                              const SizedBox(height: 12),
                              TextField(
                                key: const Key('borala-password-field'),
                                controller: _senhaController,
                                obscureText: _ocultarSenha,
                                textInputAction: TextInputAction.done,
                                onSubmitted: (_) => _conectar(),
                                decoration: InputDecoration(
                                  labelText: 'Senha',
                                  prefixIcon: const Icon(Icons.lock),
                                  suffixIcon: IconButton(
                                    tooltip: _ocultarSenha
                                        ? 'Mostrar senha'
                                        : 'Ocultar senha',
                                    onPressed: () {
                                      setState(
                                        () => _ocultarSenha = !_ocultarSenha,
                                      );
                                    },
                                    icon: Icon(
                                      _ocultarSenha
                                          ? Icons.visibility
                                          : Icons.visibility_off,
                                    ),
                                  ),
                                ),
                              ),
                              const SizedBox(height: 14),
                              FilledButton.icon(
                                key: const Key('borala-connect-button'),
                                onPressed: _viewModel.ocupado
                                    ? null
                                    : _conectar,
                                icon: _viewModel.autenticando
                                    ? const SizedBox.square(
                                        dimension: 18,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: Colors.white,
                                        ),
                                      )
                                    : const Icon(Icons.login),
                                label: Text(
                                  _viewModel.autenticando
                                      ? 'Conectando...'
                                      : 'Conectar e Buscar Eventos',
                                ),
                                style: FilledButton.styleFrom(
                                  minimumSize: const Size.fromHeight(50),
                                ),
                              ),
                            ],
                          )
                        : const SizedBox.shrink(),
                  ),
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: '2. Evento e Escopo da Portaria',
                    subtitle:
                        'Escolha uma única vez onde esta portaria irá operar.',
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: <Widget>[
                        DropdownButtonFormField<int>(
                          key: ValueKey<String>(
                            'evento-${eventoSelecionado?.idEvento}-${_viewModel.eventos.length}',
                          ),
                          initialValue: eventoSelecionado?.idEvento,
                          isExpanded: true,
                          decoration: const InputDecoration(
                            labelText: 'Evento disponível',
                            prefixIcon: Icon(Icons.event),
                          ),
                          hint: const Text(
                            'Conecte-se ao Boralá ou importe um JSON',
                          ),
                          items: _viewModel.eventos
                              .map(
                                (evento) => DropdownMenuItem<int>(
                                  value: evento.idEvento,
                                  child: Text(
                                    evento.nome,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              )
                              .toList(),
                          onChanged: _viewModel.ocupado
                              ? null
                              : (eventoId) {
                                  if (eventoId != null) {
                                    _viewModel.selecionarEvento(eventoId);
                                  }
                                },
                        ),
                        const SizedBox(height: 14),
                        _ModeOptionTile(
                          key: const Key('mode-evento-geral'),
                          title: 'Evento Geral (Acesso ao Congresso/Encontro)',
                          subtitle:
                              'Valida todos os participantes inscritos no evento.',
                          selected:
                              _viewModel.modo == PortariaEscopoModo.eventoGeral,
                          icon: Icons.stadium,
                          onTap: _viewModel.ocupado
                              ? null
                              : () => _viewModel.selecionarModo(
                                  PortariaEscopoModo.eventoGeral,
                                ),
                        ),
                        const SizedBox(height: 10),
                        _ModeOptionTile(
                          key: const Key('mode-subevento'),
                          title:
                              'Subevento Específico (Curso / Minicurso / Oficina)',
                          subtitle:
                              'Valida apenas participantes matriculados no curso escolhido.',
                          selected:
                              _viewModel.modo ==
                              PortariaEscopoModo.subeventoEspecifico,
                          icon: Icons.school,
                          onTap: _viewModel.ocupado
                              ? null
                              : () => _viewModel.selecionarModo(
                                  PortariaEscopoModo.subeventoEspecifico,
                                ),
                        ),
                        if (_viewModel.modo ==
                            PortariaEscopoModo.subeventoEspecifico) ...<Widget>[
                          const SizedBox(height: 14),
                          if (_viewModel.carregandoSubeventos)
                            const Padding(
                              padding: EdgeInsets.symmetric(vertical: 12),
                              child: Center(child: CircularProgressIndicator()),
                            )
                          else if (subeventos.isEmpty)
                            const Text(
                              'Nenhum subevento encontrado para este evento.',
                              style: TextStyle(color: Color(0xFFB45309)),
                            )
                          else
                            DropdownButtonFormField<int>(
                              key: ValueKey<String>(
                                'subevento-${subeventoSelecionado?.idSubEvento}-${subeventos.length}',
                              ),
                              initialValue: subeventoSelecionado?.idSubEvento,
                              isExpanded: true,
                              decoration: const InputDecoration(
                                labelText: 'Subevento / Curso',
                                prefixIcon: Icon(Icons.menu_book),
                              ),
                              items: subeventos
                                  .map(
                                    (sub) => DropdownMenuItem<int>(
                                      value: sub.idSubEvento,
                                      child: Text(
                                        sub.nome,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  )
                                  .toList(),
                              onChanged: _viewModel.ocupado
                                  ? null
                                  : (subId) =>
                                        _viewModel.selecionarSubevento(subId),
                            ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: '3. Carga e Cache Local (Offline)',
                    subtitle:
                        'Baixe os participantes no aparelho para validar instantaneamente mesmo em modo avião.',
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: <Widget>[
                        FilledButton.icon(
                          key: const Key('sync-participants-button'),
                          onPressed:
                              _viewModel.ocupado ||
                                  !_viewModel.podeSincronizarViaApi
                              ? null
                              : _sincronizarViaApi,
                          icon: _viewModel.sincronizando
                              ? const SizedBox.square(
                                  dimension: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : const Icon(Icons.cloud_download),
                          label: Text(
                            _viewModel.sincronizando
                                ? 'Sincronizando...'
                                : 'Sincronizar Lista de Participantes',
                          ),
                          style: FilledButton.styleFrom(
                            backgroundColor: const Color(0xFF175CD3),
                            foregroundColor: Colors.white,
                            minimumSize: const Size.fromHeight(54),
                          ),
                        ),
                        const SizedBox(height: 10),
                        OutlinedButton.icon(
                          key: const Key('import-json-file-button'),
                          onPressed: _viewModel.ocupado
                              ? null
                              : _importarArquivoJson,
                          icon: const Icon(Icons.upload_file),
                          label: const Text('Importar arquivo JSON do Boralá'),
                          style: OutlinedButton.styleFrom(
                            minimumSize: const Size.fromHeight(50),
                          ),
                        ),
                        const SizedBox(height: 8),
                        TextButton.icon(
                          key: const Key('paste-json-button'),
                          onPressed: _viewModel.ocupado
                              ? null
                              : _abrirModalColarJson,
                          icon: const Icon(Icons.data_object),
                          label: const Text('Colar JSON de carga offline'),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
      ),
    );
  }

  Future<void> _conectar() async {
    final ok = await _viewModel.conectarECarregarEventos(
      baseUrl: _urlController.text,
      usuario: _usuarioController.text,
      senha: _senhaController.text,
    );
    if (ok && mounted) {
      setState(() => _mostrarFormularioConexao = false);
    }
  }

  Future<void> _sincronizarViaApi() async {
    await _viewModel.sincronizarParticipantes();
  }

  Future<void> _importarArquivoJson() async {
    final picker = widget.filePicker ?? _pickOfflineFileWithFilePicker;
    try {
      final bytes = await picker();
      if (bytes == null || !mounted) {
        return;
      }
      final sessao = await _viewModel.importarCargaOfflineBytes(bytes);
      if (sessao != null && mounted) {
        setState(() => _mostrarFormularioConexao = false);
      }
    } on Object {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(
            content: Text('Nao foi possivel abrir o arquivo selecionado.'),
          ),
        );
    }
  }

  Future<Uint8List?> _pickOfflineFileWithFilePicker() async {
    final picked = await FilePicker.pickFile(
      dialogTitle: 'Selecionar carga JSON do Boralá',
      type: FileType.any,
    );
    if (picked == null) {
      return null;
    }
    return picked.readAsBytes();
  }

  Future<void> _abrirModalColarJson() async {
    final controller = TextEditingController();
    final rawJson = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) {
        return Padding(
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            top: 8,
            bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(
                'Colar JSON de carga offline',
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 8),
              const Text(
                'Cole o conteúdo JSON exportado pelo Boralá (/export-carga).',
              ),
              const SizedBox(height: 12),
              TextField(
                key: const Key('paste-json-input'),
                controller: controller,
                maxLines: 6,
                decoration: const InputDecoration(
                  hintText: '{"versao":1,"evento":...,"participantes":[...]}',
                ),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                key: const Key('paste-json-submit'),
                onPressed: () =>
                    Navigator.of(context).pop(controller.text.trim()),
                icon: const Icon(Icons.download_done),
                label: const Text('Importar JSON'),
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(50),
                ),
              ),
            ],
          ),
        );
      },
    );
    controller.dispose();

    if (rawJson != null && rawJson.isNotEmpty && mounted) {
      final sessao = await _viewModel.importarCargaOfflineJson(rawJson);
      if (sessao != null && mounted) {
        setState(() => _mostrarFormularioConexao = false);
      }
    }
  }

  Future<void> _confirmarSessao() async {
    final sessao = await _viewModel.confirmarSessao();
    if (sessao != null && mounted) {
      Navigator.of(context).pop<PortariaSession>(sessao);
    }
  }
}

final class _ContextSummaryCard extends StatelessWidget {
  const _ContextSummaryCard({
    required this.contexto,
    required this.resumoParticipantes,
    required this.totalNoEvento,
    this.sincronizadoEm,
  });

  final String contexto;
  final String resumoParticipantes;
  final int totalNoEvento;
  final DateTime? sincronizadoEm;

  @override
  Widget build(BuildContext context) {
    final hasData = totalNoEvento > 0;
    final accent = hasData ? const Color(0xFF16803C) : const Color(0xFF175CD3);

    return DecoratedBox(
      key: const Key('session-summary-card'),
      decoration: BoxDecoration(
        color: accent.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: accent.withValues(alpha: 0.35)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Row(
              children: <Widget>[
                Icon(
                  hasData ? Icons.offline_pin : Icons.info_outline,
                  color: accent,
                  size: 20,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    contexto,
                    style: TextStyle(
                      color: accent,
                      fontWeight: FontWeight.w800,
                      fontSize: 14,
                    ),
                  ),
                ),
              ],
            ),
            if (resumoParticipantes.isNotEmpty) ...<Widget>[
              const SizedBox(height: 6),
              Text(
                resumoParticipantes,
                key: const Key('local-participants-count-text'),
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
            if (sincronizadoEm != null) ...<Widget>[
              const SizedBox(height: 4),
              Text(
                'Última carga: ${_formatTimestamp(sincronizadoEm!)}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatTimestamp(DateTime dt) {
    final local = dt.toLocal();
    String two(int n) => n.toString().padLeft(2, '0');
    return '${two(local.day)}/${two(local.month)}/${local.year} '
        '${two(local.hour)}:${two(local.minute)}';
  }
}

final class _SectionCard extends StatelessWidget {
  const _SectionCard({
    required this.title,
    required this.subtitle,
    required this.child,
    this.trailing,
  });

  final String title;
  final String subtitle;
  final Widget child;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: Color(0xFFE4E7EC)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Text(
                        title,
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w800),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        subtitle,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                ?trailing,
              ],
            ),
            const SizedBox(height: 14),
            child,
          ],
        ),
      ),
    );
  }
}

final class _ModeOptionTile extends StatelessWidget {
  const _ModeOptionTile({
    required this.title,
    required this.subtitle,
    required this.selected,
    required this.icon,
    required this.onTap,
    super.key,
  });

  final String title;
  final String subtitle;
  final bool selected;
  final IconData icon;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final color = selected ? const Color(0xFF175CD3) : const Color(0xFF667085);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: selected
              ? const Color(0xFF175CD3).withValues(alpha: 0.08)
              : Colors.transparent,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: selected ? const Color(0xFF175CD3) : const Color(0xFFD0D5DD),
            width: selected ? 1.8 : 1,
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: <Widget>[
              Icon(
                selected
                    ? Icons.radio_button_checked
                    : Icons.radio_button_unchecked,
                color: color,
              ),
              const SizedBox(width: 10),
              Icon(icon, color: color, size: 22),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      title,
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: selected
                            ? const Color(0xFF101828)
                            : const Color(0xFF344054),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

final class _FeedbackBanner extends StatelessWidget {
  const _FeedbackBanner({required this.message, required this.isError});

  final String message;
  final bool isError;

  @override
  Widget build(BuildContext context) {
    final color = isError ? const Color(0xFFB42318) : const Color(0xFF16803C);
    return DecoratedBox(
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: <Widget>[
            Icon(
              isError ? Icons.error_outline : Icons.check_circle_outline,
              color: color,
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                message,
                style: TextStyle(color: color, fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
