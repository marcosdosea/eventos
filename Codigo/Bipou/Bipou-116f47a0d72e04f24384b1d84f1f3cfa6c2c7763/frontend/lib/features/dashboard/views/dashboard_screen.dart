import 'package:bipou_frontend/features/event_selection/views/event_selection_screen.dart';
import 'package:bipou_frontend/features/manual_registration/views/manual_registration_screen.dart';
import 'package:bipou_frontend/features/participants/views/participant_qr_screen.dart';
import 'package:bipou_frontend/features/participants/views/participants_screen.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/services/scanner_sensory_feedback.dart';
import 'package:bipou_frontend/features/scanner/view_models/scanner_view_model.dart';
import 'package:bipou_frontend/features/scanner/views/scanner_screen.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/audit_log_export_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

final class DashboardScreen extends StatefulWidget {
  const DashboardScreen({
    required this.credenciamento,
    required this.auditLogExporter,
    required this.qrCodeShare,
    required this.dispositivoId,
    this.localParticipantRepository,
    this.boralaApi,
    this.initialBoralaBaseUrl,
    this.initialContext,
    this.scannerSensoryFeedback,
    this.scannerPayloadStream,
    this.scannerCameraPreviewOverride,
    this.scannerClock,
    super.key,
  });

  final CredenciamentoGateway credenciamento;
  final AuditLogExporter auditLogExporter;
  final QrCodeShareGateway qrCodeShare;
  final String dispositivoId;
  final LocalParticipantRepository? localParticipantRepository;
  final BoralaApiClient? boralaApi;
  final String? initialBoralaBaseUrl;
  final ScannerSessionContext? initialContext;
  final ScannerSensoryFeedback? scannerSensoryFeedback;
  final Stream<String>? scannerPayloadStream;
  final Widget? scannerCameraPreviewOverride;
  final ScannerClock? scannerClock;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

final class _DashboardScreenState extends State<DashboardScreen> {
  CredenciamentoGateway get credenciamento => widget.credenciamento;
  AuditLogExporter get auditLogExporter => widget.auditLogExporter;
  QrCodeShareGateway get qrCodeShare => widget.qrCodeShare;
  String get dispositivoId => widget.dispositivoId;
  LocalParticipantRepository? get localRepository =>
      widget.localParticipantRepository;

  PortariaSession? _sessaoAtiva;
  ScannerSessionContext? _contextoAtivo;
  bool _sincronizandoAgora = false;

  FrequenciaPendenteSynchronizer? get _frequenciaSynchronizer {
    final gateway = credenciamento;
    return gateway is FrequenciaPendenteSynchronizer
        ? gateway as FrequenciaPendenteSynchronizer
        : null;
  }

  @override
  void initState() {
    super.initState();
    _contextoAtivo = widget.initialContext;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _carregarSessaoAtiva();
      _inicializarSincronizacao();
    });
  }

  Future<void> _carregarSessaoAtiva() async {
    final repo = localRepository;
    if (repo == null) {
      return;
    }
    try {
      final sessao = await repo.loadSession();
      if (!mounted) {
        return;
      }
      setState(() => _sessaoAtiva = sessao);
    } on Object {
      // Mantém operação padrão caso não haja sessão persistida.
    }
  }

  @override
  Widget build(BuildContext context) {
    final frequenciaSynchronizer = _frequenciaSynchronizer;
    final sessao = _sessaoAtiva;
    final usarSessaoLocal = localRepository != null;
    final exibirContextoManual = !usarSessaoLocal || _contextoAtivo != null;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Credenciamento'),
        centerTitle: true,
        actions: <Widget>[
          if (usarSessaoLocal)
            IconButton(
              tooltip: 'Sessão de Portaria',
              onPressed: () => _abrirConfiguracaoSessao(context),
              icon: const Icon(Icons.tune),
            ),
          if (exibirContextoManual)
            IconButton(
              key: const Key('dashboard-context-button'),
              tooltip: 'Contexto do evento',
              onPressed: () => _configurarContexto(context),
              icon: Icon(
                _contextoAtivo == null
                    ? Icons.event_note
                    : Icons.event_available,
              ),
            ),
          IconButton(
            tooltip: 'Status da API',
            onPressed: () => _abrirDiagnostico(context),
            icon: const Icon(Icons.network_check),
          ),
          IconButton(
            tooltip: 'Participantes',
            onPressed: () => _abrirParticipantes(context),
            icon: const Icon(Icons.group),
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(
                'Portaria Bipou',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Registre entradas e saídas rapidamente por QR Code.',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              if (usarSessaoLocal) ...<Widget>[
                const SizedBox(height: 16),
                _SessionStatusCard(
                  sessao: sessao,
                  onConfigure: () => _abrirConfiguracaoSessao(context),
                ),
              ],
              const SizedBox(height: 18),
              Expanded(
                child: ListView(
                  children: <Widget>[
                    if (exibirContextoManual) ...<Widget>[
                      _ContextCard(
                        contexto: _contextoAtivo,
                        onTap: () => _configurarContexto(context),
                      ),
                      const SizedBox(height: 14),
                    ],
                    if (frequenciaSynchronizer != null) ...<Widget>[
                      StreamBuilder<int>(
                        initialData:
                            frequenciaSynchronizer.frequenciasPendentesCount,
                        stream:
                            frequenciaSynchronizer.onFrequenciasPendentesChanged,
                        builder: (context, snapshot) {
                          final pendentes =
                              snapshot.data ??
                              frequenciaSynchronizer.frequenciasPendentesCount;
                          return _OfflineQueueBanner(
                            pendentes: pendentes,
                            sincronizando: _sincronizandoAgora,
                            onSincronizarAgora: _sincronizarAgoraManual,
                          );
                        },
                      ),
                      const SizedBox(height: 16),
                    ],
                    _DashboardButton(
                      label: 'Escanear QR Code',
                      icon: Icons.qr_code_scanner,
                      color: const Color(0xFF175CD3),
                      onPressed: () => _selecionarAcao(context),
                    ),
                    const SizedBox(height: 14),
                    _DashboardButton(
                      label: 'Ver participantes',
                      icon: Icons.group,
                      color: const Color(0xFF16803C),
                      onPressed: () => _abrirParticipantes(context),
                    ),
                    const SizedBox(height: 14),
                    _DashboardButton(
                      label: 'Cadastrar participante',
                      icon: Icons.person_add,
                      color: const Color(0xFF6941C6),
                      onPressed: () => _abrirCadastroManual(context),
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

  Future<void> _configurarContexto(BuildContext context) async {
    final resultado = await showModalBottomSheet<_ContextSheetResult>(
      context: context,
      backgroundColor: Colors.white,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _ContextConfigSheet(contextoAtual: _contextoAtivo),
    );

    if (resultado != null && mounted) {
      setState(() {
        _contextoAtivo = resultado.contexto;
        if (resultado.contexto != null) {
          _sessaoAtiva = null;
        }
      });
    }
  }

  Future<void> _abrirConfiguracaoSessao(BuildContext context) async {
    final repo = localRepository;
    if (repo == null) {
      return;
    }

    final result = await Navigator.of(context).push<PortariaSession>(
      MaterialPageRoute<PortariaSession>(
        builder: (_) => EventSelectionScreen(
          repository: repo,
          boralaApi: widget.boralaApi ?? BoralaApiService(),
          initialBoralaBaseUrl: widget.initialBoralaBaseUrl,
        ),
      ),
    );

    if (!context.mounted) {
      return;
    }

    if (result != null) {
      setState(() {
        _sessaoAtiva = result;
        _contextoAtivo = null;
      });
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text('Sessão ativa: ${result.contextoBanner}'),
            backgroundColor: const Color(0xFF16803C),
          ),
        );
      await _sincronizarFrequenciasAutomaticamente();
    } else {
      await _carregarSessaoAtiva();
    }
  }

  Future<void> _selecionarAcao(BuildContext context) async {
    final tipoAcao = await showModalBottomSheet<TipoAcao>(
      context: context,
      backgroundColor: Colors.white,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) {
        return SafeArea(
          child: ConstrainedBox(
            constraints: BoxConstraints(
              maxHeight: MediaQuery.sizeOf(context).height * 0.86,
            ),
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 20),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  Text(
                    'Selecione a ação',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 14),
                  _ActionButton(
                    label: 'Entrada',
                    description: 'Chegada no evento',
                    icon: Icons.login,
                    color: const Color(0xFF16803C),
                    onPressed: () =>
                        Navigator.of(context).pop(TipoAcao.entrada),
                  ),
                  const SizedBox(height: 10),
                  _ActionButton(
                    label: 'Saída',
                    description: 'Saída do evento',
                    icon: Icons.logout,
                    color: const Color(0xFFB42318),
                    onPressed: () => Navigator.of(context).pop(TipoAcao.saida),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );

    if (tipoAcao != null && context.mounted) {
      await _abrirScanner(context, tipoAcao);
    }
  }

  Future<void> _abrirScanner(BuildContext context, TipoAcao tipoAcao) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute<void>(
        builder: (_) => ScannerScreen(
          tipoAcao: tipoAcao,
          credenciamento: credenciamento,
          dispositivoId: dispositivoId,
          contextoAtivo: _contextoAtivo,
          sessaoAtiva: _sessaoAtiva,
          localParticipants: localRepository,
          sensoryFeedback: widget.scannerSensoryFeedback,
          payloadStream: widget.scannerPayloadStream,
          cameraPreviewOverride: widget.scannerCameraPreviewOverride,
          clock: widget.scannerClock,
        ),
      ),
    );
    await _atualizarContagemFrequencias();
  }

  Future<void> _abrirCadastroManual(BuildContext context) async {
    final result = await Navigator.of(context)
        .push<OperationResult<ParticipanteResponse>>(
          MaterialPageRoute<OperationResult<ParticipanteResponse>>(
            builder: (_) => ManualRegistrationScreen(
              credenciamento: credenciamento,
              dispositivoId: dispositivoId,
            ),
          ),
        );
    if (result == null || !context.mounted) {
      return;
    }

    final offline = result.status == SyncStatus.salvoOffline;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            offline
                ? 'Cadastro salvo e pendente de sincronização'
                : 'Participante cadastrado',
          ),
          backgroundColor: offline
              ? const Color(0xFFB45309)
              : const Color(0xFF16803C),
        ),
      );

    final participante = result.data;
    if (participante != null && context.mounted) {
      await Navigator.of(context).push<void>(
        MaterialPageRoute<void>(
          builder: (_) => ParticipantQrScreen(
            participante: participante,
            qrCodeShare: qrCodeShare,
          ),
        ),
      );
    }
  }

  Future<void> _abrirParticipantes(BuildContext context) {
    return Navigator.of(context).push<void>(
      MaterialPageRoute<void>(
        builder: (_) => ParticipantsScreen(
          credenciamento: credenciamento,
          qrCodeShare: qrCodeShare,
          sessaoAtiva: _sessaoAtiva,
          localParticipants: localRepository,
        ),
      ),
    );
  }

  Future<void> _inicializarSincronizacao() async {
    await _atualizarContagemFrequencias();
    await _sincronizarCadastrosPendentes();
    await _sincronizarFrequenciasAutomaticamente();
  }

  Future<void> _atualizarContagemFrequencias() async {
    final synchronizer = _frequenciaSynchronizer;
    if (synchronizer == null) {
      return;
    }
    try {
      await synchronizer.atualizarContagemFrequenciasPendentes();
    } on Object {
      // Mantém a última contagem conhecida caso o banco esteja ocupado.
    }
  }

  Future<void> _sincronizarFrequenciasAutomaticamente() async {
    final synchronizer = _frequenciaSynchronizer;
    if (synchronizer == null) {
      return;
    }
    try {
      final result = await synchronizer.sincronizarFrequenciasPendentes();
      if (!mounted) {
        return;
      }
      if (result.enviados > 0) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              '${result.enviados} presença(s) offline sincronizada(s)',
            ),
            backgroundColor: const Color(0xFF16803C),
          ),
        );
      }
    } on Object {
      // A fila permanece salva no SQLite e será tentada novamente.
    }
  }

  Future<void> _sincronizarAgoraManual() async {
    if (_sincronizandoAgora) {
      return;
    }

    setState(() => _sincronizandoAgora = true);
    try {
      await _sincronizarCadastrosPendentes();

      final synchronizer = _frequenciaSynchronizer;
      if (synchronizer == null || !mounted) {
        return;
      }

      final result = await synchronizer.sincronizarFrequenciasPendentes();
      if (!mounted) {
        return;
      }

      final String mensagem;
      final Color cor;
      if (result.offline) {
        mensagem = result.restantes > 0
            ? 'Sem conexão no momento. ${result.restantes} presença(s) aguardando sincronização.'
            : 'Dispositivo sem conexão no momento.';
        cor = const Color(0xFFB45309);
      } else if (result.enviados > 0 &&
          result.rejeitados == 0 &&
          result.restantes == 0) {
        mensagem = '${result.enviados} presença(s) offline sincronizada(s)';
        cor = const Color(0xFF16803C);
      } else if (result.enviados > 0 && result.rejeitados > 0) {
        mensagem =
            '${result.enviados} sincronizada(s) e ${result.rejeitados} recusada(s) pela API.';
        cor = const Color(0xFFB45309);
      } else if (result.enviados > 0 && result.restantes > 0) {
        mensagem =
            '${result.enviados} sincronizada(s); ${result.restantes} ainda aguardando sincronização.';
        cor = const Color(0xFFB45309);
      } else if (result.rejeitados > 0) {
        mensagem =
            '${result.rejeitados} presença(s) pendente(s) foram recusadas pela API.';
        cor = const Color(0xFFB42318);
      } else if (result.restantes > 0) {
        mensagem =
            '${result.restantes} presença(s) ainda aguardando sincronização.';
        cor = const Color(0xFFB45309);
      } else {
        mensagem = 'Nenhuma presença pendente para sincronizar.';
        cor = const Color(0xFF16803C);
      }

      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(mensagem), backgroundColor: cor));
    } on Object {
      if (mounted) {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            const SnackBar(
              content: Text('Não foi possível sincronizar a fila offline.'),
              backgroundColor: Color(0xFFB42318),
            ),
          );
      }
    } finally {
      if (mounted) {
        setState(() => _sincronizandoAgora = false);
      }
    }
  }

  Future<void> _sincronizarCadastrosPendentes() async {
    final gateway = credenciamento;
    if (gateway is! CadastroPendenteSynchronizer) {
      return;
    }
    final synchronizer = gateway as CadastroPendenteSynchronizer;

    try {
      final result = await synchronizer.sincronizarCadastrosPendentes();
      if (!mounted) {
        return;
      }
      if (result.enviados > 0) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              '${result.enviados} cadastro(s) offline sincronizado(s)',
            ),
            backgroundColor: const Color(0xFF16803C),
          ),
        );
      } else if (result.falhasPermanentes > 0) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              '${result.falhasPermanentes} cadastro(s) pendente(s) '
              'foram recusados pela API',
            ),
            backgroundColor: const Color(0xFFB42318),
          ),
        );
      }
    } on Object {
      // A fila permanece em disco e será tentada novamente na próxima ação.
    }
  }

  Future<void> _abrirDiagnostico(BuildContext context) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _ApiStatusSheet(
        credenciamento: credenciamento,
        auditLogExporter: auditLogExporter,
      ),
    );
  }
}

final class _OfflineQueueBanner extends StatelessWidget {
  const _OfflineQueueBanner({
    required this.pendentes,
    required this.sincronizando,
    required this.onSincronizarAgora,
  });

  final int pendentes;
  final bool sincronizando;
  final VoidCallback onSincronizarAgora;

  @override
  Widget build(BuildContext context) {
    final possuiPendencias = pendentes > 0;
    final accentColor = possuiPendencias
        ? const Color(0xFFB42318)
        : const Color(0xFF16803C);
    final titulo = possuiPendencias
        ? (pendentes == 1
              ? '🔴 1 presença salva offline'
              : '🔴 $pendentes presenças salvas offline')
        : 'Fila offline sincronizada';
    final subtitulo = possuiPendencias
        ? 'As leituras estão protegidas localmente e serão enviadas automaticamente ao retornar a conexão.'
        : 'Todas as presenças lidas neste dispositivo foram enviadas.';

    return DecoratedBox(
      key: const Key('offline-queue-banner'),
      decoration: BoxDecoration(
        color: accentColor.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: accentColor.withValues(alpha: 0.35)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Text(
              titulo,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
                color: accentColor,
              ),
            ),
            const SizedBox(height: 6),
            Text(subtitulo, style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 12),
            FilledButton.icon(
              key: const Key('sincronizar-agora-button'),
              onPressed: sincronizando ? null : onSincronizarAgora,
              icon: sincronizando
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.sync),
              label: Text(
                sincronizando ? 'Sincronizando...' : 'Sincronizar Agora',
              ),
              style: FilledButton.styleFrom(
                backgroundColor: accentColor,
                foregroundColor: Colors.white,
                minimumSize: const Size.fromHeight(46),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

final class _ContextSheetResult {
  const _ContextSheetResult(this.contexto);

  final ScannerSessionContext? contexto;
}

final class _ContextCard extends StatelessWidget {
  const _ContextCard({required this.contexto, required this.onTap});

  final ScannerSessionContext? contexto;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final ativo = contexto != null;
    final accentColor = ativo
        ? const Color(0xFF175CD3)
        : const Color(0xFF475467);

    return Material(
      key: const Key('dashboard-context-card'),
      color: ativo ? const Color(0xFFEFF8FF) : const Color(0xFFF9FAFB),
      borderRadius: BorderRadius.circular(14),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(14),
            border: Border.all(
              color: ativo ? const Color(0xFF84CAFF) : const Color(0xFFD0D5DD),
            ),
          ),
          child: Row(
            children: <Widget>[
              Icon(
                ativo
                    ? (contexto!.isSubevento ? Icons.school : Icons.event)
                    : Icons.tune,
                color: accentColor,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: <Widget>[
                    Text(
                      ativo ? 'Contexto ativo' : 'Contexto da portaria',
                      style: TextStyle(
                        color: accentColor,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      ativo
                          ? contexto!.bannerLabel
                          : 'Modo livre (sem filtro de evento ou curso)',
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Color(0xFF101828),
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Text(
                ativo ? 'Alterar' : 'Definir',
                style: TextStyle(
                  color: accentColor,
                  fontSize: 12,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

final class _ContextConfigSheet extends StatefulWidget {
  const _ContextConfigSheet({required this.contextoAtual});

  final ScannerSessionContext? contextoAtual;

  @override
  State<_ContextConfigSheet> createState() => _ContextConfigSheetState();
}

final class _ContextConfigSheetState extends State<_ContextConfigSheet> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _eventoIdController;
  late final TextEditingController _eventoNomeController;
  late final TextEditingController _subeventoIdController;
  late final TextEditingController _subeventoNomeController;
  late bool _isSubevento;

  @override
  void initState() {
    super.initState();
    final atual = widget.contextoAtual;
    _eventoIdController = TextEditingController(
      text: atual?.eventoId?.toString() ?? '',
    );
    _eventoNomeController = TextEditingController(
      text: atual?.eventoNome ?? '',
    );
    _subeventoIdController = TextEditingController(
      text: atual?.subeventoId?.toString() ?? '',
    );
    _subeventoNomeController = TextEditingController(
      text: atual?.subeventoNome ?? '',
    );
    _isSubevento = atual?.isSubevento ?? true;
  }

  @override
  void dispose() {
    _eventoIdController.dispose();
    _eventoNomeController.dispose();
    _subeventoIdController.dispose();
    _subeventoNomeController.dispose();
    super.dispose();
  }

  void _aplicar() {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    final eventoId = int.parse(_eventoIdController.text.trim());
    final eventoNome = _eventoNomeController.text.trim();
    final subeventoId = _isSubevento
        ? int.parse(_subeventoIdController.text.trim())
        : null;
    final subeventoNome = _isSubevento
        ? _subeventoNomeController.text.trim()
        : null;

    Navigator.of(context).pop(
      _ContextSheetResult(
        ScannerSessionContext(
          eventoId: eventoId,
          eventoNome: eventoNome,
          subeventoId: subeventoId,
          subeventoNome: subeventoNome,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.only(
          left: 20,
          right: 20,
          top: 4,
          bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
        ),
        child: SingleChildScrollView(
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(
                  'Configurar contexto da portaria',
                  style: Theme.of(
                    context,
                  ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  key: const Key('context-evento-id-field'),
                  controller: _eventoIdController,
                  keyboardType: TextInputType.number,
                  inputFormatters: <TextInputFormatter>[
                    FilteringTextInputFormatter.digitsOnly,
                  ],
                  decoration: const InputDecoration(
                    labelText: 'ID do Evento',
                    hintText: 'Ex.: 15',
                    prefixIcon: Icon(Icons.tag),
                  ),
                  validator: (value) {
                    final parsed = int.tryParse(value?.trim() ?? '');
                    if (parsed == null || parsed <= 0) {
                      return 'Informe um ID de evento válido';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 12),
                TextFormField(
                  key: const Key('context-evento-nome-field'),
                  controller: _eventoNomeController,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'Nome do Evento',
                    hintText: 'Ex.: Seminfo 2026',
                    prefixIcon: Icon(Icons.event),
                  ),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Informe o nome do evento';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 14),
                SegmentedButton<bool>(
                  segments: const <ButtonSegment<bool>>[
                    ButtonSegment<bool>(
                      value: false,
                      label: Text(
                        'Evento Geral',
                        key: Key('context-mode-geral'),
                      ),
                      icon: Icon(Icons.event),
                    ),
                    ButtonSegment<bool>(
                      value: true,
                      label: Text(
                        'Curso / Subevento',
                        key: Key('context-mode-subevento'),
                      ),
                      icon: Icon(Icons.school),
                    ),
                  ],
                  selected: <bool>{_isSubevento},
                  onSelectionChanged: (selection) {
                    setState(() => _isSubevento = selection.first);
                  },
                ),
                if (_isSubevento) ...<Widget>[
                  const SizedBox(height: 12),
                  TextFormField(
                    key: const Key('context-subevento-id-field'),
                    controller: _subeventoIdController,
                    keyboardType: TextInputType.number,
                    inputFormatters: <TextInputFormatter>[
                      FilteringTextInputFormatter.digitsOnly,
                    ],
                    decoration: const InputDecoration(
                      labelText: 'ID do Curso / Subevento',
                      hintText: 'Ex.: 42',
                      prefixIcon: Icon(Icons.numbers),
                    ),
                    validator: (value) {
                      if (!_isSubevento) {
                        return null;
                      }
                      final parsed = int.tryParse(value?.trim() ?? '');
                      if (parsed == null || parsed <= 0) {
                        return 'Informe um ID de curso válido';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    key: const Key('context-subevento-nome-field'),
                    controller: _subeventoNomeController,
                    textInputAction: TextInputAction.done,
                    decoration: const InputDecoration(
                      labelText: 'Nome do Curso / Subevento',
                      hintText: 'Ex.: Flutter Avançado',
                      prefixIcon: Icon(Icons.school),
                    ),
                    validator: (value) {
                      if (!_isSubevento) {
                        return null;
                      }
                      if (value == null || value.trim().isEmpty) {
                        return 'Informe o nome do curso';
                      }
                      return null;
                    },
                  ),
                ],
                const SizedBox(height: 16),
                FilledButton.icon(
                  key: const Key('context-apply-button'),
                  onPressed: _aplicar,
                  icon: const Icon(Icons.check),
                  label: const Text('Aplicar contexto'),
                  style: FilledButton.styleFrom(
                    minimumSize: const Size.fromHeight(52),
                  ),
                ),
                const SizedBox(height: 8),
                TextButton(
                  key: const Key('context-clear-button'),
                  onPressed: () {
                    Navigator.of(context).pop(const _ContextSheetResult(null));
                  },
                  child: const Text('Usar modo livre'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

final class _SessionStatusCard extends StatelessWidget {
  const _SessionStatusCard({required this.sessao, required this.onConfigure});

  final PortariaSession? sessao;
  final VoidCallback onConfigure;

  @override
  Widget build(BuildContext context) {
    final active = sessao;
    final hasSession = active != null;
    final accent = hasSession
        ? const Color(0xFF175CD3)
        : const Color(0xFFB45309);

    return DecoratedBox(
      key: const Key('dashboard-session-card'),
      decoration: BoxDecoration(
        color: accent.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: accent.withValues(alpha: 0.35)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: <Widget>[
            Icon(
              hasSession ? Icons.verified_user : Icons.event_note,
              color: accent,
              size: 28,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(
                    hasSession
                        ? active.contextoBanner
                        : 'Sessão de Portaria não configurada',
                    style: TextStyle(
                      color: accent,
                      fontWeight: FontWeight.w800,
                      fontSize: 13,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    hasSession
                        ? active.resumoParticipantes
                        : 'Selecione o evento/curso e baixe a lista para operar offline.',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            FilledButton.tonal(
              key: const Key('dashboard-configure-session-button'),
              onPressed: onConfigure,
              child: Text(hasSession ? 'Trocar' : 'Configurar'),
            ),
          ],
        ),
      ),
    );
  }
}

final class _DashboardButton extends StatelessWidget {
  const _DashboardButton({
    required this.label,
    required this.icon,
    required this.color,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final Color color;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return FilledButton.icon(
      onPressed: onPressed,
      icon: Icon(icon, size: 30),
      label: Text(label),
      style: FilledButton.styleFrom(
        backgroundColor: color,
        foregroundColor: Colors.white,
        minimumSize: const Size.fromHeight(86),
        alignment: Alignment.centerLeft,
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 18),
        textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
      ),
    );
  }
}

final class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.description,
    required this.icon,
    required this.color,
    required this.onPressed,
  });

  final String label;
  final String description;
  final IconData icon;
  final Color color;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return FilledButton(
      onPressed: onPressed,
      style: FilledButton.styleFrom(
        backgroundColor: color,
        foregroundColor: Colors.white,
        minimumSize: const Size.fromHeight(72),
        alignment: Alignment.centerLeft,
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
      child: Row(
        children: <Widget>[
          Icon(icon, size: 28),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Text(description, style: const TextStyle(fontSize: 13)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

final class _ApiStatusSheet extends StatefulWidget {
  const _ApiStatusSheet({
    required this.credenciamento,
    required this.auditLogExporter,
  });

  final CredenciamentoGateway credenciamento;
  final AuditLogExporter auditLogExporter;

  @override
  State<_ApiStatusSheet> createState() => _ApiStatusSheetState();
}

final class _ApiStatusSheetState extends State<_ApiStatusSheet> {
  bool _testando = false;
  bool _exportando = false;
  String? _resultado;
  bool? _ok;

  @override
  Widget build(BuildContext context) {
    final color = _ok == null
        ? const Color(0xFF475467)
        : _ok!
        ? const Color(0xFF16803C)
        : const Color(0xFFB42318);

    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Text(
              'Status da API',
              style: Theme.of(
                context,
              ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            const Text('URL configurada'),
            const SizedBox(height: 6),
            SelectableText(
              widget.credenciamento.apiBaseUrl,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 16),
            if (_resultado != null)
              DecoratedBox(
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: color.withValues(alpha: 0.35)),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Text(_resultado!, style: TextStyle(color: color)),
                ),
              ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: _testando ? null : _testarConexao,
              icon: _testando
                  ? const SizedBox.square(
                      dimension: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.wifi_tethering),
              label: Text(_testando ? 'Testando...' : 'Testar conexão'),
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(54),
              ),
            ),
            const SizedBox(height: 10),
            OutlinedButton.icon(
              onPressed: _exportando ? null : _exportarLog,
              icon: _exportando
                  ? const SizedBox.square(
                      dimension: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.ios_share),
              label: Text(_exportando ? 'Preparando...' : 'Exportar log'),
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(54),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _testarConexao() async {
    setState(() {
      _testando = true;
      _resultado = null;
      _ok = null;
    });

    try {
      await widget.credenciamento.verificarConexao();
      if (!mounted) {
        return;
      }
      setState(() {
        _ok = true;
        _resultado = 'Backend respondeu corretamente.';
      });
    } on Object catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _ok = false;
        _resultado = error.toString();
      });
    } finally {
      if (mounted) {
        setState(() => _testando = false);
      }
    }
  }

  Future<void> _exportarLog() async {
    setState(() => _exportando = true);

    try {
      await widget.auditLogExporter.export();
    } on Object catch (error) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(error.toString())));
    } finally {
      if (mounted) {
        setState(() => _exportando = false);
      }
    }
  }
}
