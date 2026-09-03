import 'dart:async';

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/features/scanner/view_models/scanner_view_model.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

final class ScannerScreen extends StatefulWidget {
  const ScannerScreen({
    required this.tipoAcao,
    required this.credenciamento,
    required this.dispositivoId,
    super.key,
  });

  final TipoAcao tipoAcao;
  final CredenciamentoGateway credenciamento;
  final String dispositivoId;

  @override
  State<ScannerScreen> createState() => _ScannerScreenState();
}

final class _ScannerScreenState extends State<ScannerScreen>
    with WidgetsBindingObserver {
  late final MobileScannerController _cameraController;
  late final ScannerViewModel _viewModel;
  final TextEditingController _cpfController = TextEditingController();
  StreamSubscription<BarcodeCapture>? _barcodeSubscription;
  StreamSubscription<ScannerFeedback>? _feedbackSubscription;
  ScannerFeedback? _feedback;
  Timer? _feedbackTimer;
  int _feedbackSequence = 0;
  bool _capturaAceita = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);

    _viewModel = ScannerViewModel(
      credenciamento: widget.credenciamento,
      tipoAcao: widget.tipoAcao,
      dispositivoId: widget.dispositivoId,
    );
    _cameraController = MobileScannerController(
      autoStart: false,
      detectionSpeed: DetectionSpeed.normal,
      detectionTimeoutMs: 250,
      formats: const <BarcodeFormat>[BarcodeFormat.qrCode],
    );

    _assinarLeituras();
    _feedbackSubscription = _viewModel.feedbacks.listen(_mostrarFeedback);
    unawaited(_cameraController.start());
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (!_cameraController.value.hasCameraPermission) {
      return;
    }

    switch (state) {
      case AppLifecycleState.resumed:
        _assinarLeituras();
        unawaited(_cameraController.start());
      case AppLifecycleState.inactive:
      case AppLifecycleState.paused:
      case AppLifecycleState.hidden:
      case AppLifecycleState.detached:
        unawaited(_barcodeSubscription?.cancel());
        _barcodeSubscription = null;
        unawaited(_cameraController.stop());
    }
  }

  void _assinarLeituras() {
    _barcodeSubscription ??= _cameraController.barcodes.listen(
      _processarCaptura,
      onError: (_, _) => _viewModel.reportarErroDaCamera(),
      cancelOnError: false,
    );
  }

  void _processarCaptura(BarcodeCapture capture) {
    if (_capturaAceita || _viewModel.processando) {
      return;
    }

    for (final barcode in capture.barcodes) {
      if (barcode.rawValue == null || barcode.rawValue!.isEmpty) {
        continue;
      }
      _capturaAceita = true;
      unawaited(_viewModel.processarPayload(barcode.rawValue));
      return;
    }
  }

  void _mostrarFeedback(ScannerFeedback feedback) {
    if (!mounted) {
      return;
    }

    _feedbackTimer?.cancel();
    _cpfController.dispose();
    setState(() {
      _feedback = feedback;
      _feedbackSequence++;
    });

    switch (feedback.type) {
      case ScannerFeedbackType.sucesso:
        unawaited(SystemSound.play(SystemSoundType.click));
        unawaited(HapticFeedback.mediumImpact());
        _agendarRetornoAoDashboard();
      case ScannerFeedbackType.offline:
        unawaited(SystemSound.play(SystemSoundType.click));
        unawaited(HapticFeedback.heavyImpact());
        _agendarRetornoAoDashboard();
      case ScannerFeedbackType.erro:
        _capturaAceita = false;
        unawaited(SystemSound.play(SystemSoundType.alert));
        unawaited(HapticFeedback.vibrate());
        _feedbackTimer = Timer(const Duration(milliseconds: 1600), () {
          if (mounted) {
            setState(() => _feedback = null);
          }
        });
    }
  }

  void _agendarRetornoAoDashboard() {
    _capturaAceita = true;
    unawaited(_barcodeSubscription?.cancel());
    _barcodeSubscription = null;
    unawaited(_cameraController.stop());
    _feedbackTimer = Timer(const Duration(milliseconds: 700), () {
      if (mounted) {
        Navigator.of(context).pop();
      }
    });
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _feedbackTimer?.cancel();
    unawaited(_barcodeSubscription?.cancel());
    unawaited(_feedbackSubscription?.cancel());
    _viewModel.dispose();
    super.dispose();
    unawaited(_cameraController.dispose());
  }

  @override
  Widget build(BuildContext context) {
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
          ValueListenableBuilder<MobileScannerState>(
            valueListenable: _cameraController,
            builder: (context, state, _) {
              final torchLigada = state.torchState == TorchState.on;
              return IconButton(
                tooltip: torchLigada ? 'Desligar lanterna' : 'Ligar lanterna',
                onPressed: _cameraController.toggleTorch,
                icon: Icon(torchLigada ? Icons.flash_on : Icons.flash_off),
              );
            },
          ),
        ],
      ),
      body: Stack(
        fit: StackFit.expand,
        children: <Widget>[
          MobileScanner(
            controller: _cameraController,
            fit: BoxFit.cover,
            tapToFocus: true,
            errorBuilder: _cameraErrorBuilder,
          ),
          const _ScannerFrame(),
          _ProcessingIndicator(viewModel: _viewModel),
          _ManualEntryButton(onPressed: _abrirEntradaManual),
          _FeedbackToast(
            key: ValueKey<int>(_feedbackSequence),
            feedback: _feedback,
          ),
        ],
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
            'Nao foi possivel acessar a camera. Verifique a permissao do app.',
            textAlign: TextAlign.center,
            style: TextStyle(color: Colors.white, fontSize: 18),
          ),
        ),
      ),
    );
  }

  String _tituloDaAcao(TipoAcao tipoAcao) {
    return switch (tipoAcao) {
      TipoAcao.entrada => 'Registrar ENTRADA',
      TipoAcao.saida => 'Registrar SAIDA',
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

              _capturaAceita = true;
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

final class _ScannerFrame extends StatelessWidget {
  const _ScannerFrame();

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: Center(
        child: Container(
          width: 280,
          height: 280,
          decoration: BoxDecoration(
            border: Border.all(color: Colors.white, width: 3),
            borderRadius: BorderRadius.circular(24),
          ),
        ),
      ),
    );
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
      bottom: 32,
      child: AnimatedBuilder(
        animation: viewModel,
        builder: (context, _) {
          final text = viewModel.processando
              ? 'Processando ${viewModel.leiturasEmProcessamento} leitura(s)'
              : 'Aponte para o QR Code';
          return Semantics(
            liveRegion: true,
            child: DecoratedBox(
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.72),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: 20,
                  vertical: 14,
                ),
                child: Text(
                  text,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.white, fontSize: 16),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

final class _ManualEntryButton extends StatelessWidget {
  const _ManualEntryButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      left: 24,
      right: 24,
      bottom: 104,
      child: SafeArea(
        child: OutlinedButton.icon(
          onPressed: onPressed,
          icon: const Icon(Icons.keyboard),
          label: const Text('Digitar CPF'),
          style: OutlinedButton.styleFrom(
            backgroundColor: Colors.white.withValues(alpha: 0.92),
            foregroundColor: Colors.black,
            minimumSize: const Size.fromHeight(52),
            side: BorderSide.none,
          ),
        ),
      ),
    );
  }
}

final class _FeedbackToast extends StatelessWidget {
  const _FeedbackToast({required this.feedback, super.key});

  final ScannerFeedback? feedback;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 20,
      left: 20,
      right: 20,
      child: SafeArea(
        child: IgnorePointer(
          child: AnimatedSwitcher(
            duration: const Duration(milliseconds: 120),
            child: feedback == null
                ? const SizedBox.shrink()
                : Semantics(
                    liveRegion: true,
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        color: _backgroundColor(feedback!.type),
                        borderRadius: BorderRadius.circular(16),
                        boxShadow: const <BoxShadow>[
                          BoxShadow(
                            color: Colors.black38,
                            blurRadius: 12,
                            offset: Offset(0, 4),
                          ),
                        ],
                      ),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 20,
                          vertical: 18,
                        ),
                        child: Row(
                          children: <Widget>[
                            Icon(
                              _icon(feedback!.type),
                              color: Colors.white,
                              size: 30,
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                feedback!.message,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 18,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
          ),
        ),
      ),
    );
  }

  Color _backgroundColor(ScannerFeedbackType type) {
    return switch (type) {
      ScannerFeedbackType.sucesso => const Color(0xFF16803C),
      ScannerFeedbackType.offline => const Color(0xFFB45309),
      ScannerFeedbackType.erro => const Color(0xFFB42318),
    };
  }

  IconData _icon(ScannerFeedbackType type) {
    return switch (type) {
      ScannerFeedbackType.sucesso => Icons.check_circle,
      ScannerFeedbackType.offline => Icons.cloud_off,
      ScannerFeedbackType.erro => Icons.error,
    };
  }
}
