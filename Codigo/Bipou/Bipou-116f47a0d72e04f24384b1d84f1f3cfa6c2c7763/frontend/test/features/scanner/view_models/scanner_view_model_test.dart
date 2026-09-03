import 'dart:async';

import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/features/scanner/view_models/scanner_view_model.dart';
import 'package:bipou_frontend/models/operation_result.dart';
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
  });

  test(
    'ignora o mesmo participante durante o cooldown de cinco segundos',
    () async {
      await viewModel.processarPayload(participantePayload);

      now = now.add(const Duration(seconds: 4));
      await viewModel.processarPayload(participantePayload);
      expect(credenciamento.calls, 1);

      now = now.add(const Duration(seconds: 1));
      await viewModel.processarPayload(participantePayload);
      expect(credenciamento.calls, 2);
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
  });

  test('emite Salvo Offline quando o envio nao alcanca a API', () async {
    credenciamento.offline = true;

    await viewModel.processarPayload(participantePayload);

    expect(feedbacks.single.type, ScannerFeedbackType.offline);
    expect(feedbacks.single.message, 'Salvo Offline: Participante Teste');
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
}

OperationResult<RegistroResponse> _successResult() {
  return OperationResult<RegistroResponse>.enviado(
    RegistroResponse(
      id: 'registro-01',
      leituraId: '550e8400-e29b-41d4-a716-446655440000',
      participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      participanteNome: 'Participante Teste',
      tipoAcao: TipoAcao.entrada,
      dataHora: DateTime.utc(2026, 8, 22, 12, 30),
      dataHoraLidaNoCelular: DateTime.utc(2026, 8, 22, 12, 30),
      dispositivoId: 'portaria-01',
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
    return _successResult();
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
