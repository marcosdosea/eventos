import 'dart:async';
import 'dart:math' as math;

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/features/scanner/models/recent_scan_record.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/features/scanner/services/scanner_sensory_feedback.dart';
import 'package:bipou_frontend/features/scanner/view_models/scanner_view_model.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

final class ScannerScreen extends StatefulWidget {
  const ScannerScreen({
    required this.credenciamento,
    required this.dispositivoId,
    this.tipoAcao,
    this.contextoAtivo,
    this.sessaoAtiva,
    this.localParticipants,
    this.sensoryFeedback,
    this.payloadStream,
    this.barcodeCaptureStream,
    this.cameraPreviewOverride,
    this.clock,
    super.key,
  });

  final TipoAcao? tipoAcao;
  final CredenciamentoGateway credenciamento;
  final String dispositivoId;
  final ScannerSessionContext? contextoAtivo;
  final PortariaSession? sessaoAtiva;
  final LocalParticipantLookup? localParticipants;
  final ScannerSensoryFeedback? sensoryFeedback;
  final Stream<String>? payloadStream;
  final Stream<BarcodeCapture>? barcodeCaptureStream;
  final Widget? cameraPreviewOverride;
  final ScannerClock? clock;

  @override
  State<ScannerScreen> createState() => _ScannerScreenState();
}

final class _ScannerScreenState extends State<ScannerScreen>
    with WidgetsBindingObserver {
  static const Duration _ausenciaParaNovaApresentacao = Duration(
    milliseconds: 1500,
  );
  static const Duration _feedbackDisplayDuration = Duration(milliseconds: 2200);

  MobileScannerController? _cameraController;
  late final ScannerViewModel _viewModel;
  late final ScannerSensoryFeedback _sensoryFeedback;
  late final ScannerClock _clock;
  final TextEditingController _cpfController = TextEditingController();
  StreamSubscription<BarcodeCapture>? _barcodeSubscription;
  StreamSubscription<BarcodeCapture>? _testBarcodeSubscription;
  StreamSubscription<String>? _payloadSubscription;
  StreamSubscription<ScannerFeedback>? _feedbackSubscription;
  ScannerFeedback? _feedback;
  Timer? _feedbackTimer;
  int _feedbackSequence = 0;
  String? _codigoAtualmenteNoQuadro;
  DateTime? _ultimaPresencaNoQuadroEm;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);

    _clock = widget.clock ?? DateTime.now;
    _sensoryFeedback =
        widget.sensoryFeedback ?? const PlatformScannerSensoryFeedback();
    _viewModel = ScannerViewModel(
      credenciamento: widget.credenciamento,
      tipoAcao: widget.tipoAcao ?? TipoAcao.auto,
      dispositivoId: widget.dispositivoId,
      contextoAtivo: widget.contextoAtivo,
      sessaoAtiva: widget.sessaoAtiva,
      localParticipants: widget.localParticipants,
      clock: _clock,
    );

    _feedbackSubscription = _viewModel.feedbacks.listen(_mostrarFeedback);
    _payloadSubscription = widget.payloadStream?.listen((rawPayload) {
      unawaited(_viewModel.processarPayload(rawPayload));
    });
    _testBarcodeSubscription = widget.barcodeCaptureStream?.listen(
      _processarCaptura,
    );

    if (widget.cameraPreviewOverride == null) {
      final controller = MobileScannerController(
        autoStart: false,
        detectionSpeed: DetectionSpeed.normal,
        detectionTimeoutMs: 120,
        formats: const <BarcodeFormat>[BarcodeFormat.qrCode],
      );
      _cameraController = controller;
      _assinarLeituras();
      unawaited(controller.start());
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    final controller = _cameraController;
    if (controller == null || !controller.value.hasCameraPermission) {
      return;
    }

    switch (state) {
      case AppLifecycleState.resumed:
        _assinarLeituras();
        unawaited(controller.start());
      case AppLifecycleState.inactive:
      case AppLifecycleState.paused:
      case AppLifecycleState.hidden:
      case AppLifecycleState.detached:
        unawaited(_barcodeSubscription?.cancel());
        _barcodeSubscription = null;
        unawaited(controller.stop());
    }
  }

  void _assinarLeituras() {
    final controller = _cameraController;
    if (controller == null) {
      return;
    }
    _barcodeSubscription ??= controller.barcodes.listen(
      _processarCaptura,
      onError: (_, _) => _viewModel.reportarErroDaCamera(),
      cancelOnError: false,
    );
  }

  void _processarCaptura(BarcodeCapture capture) {
    String? rawValue;
    for (final barcode in capture.barcodes) {
      final candidate = barcode.rawValue;
      if (candidate != null && candidate.isNotEmpty) {
        rawValue = candidate;
        break;
      }
    }

    final agora = _clock();
    if (rawValue == null) {
      _codigoAtualmenteNoQuadro = null;
      _ultimaPresencaNoQuadroEm = null;
      return;
    }

    final mesmoCodigoNoQuadro =
        _codigoAtualmenteNoQuadro == rawValue &&
        _ultimaPresencaNoQuadroEm != null &&
        agora.difference(_ultimaPresencaNoQuadroEm!) <
            _ausenciaParaNovaApresentacao;

    if (mesmoCodigoNoQuadro) {
      _ultimaPresencaNoQuadroEm = agora;
      return;
    }

    if (_viewModel.processando) {
      return;
    }

    _codigoAtualmenteNoQuadro = rawValue;
    _ultimaPresencaNoQuadroEm = agora;
    unawaited(_viewModel.processarPayload(rawValue));
  }

  void _mostrarFeedback(ScannerFeedback feedback) {
    if (!mounted) {
      return;
    }

    _feedbackTimer?.cancel();
    setState(() {
      _feedback = feedback;
      _feedbackSequence++;
    });

    switch (feedback.type) {
      case ScannerFeedbackType.processando:
        return;
      case ScannerFeedbackType.sucesso:
        unawaited(_sensoryFeedback.playSuccess());
      case ScannerFeedbackType.offline:
        unawaited(_sensoryFeedback.playOffline());
      case ScannerFeedbackType.naoInscrito:
        unawaited(_sensoryFeedback.playNotEnrolled());
      case ScannerFeedbackType.duplicado:
        unawaited(_sensoryFeedback.playDuplicate());
      case ScannerFeedbackType.erro:
        unawaited(_sensoryFeedback.playError());
    }

    _feedbackTimer = Timer(_feedbackDisplayDuration, () {
      if (mounted) {
        setState(() => _feedback = null);
      }
    });
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _feedbackTimer?.cancel();
    _cpfController.dispose();
    unawaited(_barcodeSubscription?.cancel());
    unawaited(_testBarcodeSubscription?.cancel());
    unawaited(_payloadSubscription?.cancel());
    unawaited(_feedbackSubscription?.cancel());
    _viewModel.dispose();
    final controller = _cameraController;
    super.dispose();
    if (controller != null) {
      unawaited(controller.dispose());
    }
  }

  @override
  Widget build(BuildContext context) {
    final contexto = widget.contextoAtivo;
    final sessao = widget.sessaoAtiva;
    final controller = _cameraController;
    final bannerLabel = contexto?.bannerLabel ?? sessao?.contextoBanner;
    final bannerIsSubevento = contexto != null
        ? contexto.isSubevento
        : (sessao != null && !sessao.isEventoGeral);
    final bannerOfflineCount =
        contexto == null ? sessao?.totalParticipantesCarregados : null;

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        title: Text(_tituloDaAcao(widget.tipoAcao)),
        actions: <Widget>[
          IconButton(
            tooltip: 'Digitar CPF',
            onPressed: _abrirEntradaManual,
            icon: const Icon(Icons.keyboard),
          ),
          if (controller != null)
            ValueListenableBuilder<MobileScannerState>(
              valueListenable: controller,
              builder: (context, state, _) {
                final torchLigada = state.torchState == TorchState.on;
                return IconButton(
                  tooltip: torchLigada
                      ? 'Desligar lanterna'
                      : 'Ligar lanterna',
                  onPressed: controller.toggleTorch,
                  icon: Icon(torchLigada ? Icons.flash_on : Icons.flash_off),
                );
              },
            ),
        ],
      ),
      body: SafeArea(
        top: false,
        child: Column(
          children: <Widget>[
            if (bannerLabel != null)
              _ActiveSessionBanner(
                bannerLabel: bannerLabel,
                isSubevento: bannerIsSubevento,
                offlineCount: bannerOfflineCount,
              ),
            Expanded(
              child: Stack(
                fit: StackFit.expand,
                children: <Widget>[
                  if (widget.cameraPreviewOverride != null)
                    widget.cameraPreviewOverride!
                  else if (controller != null)
                    MobileScanner(
                      controller: controller,
                      fit: BoxFit.cover,
                      tapToFocus: true,
                      errorBuilder: _cameraErrorBuilder,
                    ),
                  _ScannerFrame(feedbackType: _feedback?.type),
                  if (_feedback == null)
                    _ProcessingIndicator(viewModel: _viewModel),
                  _FeedbackBanner(
                    key: ValueKey<int>(_feedbackSequence),
                    feedback: _feedback,
                  ),
                ],
              ),
            ),
            _RecentHistoryPanel(
              viewModel: _viewModel,
              onOpenCpfModal: _abrirEntradaManual,
            ),
          ],
        ),
      ),
    );
  }

  Widget _cameraErrorBuilder(
    BuildContext context,
    MobileScannerException error,
  ) {
    return const ColoredBox(
      color: Colors.black,
      child: Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Não foi possível acessar a câmera. Verifique a permissão do app.',
            textAlign: TextAlign.center,
            style: TextStyle(color: Colors.white, fontSize: 18),
          ),
        ),
      ),
    );
  }

  String _tituloDaAcao(TipoAcao? tipoAcao) {
    return switch (tipoAcao) {
      null => 'Escanear QR Code',
      TipoAcao.entrada => 'Registrar ENTRADA',
      TipoAcao.saida => 'Registrar SAÍDA',
      TipoAcao.auto => 'Registrar Frequência',
    };
  }

  Future<void> _abrirEntradaManual() async {
    _cpfController.clear();
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      builder: (context) {
        String? errorText;
        return StatefulBuilder(
          builder: (context, setSheetState) {
            void registrar() {
              final cpf = _cpfController.text.trim();
              if (!CpfValidator.isValid(cpf)) {
                setSheetState(() => errorText = 'Informe um CPF válido');
                return;
              }

              Navigator.of(context).pop();
              unawaited(_viewModel.processarCpf(cpf));
            }

            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 20,
                bottom: MediaQuery.viewInsetsOf(context).bottom + 20,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  Text(
                    'Digitar CPF',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: _cpfController,
                    autofocus: true,
                    keyboardType: TextInputType.number,
                    maxLength: 11,
                    inputFormatters: <TextInputFormatter>[
                      FilteringTextInputFormatter.digitsOnly,
                      LengthLimitingTextInputFormatter(11),
                    ],
                    textInputAction: TextInputAction.done,
                    decoration: InputDecoration(
                      labelText: 'CPF',
                      hintText: '***.***.***-**',
                      prefixIcon: const Icon(Icons.badge),
                      errorText: errorText,
                    ),
                    onChanged: (_) {
                      if (errorText != null) {
                        setSheetState(() => errorText = null);
                      }
                    },
                    onSubmitted: (_) => registrar(),
                  ),
                  const SizedBox(height: 8),
                  FilledButton.icon(
                    onPressed: registrar,
                    icon: const Icon(Icons.check),
                    label: const Text('Registrar'),
                    style: FilledButton.styleFrom(
                      minimumSize: const Size.fromHeight(56),
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

final class _ActiveSessionBanner extends StatelessWidget {
  const _ActiveSessionBanner({
    required this.bannerLabel,
    required this.isSubevento,
    this.offlineCount,
  });

  final String bannerLabel;
  final bool isSubevento;
  final int? offlineCount;

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('scanner-context-banner'),
      width: double.infinity,
      color: const Color(0xFF175CD3),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      child: Row(
        children: <Widget>[
          Icon(
            isSubevento ? Icons.school : Icons.event,
            color: Colors.white,
            size: 18,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              bannerLabel,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 13,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          if (offlineCount != null) ...<Widget>[
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.18),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                '$offlineCount offline',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

final class _ScannerFrame extends StatelessWidget {
  const _ScannerFrame({required this.feedbackType});

  final ScannerFeedbackType? feedbackType;

  @override
  Widget build(BuildContext context) {
    final borderColor = _frameColor(feedbackType);
    final hasFeedback = feedbackType != null;

    return IgnorePointer(
      child: Stack(
        fit: StackFit.expand,
        children: <Widget>[
          if (hasFeedback)
            AnimatedContainer(
              duration: const Duration(milliseconds: 120),
              decoration: BoxDecoration(
                border: Border.all(
                  color: borderColor.withValues(alpha: 0.85),
                  width: 6,
                ),
              ),
            ),
          Center(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final side = math
                    .min(
                      250.0,
                      math.min(constraints.maxWidth, constraints.maxHeight) *
                          0.64,
                    )
                    .clamp(110.0, 250.0);
                return AnimatedContainer(
                  key: const Key('scanner-frame'),
                  duration: const Duration(milliseconds: 120),
                  width: side,
                  height: side,
                  decoration: BoxDecoration(
                    color: hasFeedback
                        ? borderColor.withValues(alpha: 0.12)
                        : Colors.transparent,
                    border: Border.all(
                      color: borderColor,
                      width: hasFeedback ? 5 : 3,
                    ),
                    borderRadius: BorderRadius.circular(24),
                    boxShadow: hasFeedback
                        ? <BoxShadow>[
                            BoxShadow(
                              color: borderColor.withValues(alpha: 0.45),
                              blurRadius: 20,
                              spreadRadius: 2,
                            ),
                          ]
                        : null,
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  static Color _frameColor(ScannerFeedbackType? type) {
    return switch (type) {
      null => Colors.white,
      ScannerFeedbackType.processando => const Color(0xFF3B82F6),
      ScannerFeedbackType.sucesso ||
      ScannerFeedbackType.offline => const Color(0xFF22C55E),
      ScannerFeedbackType.naoInscrito ||
      ScannerFeedbackType.erro => const Color(0xFFEF4444),
      ScannerFeedbackType.duplicado => const Color(0xFFFACC15),
    };
  }
}

final class _ProcessingIndicator extends StatelessWidget {
  const _ProcessingIndicator({required this.viewModel});

  final ScannerViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      left: 24,
      right: 24,
      bottom: 12,
      child: AnimatedBuilder(
        animation: viewModel,
        builder: (context, _) {
          final text = viewModel.processando
              ? 'Processando ${viewModel.leiturasEmProcessamento} leitura(s)...'
              : 'Aponte para o QR Code';
          return Semantics(
            liveRegion: true,
            child: Center(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: Colors.black.withValues(alpha: 0.72),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 8,
                  ),
                  child: Text(
                    text,
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: Colors.white, fontSize: 14),
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

final class _FeedbackBanner extends StatelessWidget {
  const _FeedbackBanner({required this.feedback, super.key});

  final ScannerFeedback? feedback;

  @override
  Widget build(BuildContext context) {
    final item = feedback;
    return Positioned(
      top: 12,
      left: 16,
      right: 16,
      child: IgnorePointer(
        child: AnimatedSwitcher(
          duration: const Duration(milliseconds: 120),
          child: item == null
              ? const SizedBox.shrink()
              : Semantics(
                  liveRegion: true,
                  child: Container(
                    key: const Key('scanner-feedback-banner'),
                    decoration: BoxDecoration(
                      color: _backgroundColor(item.type),
                      borderRadius: BorderRadius.circular(16),
                      boxShadow: const <BoxShadow>[
                        BoxShadow(
                          color: Colors.black45,
                          blurRadius: 12,
                          offset: Offset(0, 4),
                        ),
                      ],
                    ),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 12,
                    ),
                    child: Row(
                      children: <Widget>[
                        if (item.type == ScannerFeedbackType.processando)
                          SizedBox.square(
                            dimension: 28,
                            child: CircularProgressIndicator(
                              strokeWidth: 3,
                              color: _foregroundColor(item.type),
                            ),
                          )
                        else
                          Icon(
                            _icon(item.type),
                            color: _foregroundColor(item.type),
                            size: 30,
                          ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: _FeedbackContent(
                            feedback: item,
                            foregroundColor: _foregroundColor(item.type),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
        ),
      ),
    );
  }

  static Color _backgroundColor(ScannerFeedbackType type) {
    return switch (type) {
      ScannerFeedbackType.processando => const Color(0xFF1D4ED8),
      ScannerFeedbackType.sucesso => const Color(0xFF15803D),
      ScannerFeedbackType.offline => const Color(0xFF166534),
      ScannerFeedbackType.naoInscrito ||
      ScannerFeedbackType.erro => const Color(0xFFB42318),
      ScannerFeedbackType.duplicado => const Color(0xFFFACC15),
    };
  }

  static Color _foregroundColor(ScannerFeedbackType type) {
    return switch (type) {
      ScannerFeedbackType.duplicado => const Color(0xFF111827),
      _ => Colors.white,
    };
  }

  static IconData _icon(ScannerFeedbackType type) {
    return switch (type) {
      ScannerFeedbackType.processando => Icons.sync,
      ScannerFeedbackType.sucesso => Icons.check_circle,
      ScannerFeedbackType.offline => Icons.cloud_off,
      ScannerFeedbackType.naoInscrito => Icons.block,
      ScannerFeedbackType.duplicado => Icons.hourglass_top,
      ScannerFeedbackType.erro => Icons.error,
    };
  }
}

final class _FeedbackContent extends StatelessWidget {
  const _FeedbackContent({
    required this.feedback,
    required this.foregroundColor,
  });

  final ScannerFeedback feedback;
  final Color foregroundColor;

  @override
  Widget build(BuildContext context) {
    final titulo = _titulo(feedback);
    final subtitulo = _subtitulo(feedback);

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        if (feedback.statusLabel != null)
          Row(
            children: <Widget>[
              Flexible(
                child: Text(
                  feedback.statusLabel!,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: foregroundColor.withValues(alpha: 0.9),
                    fontSize: 12,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 0.6,
                  ),
                ),
              ),
              if (feedback.type == ScannerFeedbackType.offline) ...<Widget>[
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 6,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFB45309),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: const Text(
                    'OFFLINE',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ],
            ],
          ),
        Text(
          titulo,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: foregroundColor,
            fontSize: 17,
            fontWeight: FontWeight.w800,
          ),
        ),
        if (subtitulo != null && subtitulo.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 2),
            child: Text(
              subtitulo,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: foregroundColor.withValues(alpha: 0.92),
                fontSize: 13,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
      ],
    );
  }

  static String _titulo(ScannerFeedback feedback) {
    if (feedback.type == ScannerFeedbackType.sucesso ||
        feedback.type == ScannerFeedbackType.offline ||
        feedback.type == ScannerFeedbackType.processando) {
      return feedback.participantName ?? feedback.message;
    }
    if (feedback.type == ScannerFeedbackType.erro &&
        feedback.message == 'QR Code invalido') {
      return 'QR Code inválido';
    }
    return feedback.message;
  }

  static String? _subtitulo(ScannerFeedback feedback) {
    if (feedback.type == ScannerFeedbackType.duplicado ||
        feedback.type == ScannerFeedbackType.naoInscrito) {
      final parts = <String>[
        if (feedback.participantName != null &&
            feedback.participantName!.isNotEmpty)
          feedback.participantName!,
        if (feedback.detail != null && feedback.detail!.isNotEmpty)
          feedback.detail!,
      ];
      return parts.isEmpty ? null : parts.join(' • ');
    }
    return feedback.detail;
  }
}

final class _RecentHistoryPanel extends StatelessWidget {
  const _RecentHistoryPanel({
    required this.viewModel,
    required this.onOpenCpfModal,
  });

  final ScannerViewModel viewModel;
  final VoidCallback onOpenCpfModal;

  @override
  Widget build(BuildContext context) {
    final maxPanelHeight = (MediaQuery.sizeOf(context).height * 0.34)
        .clamp(120.0, 260.0);

    return Container(
      key: const Key('scanner-recent-history'),
      width: double.infinity,
      constraints: BoxConstraints(maxHeight: maxPanelHeight),
      decoration: const BoxDecoration(
        color: Color(0xFF101828),
        border: Border(top: BorderSide(color: Color(0xFF1D2939))),
      ),
      padding: const EdgeInsets.fromLTRB(14, 8, 14, 10),
      child: AnimatedBuilder(
        animation: viewModel,
        builder: (context, _) {
          final registros = viewModel.historicoRecente;
          return Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Row(
                children: <Widget>[
                  const Icon(Icons.history, color: Colors.white70, size: 16),
                  const SizedBox(width: 6),
                  const Expanded(
                    child: Text(
                      'Últimos registros',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 2,
                    ),
                    decoration: BoxDecoration(
                      color: const Color(0xFF1D2939),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Text(
                      '${registros.length}/${ScannerViewModel.maxHistoricoRecente}',
                      style: const TextStyle(
                        color: Colors.white70,
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  TextButton.icon(
                    onPressed: onOpenCpfModal,
                    icon: const Icon(Icons.keyboard, size: 16),
                    label: const Text('CPF'),
                    style: TextButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: const Color(0xFF1D2939),
                      visualDensity: VisualDensity.compact,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 4,
                      ),
                      minimumSize: Size.zero,
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              if (registros.isEmpty)
                const Padding(
                  padding: EdgeInsets.symmetric(vertical: 6),
                  child: Text(
                    'Nenhum registro nesta sessão',
                    style: TextStyle(color: Colors.white60, fontSize: 12),
                  ),
                )
              else
                Flexible(
                  child: ListView.separated(
                    shrinkWrap: true,
                    padding: EdgeInsets.zero,
                    itemCount: registros.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 4),
                    itemBuilder: (context, index) =>
                        _RecentHistoryRow(record: registros[index]),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}

final class _RecentHistoryRow extends StatelessWidget {
  const _RecentHistoryRow({required this.record});

  final RecentScanRecord record;

  @override
  Widget build(BuildContext context) {
    final isSaida = record.tipoAcao == TipoAcao.saida;
    final badgeColor = isSaida
        ? const Color(0xFFB42318)
        : const Color(0xFF15803D);
    final horaLocal = record.registradoEm.toLocal();
    final hh = horaLocal.hour.toString().padLeft(2, '0');
    final mm = horaLocal.minute.toString().padLeft(2, '0');
    final ss = horaLocal.second.toString().padLeft(2, '0');

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFF1D2939),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: <Widget>[
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
            decoration: BoxDecoration(
              color: badgeColor,
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text(
              record.statusLabel,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 10,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              record.participanteNome,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 13,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          if (record.offline) ...<Widget>[
            const SizedBox(width: 6),
            const Icon(Icons.cloud_off, color: Color(0xFFF59E0B), size: 14),
          ],
          const SizedBox(width: 8),
          Text(
            '$hh:$mm:$ss',
            style: const TextStyle(
              color: Colors.white60,
              fontSize: 11,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
