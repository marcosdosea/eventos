import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:qr_flutter/qr_flutter.dart';

final class ParticipantQrScreen extends StatefulWidget {
  const ParticipantQrScreen({
    required this.participante,
    required this.qrCodeShare,
    super.key,
  });

  final ParticipanteResponse participante;
  final QrCodeShareGateway qrCodeShare;

  @override
  State<ParticipantQrScreen> createState() => _ParticipantQrScreenState();
}

final class _ParticipantQrScreenState extends State<ParticipantQrScreen> {
  final GlobalKey _credentialKey = GlobalKey();
  bool _sharing = false;

  @override
  Widget build(BuildContext context) {
    final payload = ParticipanteQrPayload(
      id: widget.participante.id,
      nome: widget.participante.nome,
    ).toRawJson();
    return Scaffold(
      appBar: AppBar(title: const Text('QR Code')),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              FilledButton.icon(
                onPressed: _sharing ? null : _shareCredential,
                icon: _sharing
                    ? const SizedBox.square(
                        dimension: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.ios_share),
                label: Text(
                  _sharing ? 'Preparando imagem...' : 'Compartilhar QR Code',
                ),
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFF16803C),
                  foregroundColor: Colors.white,
                  minimumSize: const Size.fromHeight(56),
                ),
              ),
              const SizedBox(height: 10),
              OutlinedButton.icon(
                onPressed: () => Navigator.of(context).pop(),
                icon: const Icon(Icons.check),
                label: const Text('Concluir'),
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size.fromHeight(52),
                ),
              ),
            ],
          ),
        ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
          child: ListView(
            children: <Widget>[
              Text(
                'Credencial pronta',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Compartilhe a imagem ou apresente o QR Code na portaria.',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 24),
              Center(
                child: RepaintBoundary(
                  key: _credentialKey,
                  child: _ParticipantCredentialCard(
                    participanteNome: widget.participante.nome,
                    qrPayload: payload,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _shareCredential() async {
    setState(() => _sharing = true);
    try {
      final pngBytes = await _captureCredential();
      await widget.qrCodeShare.share(
        pngBytes: pngBytes,
        participanteNome: widget.participante.nome,
      );
    } on Object catch (error) {
      if (!mounted) {
        return;
      }
      final message = error is QrCodeShareException
          ? error.message
          : 'Nao foi possivel compartilhar o QR Code.';
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) {
        setState(() => _sharing = false);
      }
    }
  }

  Future<Uint8List> _captureCredential() async {
    await WidgetsBinding.instance.endOfFrame;
    final renderObject = _credentialKey.currentContext?.findRenderObject();
    if (renderObject is! RenderRepaintBoundary) {
      throw const QrCodeShareException(
        'Nao foi possivel gerar a imagem do QR Code.',
      );
    }

    final image = await renderObject.toImage(pixelRatio: 3);
    try {
      final byteData = await image.toByteData(format: ui.ImageByteFormat.png);
      if (byteData == null) {
        throw const QrCodeShareException(
          'Nao foi possivel gerar a imagem do QR Code.',
        );
      }
      return byteData.buffer.asUint8List();
    } finally {
      image.dispose();
    }
  }
}

final class _ParticipantCredentialCard extends StatelessWidget {
  const _ParticipantCredentialCard({
    required this.participanteNome,
    required this.qrPayload,
  });

  final String participanteNome;
  final String qrPayload;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 340,
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 28),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: const Color(0xFFD0D5DD)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: <Widget>[
              Flexible(
                flex: 5,
                child: SizedBox(
                  width: 104,
                  height: 64,
                  child: Image.asset(
                    'logo-ufs-azul.png',
                    key: const Key('logo-ufs'),
                    fit: BoxFit.contain,
                    semanticLabel: 'Logo da Universidade Federal de Sergipe',
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Flexible(
                flex: 7,
                child: SizedBox(
                  width: 140,
                  height: 64,
                  child: Image.asset(
                    'MarcaDSI2026.png',
                    key: const Key('marca-dsi'),
                    fit: BoxFit.contain,
                    semanticLabel:
                        'Marca do Departamento de Sistemas de Informação',
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          const Text(
            'CREDENCIAL DO PARTICIPANTE',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Color(0xFF667085),
              fontSize: 11,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.8,
            ),
          ),
          const SizedBox(height: 20),
          Text(
            participanteNome,
            textAlign: TextAlign.center,
            maxLines: 3,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: Color(0xFF101828),
              fontSize: 22,
              fontWeight: FontWeight.w800,
              height: 1.15,
            ),
          ),
          const SizedBox(height: 18),
          QrImageView(
            data: qrPayload,
            version: QrVersions.auto,
            errorCorrectionLevel: QrErrorCorrectLevel.M,
            size: 240,
            padding: const EdgeInsets.all(8),
            backgroundColor: Colors.white,
          ),
          const SizedBox(height: 12),
          const Text(
            'Apresente este QR Code na portaria',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Color(0xFF475467),
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
