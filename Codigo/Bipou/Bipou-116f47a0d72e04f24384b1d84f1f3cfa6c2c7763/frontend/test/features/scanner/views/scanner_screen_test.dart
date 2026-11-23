import 'dart:async';

import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/services/scanner_sensory_feedback.dart';
import 'package:bipou_frontend/features/scanner/views/scanner_screen.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

void main() {
  testWidgets(
    'exibe moldura verde, feedback sensorial e historico continuo dos ultimos 5',
    (tester) async {
      final now = DateTime.utc(2026, 11, 19, 14, 0, 0);
      final payloadController = StreamController<String>.broadcast();
      final sensory = _SensoryFeedbackSpy();
      final credenciamento = _CredenciamentoScreenSpy();
      addTearDown(payloadController.close);

      await tester.pumpWidget(
        MaterialApp(
          home: ScannerScreen(
            tipoAcao: TipoAcao.entrada,
            credenciamento: credenciamento,
            dispositivoId: 'portaria-01',
            contextoAtivo: const ScannerSessionContext(
              eventoId: 15,
              eventoNome: 'Seminfo 2026',
              subeventoId: 42,
              subeventoNome: 'Curso A',
            ),
            sensoryFeedback: sensory,
            payloadStream: payloadController.stream,
            cameraPreviewOverride: const ColoredBox(color: Colors.black87),
            clock: () => now,
          ),
        ),
      );

      expect(
        find.text('[Evento: Seminfo 2026] » [Curso: Curso A]'),
        findsOneWidget,
      );
      expect(find.text('Nenhum registro nesta sessão'), findsOneWidget);

      // 1. Leitura valida (Vetor Golden RFC-0001)
      const goldenV1 =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      payloadController.add(goldenV1);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.successCalls, 1);
      expect(credenciamento.calls, 1);
      expect(credenciamento.lastEventoId, 15);
      expect(credenciamento.lastSubeventoId, 42);
      expect(find.text('ENTRADA REGISTRADA'), findsOneWidget);
      expect(find.text('Maria Santos'), findsNWidgets(2)); // banner + historico
      expect(_frameBorderColor(tester), const Color(0xFF22C55E));
      expect(find.text('1/5'), findsOneWidget);

      // 2. Leitura duplicada em < 5s -> moldura amarela
      payloadController.add(goldenV1);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.duplicateCalls, 1);
      expect(find.text('Aguarde para bipar novamente'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFFFACC15));
      expect(find.text('1/5'), findsOneWidget);

      // 3. Participante nao inscrito neste curso -> moldura vermelha
      final outroCurso = ParticipanteQrPayload.v1(
        pid: 2001,
        cpf: '52998224725',
        nome: 'Carlos Curso B',
        eid: 15,
        sid: const <int>[43],
      ).toRawJson();
      payloadController.add(outroCurso);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.notEnrolledCalls, 1);
      expect(find.text('Não inscrito neste curso!'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFFEF4444));
      expect(find.text('1/5'), findsOneWidget);

      // 4. QR Code com hash adulterado -> moldura vermelha
      const adulterado =
          '{"v":1,"pid":9999,"cpf":"12345678901","nome":"Fraude","eid":15,"sid":[42],"hash":"0ec02205"}';
      payloadController.add(adulterado);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(sensory.errorCalls, 1);
      expect(find.text('QR Code inválido'), findsOneWidget);
      expect(find.text('Assinatura hash divergente'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFFEF4444));

      // 5. Mais 5 leituras validas -> tela continua aberta e exibe ultimos 5
      for (var i = 1; i <= 5; i++) {
        final payload = ParticipanteQrPayload.v1(
          pid: 3000 + i,
          cpf: '52998224725',
          nome: 'Aluno $i',
          eid: 15,
          sid: const <int>[42],
        ).toRawJson();
        payloadController.add(payload);
        await tester.pump();
      }
      await tester.pump(const Duration(milliseconds: 150));

      expect(find.byType(ScannerScreen), findsOneWidget);
      expect(find.text('5/5'), findsOneWidget);
      expect(find.text('Aluno 5'), findsNWidgets(2)); // banner + historico
      expect(find.text('Aluno 1'), findsOneWidget);
      expect(find.text('Maria Santos'), findsNothing);

      // Limpa o timer visual de 2200ms antes do encerramento do teste
      await tester.pump(const Duration(seconds: 3));
    },
  );

  testWidgets('exibe SAÍDA REGISTRADA no modo de saida', (tester) async {
    final payloadController = StreamController<String>.broadcast();
    final sensory = _SensoryFeedbackSpy();
    addTearDown(payloadController.close);

    await tester.pumpWidget(
      MaterialApp(
        home: ScannerScreen(
          tipoAcao: TipoAcao.saida,
          credenciamento: _CredenciamentoScreenSpy(),
          dispositivoId: 'portaria-01',
          sensoryFeedback: sensory,
          payloadStream: payloadController.stream,
          cameraPreviewOverride: const ColoredBox(color: Colors.black87),
        ),
      ),
    );

    expect(find.text('Registrar SAÍDA'), findsOneWidget);

    const goldenV1 =
        '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
    payloadController.add(goldenV1);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 150));

    expect(find.text('SAÍDA REGISTRADA'), findsOneWidget);
    expect(find.text('SAÍDA'), findsOneWidget);
    expect(sensory.successCalls, 1);

    await tester.pump(const Duration(seconds: 3));
  });

  testWidgets(
    'nao realiza segunda chamada com o mesmo cracha parado no quadro por 6 segundos',
    (tester) async {
      var now = DateTime.utc(2026, 11, 19, 14, 0, 0);
      final captureController = StreamController<BarcodeCapture>.broadcast();
      final sensory = _SensoryFeedbackSpy();
      final credenciamento = _CredenciamentoScreenSpy();
      addTearDown(captureController.close);

      await tester.pumpWidget(
        MaterialApp(
          home: ScannerScreen(
            tipoAcao: TipoAcao.entrada,
            credenciamento: credenciamento,
            dispositivoId: 'portaria-01',
            contextoAtivo: const ScannerSessionContext(
              eventoId: 15,
              eventoNome: 'Seminfo 2026',
              subeventoId: 42,
              subeventoNome: 'Curso A',
            ),
            sensoryFeedback: sensory,
            barcodeCaptureStream: captureController.stream,
            cameraPreviewOverride: const ColoredBox(color: Colors.black87),
            clock: () => now,
          ),
        ),
      );

      const goldenV1 =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      final captureComCracha = BarcodeCapture(
        barcodes: const <Barcode>[Barcode(rawValue: goldenV1)],
      );

      // Mantem o mesmo cracha continuamente no quadro por 6,5 segundos (frames a cada 250ms).
      for (var ms = 0; ms <= 6500; ms += 250) {
        captureController.add(captureComCracha);
        await tester.pump();
        now = now.add(const Duration(milliseconds: 250));
      }

      expect(credenciamento.calls, 1);
      expect(sensory.successCalls, 1);
      expect(sensory.duplicateCalls, 0);

      // Retira o cracha do quadro (quadro vazio) e apresenta novamente -> nova leitura aceita.
      captureController.add(const BarcodeCapture());
      await tester.pump();
      captureController.add(captureComCracha);
      await tester.pump();

      expect(credenciamento.calls, 2);
      expect(sensory.successCalls, 2);

      await tester.pump(const Duration(seconds: 3));
    },
  );

  testWidgets(
    'exibe banner de processamento imediato quando API demora 420ms e confirma ao concluir',
    (tester) async {
      final payloadController = StreamController<String>.broadcast();
      final sensory = _SensoryFeedbackSpy();
      final pending = Completer<OperationResult<RegistroResponse>>();
      final credenciamento = _CredenciamentoScreenSpy()..pendingResult = pending;
      addTearDown(payloadController.close);

      await tester.pumpWidget(
        MaterialApp(
          home: ScannerScreen(
            tipoAcao: TipoAcao.entrada,
            credenciamento: credenciamento,
            dispositivoId: 'portaria-01',
            contextoAtivo: const ScannerSessionContext(
              eventoId: 15,
              eventoNome: 'Seminfo 2026',
              subeventoId: 42,
              subeventoNome: 'Curso A',
            ),
            sensoryFeedback: sensory,
            payloadStream: payloadController.stream,
            cameraPreviewOverride: const ColoredBox(color: Colors.black87),
          ),
        ),
      );

      const goldenV1 =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      payloadController.add(goldenV1);
      await tester.pump();

      // Antes de concluir a API: feedback imediato de leitura em processamento.
      expect(find.text('PROCESSANDO LEITURA'), findsOneWidget);
      expect(find.text('Maria Santos'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFF3B82F6));
      expect(sensory.successCalls, 0);

      // Simula API respondendo apos 420ms.
      await tester.pump(const Duration(milliseconds: 420));
      pending.complete(
        OperationResult<RegistroResponse>.enviado(
          RegistroResponse(
            id: 'reg-420ms',
            leituraId: '550e8400-e29b-41d4-a716-446655440000',
            participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
            participanteNome: 'Maria Santos',
            tipoAcao: TipoAcao.entrada,
            dataHora: DateTime.utc(2026, 11, 19, 14, 0),
            dataHoraLidaNoCelular: DateTime.utc(2026, 11, 19, 14, 0),
            dispositivoId: 'portaria-01',
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 150));

      expect(find.text('ENTRADA REGISTRADA'), findsOneWidget);
      expect(_frameBorderColor(tester), const Color(0xFF22C55E));
      expect(sensory.successCalls, 1);

      await tester.pump(const Duration(seconds: 3));
    },
  );
}

Color? _frameBorderColor(WidgetTester tester) {
  final container = tester.widget<AnimatedContainer>(
    find.byKey(const Key('scanner-frame')),
  );
  final decoration = container.decoration as BoxDecoration?;
  return decoration?.border?.top.color;
}

final class _SensoryFeedbackSpy implements ScannerSensoryFeedback {
  int successCalls = 0;
  int offlineCalls = 0;
  int notEnrolledCalls = 0;
  int duplicateCalls = 0;
  int errorCalls = 0;

  @override
  Future<void> playDuplicate() async {
    duplicateCalls++;
  }

  @override
  Future<void> playError() async {
    errorCalls++;
  }

  @override
  Future<void> playNotEnrolled() async {
    notEnrolledCalls++;
  }

  @override
  Future<void> playOffline() async {
    offlineCalls++;
  }

  @override
  Future<void> playSuccess() async {
    successCalls++;
  }
}

final class _CredenciamentoScreenSpy implements CredenciamentoGateway {
  int calls = 0;
  int? lastEventoId;
  int? lastSubeventoId;
  Completer<OperationResult<RegistroResponse>>? pendingResult;

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
  Future<List<ParticipanteResponse>> listarParticipantes() {
    throw UnimplementedError();
  }

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
    calls++;
    lastEventoId = eventoId;
    lastSubeventoId = subeventoId;
    final pending = pendingResult;
    if (pending != null) {
      return pending.future;
    }
    return OperationResult<RegistroResponse>.enviado(
      RegistroResponse(
        id: 'reg-${DateTime.now().microsecondsSinceEpoch}',
        leituraId: '550e8400-e29b-41d4-a716-446655440000',
        participanteId: participanteId,
        participanteNome: participanteNome,
        tipoAcao: tipoAcao,
        dataHora: DateTime.utc(2026, 11, 19, 14, 0),
        dataHoraLidaNoCelular: DateTime.utc(2026, 11, 19, 14, 0),
        dispositivoId: dispositivoId,
      ),
    );
  }

  @override
  Future<void> verificarConexao() async {}
}
