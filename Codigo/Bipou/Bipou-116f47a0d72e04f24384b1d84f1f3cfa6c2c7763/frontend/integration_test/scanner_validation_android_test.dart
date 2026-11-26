import 'dart:async';
import 'dart:typed_data';

import 'package:bipou_frontend/main.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/audit_log_export_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:qr_flutter/qr_flutter.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'fluxo completo Android: contexto, validacao QR v1, feedbacks, cooldown e ultimos 5',
    (tester) async {
      final payloadController = StreamController<String>.broadcast();
      final activeBadge = ValueNotifier<_ActivePreviewBadge?>(null);
      final credenciamento = _InMemoryCredenciamentoGateway();
      addTearDown(() async {
        activeBadge.dispose();
        await payloadController.close();
      });

      Future<void> emitQr({
        required String label,
        required String rawPayload,
      }) async {
        activeBadge.value = _ActivePreviewBadge(
          label: label,
          rawPayload: rawPayload,
        );
        await tester.pump();
        payloadController.add(rawPayload);
        await tester.pump();
        expect(find.byKey(const Key('scanner-feedback-banner')), findsOneWidget);
        await _pauseAndPump(tester, const Duration(milliseconds: 220));
      }

      await tester.pumpWidget(
        BipouApp(
          credenciamento: credenciamento,
          auditLogExporter: _NoOpAuditExporter(),
          qrCodeShare: _NoOpQrShareGateway(),
          dispositivoId: 'android-portaria-01',
          scannerPayloadStream: payloadController.stream,
          scannerCameraPreviewOverride: _CameraPreviewSimulator(
            activeBadge: activeBadge,
          ),
        ),
      );
      await _pauseAndPump(tester, const Duration(milliseconds: 1000));

      // 1. Configurar Evento 15 e Curso A (42) na interface Android
      await tester.tap(find.byKey(const Key('dashboard-context-card')));
      await _pauseAndPump(tester, const Duration(milliseconds: 700));

      await _enterTextField(
        tester,
        const Key('context-evento-id-field'),
        '15',
      );
      await _enterTextField(
        tester,
        const Key('context-evento-nome-field'),
        'Seminfo 2026',
      );
      await _enterTextField(
        tester,
        const Key('context-subevento-id-field'),
        '42',
      );
      await _enterTextField(
        tester,
        const Key('context-subevento-nome-field'),
        'Curso A - Flutter',
      );
      await _dismissKeyboardAndTap(
        tester,
        const Key('context-apply-button'),
      );
      await _pauseAndPump(tester, const Duration(milliseconds: 900));

      expect(
        find.text('[Evento: Seminfo 2026] » [Curso: Curso A - Flutter]'),
        findsOneWidget,
      );

      // 2. Abrir Scanner diretamente (sem modal intermediario)
      await tester.tap(find.text('Escanear QR Code'));
      await _pauseAndPump(tester, const Duration(milliseconds: 900));

      expect(
        find.text('[Evento: Seminfo 2026] » [Curso: Curso A - Flutter]'),
        findsOneWidget,
      );

      // 3. Sucesso (Vetor Golden RFC-0001 criado com o contrato de Lorena)
      const mariaPayload =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      await emitQr(
        label: 'Maria Santos • sid: [42, 43]',
        rawPayload: mariaPayload,
      );
      expect(find.text('ENTRADA REGISTRADA'), findsOneWidget);
      expect(find.text('1/5'), findsOneWidget);
      await _pauseAndPump(tester, const Duration(milliseconds: 1800));

      // 4. Duplicado dentro do cooldown de 5s -> moldura amarela
      await emitQr(
        label: 'Releitura < 5s: Maria Santos',
        rawPayload: mariaPayload,
      );
      expect(find.text('Aguarde para bipar novamente'), findsOneWidget);
      expect(find.text('1/5'), findsOneWidget);
      await _pauseAndPump(tester, const Duration(milliseconds: 1800));

      // 5. Nao inscrito no Curso A (apenas Curso B = 43) -> moldura vermelha
      final carlosCursoB = ParticipanteQrPayload.v1(
        pid: 2001,
        cpf: '52998224725',
        nome: 'Carlos Souza',
        eid: 15,
        sid: const <int>[43],
      ).toRawJson();
      await emitQr(
        label: 'Carlos Souza • sid: [43]',
        rawPayload: carlosCursoB,
      );
      expect(find.text('Não inscrito neste curso!'), findsOneWidget);
      expect(find.text('1/5'), findsOneWidget);
      await _pauseAndPump(tester, const Duration(milliseconds: 1800));

      // 6. QR Code adulterado (hash divergente) -> moldura vermelha
      const adulteradoPayload =
          '{"v":1,"pid":9999,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      await emitQr(
        label: 'Crachá Adulterado (pid: 9999)',
        rawPayload: adulteradoPayload,
      );
      expect(find.text('QR Code inválido'), findsOneWidget);
      expect(find.text('Assinatura hash divergente'), findsOneWidget);
      await _pauseAndPump(tester, const Duration(milliseconds: 1800));

      // 7. Leitura continua de mais 5 participantes -> mantem ultimos 5 no painel
      final extras = <(int, String)>[
        (3001, 'Ana Paula'),
        (3002, 'Bruno Lima'),
        (3003, 'Carla Dias'),
        (3004, 'Diego Melo'),
        (3005, 'Elisa Rocha'),
      ];
      for (final (pid, nome) in extras) {
        final payload = ParticipanteQrPayload.v1(
          pid: pid,
          cpf: '52998224725',
          nome: nome,
          eid: 15,
          sid: const <int>[42],
        ).toRawJson();
        await emitQr(label: '$nome • sid: [42]', rawPayload: payload);
        expect(find.text('ENTRADA REGISTRADA'), findsOneWidget);
        await _pauseAndPump(tester, const Duration(milliseconds: 1000));
      }

      expect(find.text('5/5'), findsOneWidget);
      expect(find.text('Elisa Rocha'), findsWidgets);
      expect(find.text('Ana Paula'), findsOneWidget);
      expect(find.text('Maria Santos'), findsNothing);
      await _pauseAndPump(tester, const Duration(milliseconds: 1200));

      // 8. Trocar para Curso B (43) e abrir modo Saida (criterio de aceite 1)
      await tester.pageBack();
      await _pauseAndPump(tester, const Duration(milliseconds: 800));

      await tester.tap(find.byKey(const Key('dashboard-context-card')));
      await _pauseAndPump(tester, const Duration(milliseconds: 600));
      await _enterTextField(
        tester,
        const Key('context-subevento-id-field'),
        '43',
      );
      await _enterTextField(
        tester,
        const Key('context-subevento-nome-field'),
        'Curso B - Segurança Web',
      );
      await _dismissKeyboardAndTap(
        tester,
        const Key('context-apply-button'),
      );
      await _pauseAndPump(tester, const Duration(milliseconds: 700));

      await tester.tap(find.text('Escanear QR Code'));
      await _pauseAndPump(tester, const Duration(milliseconds: 900));

      // Aluno apenas do Curso A (sid: [42]) e barrado na portaria do Curso B
      final alunoCursoA = ParticipanteQrPayload.v1(
        pid: 4001,
        cpf: '52998224725',
        nome: 'Ana Paula',
        eid: 15,
        sid: const <int>[42],
      ).toRawJson();
      await emitQr(
        label: 'Ana Paula • sid: [42] (Curso A)',
        rawPayload: alunoCursoA,
      );
      expect(find.text('Não inscrito neste curso!'), findsOneWidget);
      await _pauseAndPump(tester, const Duration(milliseconds: 1800));

      // Carlos Souza (sid: [43]) registra leitura no Curso B
      await emitQr(
        label: 'Carlos Souza • sid: [43] (Curso B)',
        rawPayload: carlosCursoB,
      );
      expect(find.text('ENTRADA REGISTRADA'), findsOneWidget);
      expect(find.text('1/5'), findsOneWidget);
      await _pauseAndPump(tester, const Duration(milliseconds: 2200));
    },
  );
}

Future<void> _pauseAndPump(
  WidgetTester tester,
  Duration duration,
) async {
  const step = Duration(milliseconds: 100);
  var elapsed = Duration.zero;
  while (elapsed < duration) {
    await Future<void>.delayed(step);
    await tester.pump();
    elapsed += step;
  }
}

Future<void> _enterTextField(
  WidgetTester tester,
  Key key,
  String text,
) async {
  final finder = find.byKey(key);
  await tester.ensureVisible(finder);
  await tester.enterText(finder, text);
  await _pauseAndPump(tester, const Duration(milliseconds: 250));
}

Future<void> _dismissKeyboardAndTap(WidgetTester tester, Key key) async {
  FocusManager.instance.primaryFocus?.unfocus();
  await _pauseAndPump(tester, const Duration(milliseconds: 350));
  final finder = find.byKey(key);
  await tester.ensureVisible(finder);
  await tester.tap(finder);
}

final class _ActivePreviewBadge {
  const _ActivePreviewBadge({required this.label, required this.rawPayload});

  final String label;
  final String rawPayload;
}

final class _CameraPreviewSimulator extends StatelessWidget {
  const _CameraPreviewSimulator({required this.activeBadge});

  final ValueNotifier<_ActivePreviewBadge?> activeBadge;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: <Color>[Color(0xFF0F172A), Color(0xFF1E293B)],
        ),
      ),
      child: ValueListenableBuilder<_ActivePreviewBadge?>(
        valueListenable: activeBadge,
        builder: (context, badge, _) {
          if (badge == null) {
            return const SizedBox.expand();
          }
          return Center(
            child: Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  QrImageView(
                    data: badge.rawPayload,
                    size: 132,
                    backgroundColor: Colors.white,
                  ),
                  const SizedBox(height: 6),
                  Text(
                    badge.label,
                    style: const TextStyle(
                      color: Color(0xFF101828),
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

final class _InMemoryCredenciamentoGateway implements CredenciamentoGateway {
  int _seq = 0;

  @override
  String get apiBaseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() async =>
      const <ParticipanteResponse>[];

  @override
  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
    int? idPessoa,
    int? idEvento,
    int? idSubEvento,
    int? eventoId,
    int? subeventoId,
    String? operadorUsuario,
  }) async {
    _seq++;
    final agora = DateTime.now().toUtc();
    return OperationResult<RegistroResponse>.enviado(
      RegistroResponse(
        id: 'reg-$_seq',
        leituraId: '550e8400-e29b-41d4-a716-446655440000',
        participanteId: participanteId,
        participanteNome: participanteNome,
        tipoAcao: tipoAcao,
        dataHora: agora,
        dataHoraLidaNoCelular: agora,
        dispositivoId: dispositivoId,
      ),
    );
  }

  @override
  Future<void> verificarConexao() async {}
}

final class _NoOpAuditExporter implements AuditLogExporter {
  @override
  Future<void> export() async {}
}

final class _NoOpQrShareGateway implements QrCodeShareGateway {
  @override
  Future<void> share({
    required Uint8List pngBytes,
    required String participanteNome,
  }) async {}
}
