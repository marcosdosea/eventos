import 'dart:async';

import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/features/scanner/view_models/scanner_view_model.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';
  const outroParticipanteId = '6ba7b811-9dad-41d1-80b4-00c04fd430c8';
  const participantePayload =
      '{"id":"$participanteId","nome":"Participante Teste"}';
  late DateTime now;
  late _CredenciamentoSpy credenciamento;
  late ScannerViewModel viewModel;
  late List<ScannerFeedback> feedbacks;
  late StreamSubscription<ScannerFeedback> subscription;

  setUp(() {
    now = DateTime.utc(2026, 8, 22, 12, 30);
    credenciamento = _CredenciamentoSpy();
    viewModel = ScannerViewModel(
      credenciamento: credenciamento,
      tipoAcao: TipoAcao.entrada,
      dispositivoId: 'portaria-01',
      clock: () => now,
    );
    feedbacks = <ScannerFeedback>[];
    subscription = viewModel.feedbacks.listen(feedbacks.add);
  });

  tearDown(() async {
    await subscription.cancel();
    viewModel.dispose();
  });

  test('extrai id e nome do JSON e registra a acao selecionada', () async {
    await viewModel.processarPayload(participantePayload);

    expect(credenciamento.calls, 1);
    expect(credenciamento.lastParticipanteId, participanteId);
    expect(credenciamento.lastParticipanteNome, 'Participante Teste');
    expect(credenciamento.lastTipoAcao, TipoAcao.entrada);
    expect(credenciamento.lastDispositivoId, 'portaria-01');
    expect(feedbacks.single.type, ScannerFeedbackType.sucesso);
    expect(feedbacks.single.message, contains('Participante Teste'));
    expect(feedbacks.single.statusLabel, 'ENTRADA REGISTRADA');
  });

  test(
    'processa payload unificado v1 gerado pelo Borala e credencia com sucesso',
    () async {
      const v1Payload =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';

      await viewModel.processarPayload(v1Payload);

      expect(credenciamento.calls, 1);
      expect(credenciamento.lastParticipanteNome, 'Maria Santos');
      expect(credenciamento.lastParticipanteId, isNotNull);
      expect(feedbacks.single.type, ScannerFeedbackType.sucesso);
      expect(feedbacks.single.message, contains('Maria Santos'));
      expect(viewModel.historicoRecente, hasLength(1));
      expect(viewModel.historicoRecente.single.participanteNome, 'Maria Santos');
    },
  );

  test('rejeita payload v1 com hash adulterado no scanner', () async {
    const v1Adulterado =
        '{"v":1,"pid":9999,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';

    await viewModel.processarPayload(v1Adulterado);

    expect(credenciamento.calls, 0);
    expect(feedbacks.single.type, ScannerFeedbackType.erro);
    expect(feedbacks.single.message, 'QR Code invalido');
    expect(feedbacks.single.detail, 'Assinatura hash divergente');
    expect(viewModel.historicoRecente, isEmpty);
  });

  test(
    'ignora o mesmo participante durante o cooldown de cinco segundos',
    () async {
      await viewModel.processarPayload(participantePayload);

      now = now.add(const Duration(seconds: 4));
      await viewModel.processarPayload(participantePayload);
      expect(credenciamento.calls, 1);
      expect(feedbacks.last.type, ScannerFeedbackType.duplicado);
      expect(feedbacks.last.message, 'Aguarde para bipar novamente');

      now = now.add(const Duration(seconds: 1));
      await viewModel.processarPayload(participantePayload);
      expect(credenciamento.calls, 2);
      expect(feedbacks.last.type, ScannerFeedbackType.sucesso);
    },
  );

  test(
    'nao duplica participante enquanto a requisicao esta em andamento',
    () async {
      final pendingResult = Completer<OperationResult<RegistroResponse>>();
      credenciamento.pendingResult = pendingResult;

      final firstRead = viewModel.processarPayload(participantePayload);
      await viewModel.processarPayload(participantePayload);

      expect(credenciamento.calls, 1);
      expect(viewModel.leiturasEmProcessamento, 1);

      pendingResult.complete(_successResult());
      await firstRead;
      expect(viewModel.processando, isFalse);
    },
  );

  test(
    'nao registra outro participante enquanto existe leitura em andamento',
    () async {
      final pendingResult = Completer<OperationResult<RegistroResponse>>();
      credenciamento.pendingResult = pendingResult;

      final firstRead = viewModel.processarPayload(participantePayload);
      await viewModel.processarPayload(
        '{"id":"$outroParticipanteId","nome":"Outro Participante"}',
      );

      expect(credenciamento.calls, 1);
      expect(credenciamento.lastParticipanteId, participanteId);

      pendingResult.complete(_successResult());
      await firstRead;
    },
  );

  test('informa erro e nao chama o servico para QR invalido', () async {
    await viewModel.processarPayload('{"id":"invalido","nome":"Teste"}');

    expect(credenciamento.calls, 0);
    expect(feedbacks.single.type, ScannerFeedbackType.erro);
    expect(feedbacks.single.message, 'QR Code invalido');
    expect(feedbacks.single.detail, 'QR Code corrompido ou inválido');
  });

  test('emite Salvo Offline quando o envio nao alcanca a API', () async {
    credenciamento.offline = true;

    await viewModel.processarPayload(participantePayload);

    expect(feedbacks.single.type, ScannerFeedbackType.offline);
    expect(feedbacks.single.message, 'Salvo Offline: Participante Teste');
    expect(viewModel.historicoRecente.single.offline, isTrue);
  });

  test('consulta participante pelo CPF e registra usando o UUID', () async {
    await viewModel.processarCpf('52998224725');

    expect(credenciamento.cpfConsultado, '52998224725');
    expect(credenciamento.calls, 1);
    expect(credenciamento.lastParticipanteId, participanteId);
    expect(credenciamento.lastParticipanteNome, 'Participante Teste');
    expect(feedbacks.single.type, ScannerFeedbackType.sucesso);
  });

  test('rejeita CPF invalido antes de consultar o backend', () async {
    await viewModel.processarCpf('12345678900');

    expect(credenciamento.cpfConsultado, isNull);
    expect(credenciamento.calls, 0);
    expect(feedbacks.single.message, 'CPF invalido');
  });

  test('informa quando o CPF nao pertence a um participante', () async {
    credenciamento.cpfNaoEncontrado = true;

    await viewModel.processarCpf('52998224725');

    expect(credenciamento.calls, 0);
    expect(feedbacks.single.type, ScannerFeedbackType.erro);
    expect(feedbacks.single.message, 'Participante nao encontrado');
  });

  group('Validacao de contexto, feedback e historico (#901)', () {
    test(
      'impede aluno matriculado no Curso A de registrar presenca no Curso B',
      () async {
        final cursoB = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-curso-b',
          contextoAtivo: const ScannerSessionContext(
            eventoId: 15,
            eventoNome: 'Seminfo 2026',
            subeventoId: 43,
            subeventoNome: 'Curso B - Segurança Web',
          ),
          clock: () => now,
        );
        final eventosCursoB = <ScannerFeedback>[];
        final sub = cursoB.feedbacks.listen(eventosCursoB.add);
        addTearDown(() async {
          await sub.cancel();
          cursoB.dispose();
        });

        final alunoCursoA = ParticipanteQrPayload.v1(
          pid: 2001,
          cpf: '52998224725',
          nome: 'Aluno Curso A',
          eid: 15,
          sid: const <int>[42],
        ).toRawJson();

        await cursoB.processarPayload(alunoCursoA);

        expect(credenciamento.calls, 0);
        expect(eventosCursoB.single.type, ScannerFeedbackType.naoInscrito);
        expect(eventosCursoB.single.message, 'Não inscrito neste curso!');
        expect(eventosCursoB.single.participantName, 'Aluno Curso A');
        expect(cursoB.historicoRecente, isEmpty);
      },
    );

    test('bloqueia participante pertencente a outro evento', () async {
      final portariaEvento15 = ScannerViewModel(
        credenciamento: credenciamento,
        tipoAcao: TipoAcao.entrada,
        dispositivoId: 'portaria-01',
        contextoAtivo: const ScannerSessionContext(
          eventoId: 15,
          eventoNome: 'Seminfo 2026',
        ),
        clock: () => now,
      );
      final eventos = <ScannerFeedback>[];
      final sub = portariaEvento15.feedbacks.listen(eventos.add);
      addTearDown(() async {
        await sub.cancel();
        portariaEvento15.dispose();
      });

      final outroEvento = ParticipanteQrPayload.v1(
        pid: 3001,
        cpf: '52998224725',
        nome: 'Visitante Evento 99',
        eid: 99,
        sid: const <int>[42],
      ).toRawJson();

      await portariaEvento15.processarPayload(outroEvento);

      expect(credenciamento.calls, 0);
      expect(eventos.single.type, ScannerFeedbackType.naoInscrito);
      expect(eventos.single.detail, 'Crachá pertence a outro evento');
      expect(portariaEvento15.historicoRecente, isEmpty);
    });

    test('emite SAÍDA REGISTRADA quando a acao selecionada for saida', () async {
      final saidaVm = ScannerViewModel(
        credenciamento: credenciamento,
        tipoAcao: TipoAcao.saida,
        dispositivoId: 'portaria-01',
        contextoAtivo: const ScannerSessionContext(
          eventoId: 15,
          eventoNome: 'Seminfo 2026',
          subeventoId: 42,
          subeventoNome: 'Curso A',
        ),
        clock: () => now,
      );
      final eventos = <ScannerFeedback>[];
      final sub = saidaVm.feedbacks.listen(eventos.add);
      addTearDown(() async {
        await sub.cancel();
        saidaVm.dispose();
      });

      const v1Payload =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      await saidaVm.processarPayload(v1Payload);

      expect(credenciamento.calls, 1);
      expect(credenciamento.lastTipoAcao, TipoAcao.saida);
      expect(eventos.single.type, ScannerFeedbackType.sucesso);
      expect(eventos.single.statusLabel, 'SAÍDA REGISTRADA');
      expect(saidaVm.historicoRecente.single.statusLabel, 'SAÍDA');
    });

    test('mantem estritamente os ultimos 5 participantes no historico', () async {
      for (var i = 1; i <= 6; i++) {
        final payload = ParticipanteQrPayload.v1(
          pid: 5000 + i,
          cpf: '52998224725',
          nome: 'Participante $i',
          eid: 15,
          sid: const <int>[42],
        ).toRawJson();
        await viewModel.processarPayload(payload);
      }

      expect(viewModel.historicoRecente, hasLength(5));
      expect(
        viewModel.historicoRecente.map((r) => r.participanteNome).toList(),
        <String>[
          'Participante 6',
          'Participante 5',
          'Participante 4',
          'Participante 3',
          'Participante 2',
        ],
      );
    });

    test(
      'processa validacao e emite feedback em menos de 300ms (criterio de aceite)',
      () async {
        final vmComContexto = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          contextoAtivo: const ScannerSessionContext(
            eventoId: 15,
            eventoNome: 'Seminfo 2026',
            subeventoId: 42,
            subeventoNome: 'Curso A',
          ),
          clock: () => now,
        );
        final eventos = <ScannerFeedback>[];
        final sub = vmComContexto.feedbacks.listen(eventos.add);
        addTearDown(() async {
          await sub.cancel();
          vmComContexto.dispose();
        });

        const v1Payload =
            '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';

        final sw = Stopwatch()..start();
        await vmComContexto.processarPayload(v1Payload);
        sw.stop();

        expect(sw.elapsedMilliseconds, lessThan(300));
        expect(eventos.single.processingDuration, isNotNull);
        expect(
          eventos.single.processingDuration!.inMilliseconds,
          lessThan(300),
        );
      },
    );
  });
}

OperationResult<RegistroResponse> _successResult({
  String participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
  String participanteNome = 'Participante Teste',
  TipoAcao tipoAcao = TipoAcao.entrada,
  String dispositivoId = 'portaria-01',
}) {
  return OperationResult<RegistroResponse>.enviado(
    RegistroResponse(
      id: 'registro-01',
      leituraId: '550e8400-e29b-41d4-a716-446655440000',
      participanteId: participanteId,
      participanteNome: participanteNome,
      tipoAcao: tipoAcao,
      dataHora: DateTime.utc(2026, 8, 22, 12, 30),
      dataHoraLidaNoCelular: DateTime.utc(2026, 8, 22, 12, 30),
      dispositivoId: dispositivoId,
    ),
  );
}

final class _CredenciamentoSpy implements CredenciamentoGateway {
  int calls = 0;
  bool offline = false;
  String? lastParticipanteId;
  String? lastParticipanteNome;
  TipoAcao? lastTipoAcao;
  String? lastDispositivoId;
  Completer<OperationResult<RegistroResponse>>? pendingResult;
  String? cpfConsultado;
  bool cpfNaoEncontrado = false;

  @override
  String get apiBaseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) async {
    cpfConsultado = cpf;
    if (cpfNaoEncontrado) {
      throw const ApiRequestException(
        'Participante nao encontrado',
        statusCode: 404,
      );
    }
    return const ParticipanteResponse(
      id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      nome: 'Participante Teste',
      cpf: '52998224725',
    );
  }

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
  }) async {
    calls++;
    lastParticipanteId = participanteId;
    lastParticipanteNome = participanteNome;
    lastTipoAcao = tipoAcao;
    lastDispositivoId = dispositivoId;

    final pending = pendingResult;
    if (pending != null) {
      return pending.future;
    }
    if (offline) {
      return const OperationResult<RegistroResponse>.salvoOffline();
    }
    return _successResult(
      participanteId: participanteId,
      participanteNome: participanteNome,
      tipoAcao: tipoAcao,
      dispositivoId: dispositivoId,
    );
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() {
    throw UnimplementedError();
  }

  @override
  Future<void> verificarConexao() {
    throw UnimplementedError();
  }
}
