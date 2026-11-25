import 'dart:async';

import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/features/scanner/view_models/scanner_view_model.dart';
import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

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

  test('usa TipoAcao.entrada como padrao quando tipoAcao nao e informado', () async {
    final vmPadrao = ScannerViewModel(
      credenciamento: credenciamento,
      dispositivoId: 'portaria-01',
      clock: () => now,
    );
    addTearDown(vmPadrao.dispose);

    await vmPadrao.processarPayload(participantePayload);

    expect(credenciamento.calls, 1);
    expect(credenciamento.lastTipoAcao, TipoAcao.entrada);
  });

  test('extrai id e nome do JSON e registra a acao selecionada', () async {
    await viewModel.processarPayload(participantePayload);

    expect(credenciamento.calls, 1);
    expect(credenciamento.lastParticipanteId, participanteId);
    expect(credenciamento.lastParticipanteNome, 'Participante Teste');
    expect(credenciamento.lastTipoAcao, TipoAcao.entrada);
    expect(credenciamento.lastDispositivoId, 'portaria-01');
    expect(feedbacks.first.type, ScannerFeedbackType.processando);
    expect(feedbacks.last.type, ScannerFeedbackType.sucesso);
    expect(feedbacks.last.message, contains('Participante Teste'));
    expect(feedbacks.last.statusLabel, 'ENTRADA REGISTRADA');
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
      expect(credenciamento.lastIdPessoa, 1042);
      expect(credenciamento.lastIdEvento, 15);
      expect(credenciamento.lastIdSubEvento, isNull);
      expect(credenciamento.lastEventoId, 15);
      expect(feedbacks.first.type, ScannerFeedbackType.processando);
      expect(feedbacks.last.type, ScannerFeedbackType.sucesso);
      expect(feedbacks.last.message, contains('Maria Santos'));
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
      expect(feedbacks.single.type, ScannerFeedbackType.processando);

      pendingResult.complete(_successResult());
      await firstRead;
      expect(viewModel.processando, isFalse);
      expect(feedbacks.last.type, ScannerFeedbackType.sucesso);
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

    expect(feedbacks.first.type, ScannerFeedbackType.processando);
    expect(feedbacks.last.type, ScannerFeedbackType.offline);
    expect(feedbacks.last.message, 'Salvo Offline: Participante Teste');
    expect(viewModel.historicoRecente.single.offline, isTrue);
  });

  test('consulta participante pelo CPF e registra usando o UUID', () async {
    await viewModel.processarCpf('52998224725');

    expect(credenciamento.cpfConsultado, '52998224725');
    expect(credenciamento.calls, 1);
    expect(credenciamento.lastParticipanteId, participanteId);
    expect(credenciamento.lastParticipanteNome, 'Participante Teste');
    expect(feedbacks.first.type, ScannerFeedbackType.processando);
    expect(feedbacks.last.type, ScannerFeedbackType.sucesso);
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
    expect(feedbacks.first.type, ScannerFeedbackType.processando);
    expect(feedbacks.last.type, ScannerFeedbackType.erro);
    expect(feedbacks.last.message, 'Participante nao encontrado');
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

    test('envia eventoId e subeventoId no registro conforme contexto ativo', () async {
      final portaria = ScannerViewModel(
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
      addTearDown(portaria.dispose);

      const v1Payload =
          '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
      await portaria.processarPayload(v1Payload);

      expect(credenciamento.calls, 1);
      expect(credenciamento.lastEventoId, 15);
      expect(credenciamento.lastSubeventoId, 42);
    });

    test(
      'aplica validacao de evento e curso ativo na entrada manual por CPF',
      () async {
        final cursoB = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-curso-b',
          contextoAtivo: const ScannerSessionContext(
            eventoId: 15,
            eventoNome: 'Seminfo 2026',
            subeventoId: 43,
            subeventoNome: 'Curso B',
          ),
          clock: () => now,
        );
        final eventos = <ScannerFeedback>[];
        final sub = cursoB.feedbacks.listen(eventos.add);
        addTearDown(() async {
          await sub.cancel();
          cursoB.dispose();
        });

        // 1. Participante encontrado na API, mas inscrito apenas no curso 42
        credenciamento.cpfResposta = const ParticipanteResponse(
          id: participanteId,
          nome: 'Aluno Curso A',
          cpf: '52998224725',
          idPessoaBorala: 1042,
          eventoId: 15,
          subeventoIds: <int>[42],
        );

        await cursoB.processarCpf('52998224725');
        expect(credenciamento.cpfEventoIdConsultado, 15);
        expect(credenciamento.calls, 0);
        expect(eventos.last.type, ScannerFeedbackType.naoInscrito);
        expect(eventos.last.message, 'Não inscrito neste curso!');

        // 2. Participante de outro evento
        credenciamento.cpfResposta = const ParticipanteResponse(
          id: outroParticipanteId,
          nome: 'Visitante Evento 99',
          cpf: '11144477735',
          idPessoaBorala: 3001,
          eventoId: 99,
          subeventoIds: <int>[43],
        );

        await cursoB.processarCpf('11144477735');
        expect(credenciamento.calls, 0);
        expect(eventos.last.type, ScannerFeedbackType.naoInscrito);
        expect(eventos.last.detail, 'Crachá pertence a outro evento');

        // 3. Participante inscrito no evento 15 e curso 43 -> registra enviando IDs
        credenciamento.cpfResposta = const ParticipanteResponse(
          id: participanteId,
          nome: 'Maria Curso B',
          cpf: '52998224725',
          idPessoaBorala: 1042,
          eventoId: 15,
          subeventoIds: <int>[42, 43],
        );

        await cursoB.processarCpf('52998224725');
        expect(credenciamento.calls, 1);
        expect(credenciamento.lastEventoId, 15);
        expect(credenciamento.lastSubeventoId, 43);
        expect(eventos.last.type, ScannerFeedbackType.sucesso);
      },
    );

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
      expect(credenciamento.lastEventoId, 15);
      expect(credenciamento.lastSubeventoId, 42);
      expect(eventos.first.type, ScannerFeedbackType.processando);
      expect(eventos.last.type, ScannerFeedbackType.sucesso);
      expect(eventos.last.statusLabel, 'SAÍDA REGISTRADA');
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
      'emite feedback imediato de processamento em menos de 300ms mesmo com API demorando 420ms',
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

        credenciamento.artificialDelay = const Duration(milliseconds: 420);

        const v1Payload =
            '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';

        final sw = Stopwatch()..start();
        final future = vmComContexto.processarPayload(v1Payload);

        // Antes da resposta da API (0-20ms), o feedback imediato ja foi emitido.
        expect(eventos, hasLength(1));
        expect(eventos.single.type, ScannerFeedbackType.processando);
        expect(eventos.single.statusLabel, 'PROCESSANDO LEITURA');
        expect(eventos.single.participantName, 'Maria Santos');
        expect(eventos.single.processingDuration, isNotNull);
        expect(
          eventos.single.processingDuration!.inMilliseconds,
          lessThan(300),
        );

        await future;
        sw.stop();

        expect(sw.elapsedMilliseconds, greaterThanOrEqualTo(400));
        expect(eventos, hasLength(2));
        expect(eventos.last.type, ScannerFeedbackType.sucesso);
        expect(eventos.last.statusLabel, 'ENTRADA REGISTRADA');
      },
    );
  });

  group('modo offline com cache SQLite e sessao de portaria ativa', () {
    late SqliteLocalParticipantRepository localRepo;

    setUp(() async {
      sqfliteFfiInit();
      localRepo = SqliteLocalParticipantRepository(
        databaseFactory: databaseFactoryFfi,
        databasePath: inMemoryDatabasePath,
      );
      await localRepo.replaceAllForEvent(
        eventoId: 15,
        participants: <LocalParticipant>[
          LocalParticipant.fromBorala(
            eventoId: 15,
            participante: BoralaParticipante(
              idPessoa: 1042,
              nome: 'Maria Santos da Silva',
              nomeCracha: 'Maria Santos',
              cpf: '52998224725',
              subeventosInscritos: const <int>[42],
            ),
          ),
          LocalParticipant.fromBorala(
            eventoId: 15,
            participante: BoralaParticipante(
              idPessoa: 1043,
              nome: 'Joao Somente Geral',
              cpf: '11144477735',
              subeventosInscritos: const <int>[],
            ),
          ),
        ],
      );
    });

    tearDown(() async {
      await localRepo.close();
    });

    test(
      'identifica participante pelo nome imediatamente via cache local em modo aviao (QR e CPF)',
      () async {
        credenciamento.offline = true;
        final sessao = PortariaSession(
          eventoId: 15,
          eventoNome: 'Seminfo 2026',
          subeventoId: 42,
          subeventoNome: 'Curso de Python',
          totalParticipantesCarregados: 1,
          sincronizadoEm: now,
        );
        final offlineVm = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          sessaoAtiva: sessao,
          localParticipants: localRepo,
          clock: () => now,
        );
        final offlineFeedbacks = <ScannerFeedback>[];
        final sub = offlineVm.feedbacks.listen(offlineFeedbacks.add);
        addTearDown(() async {
          await sub.cancel();
          offlineVm.dispose();
        });

        // Consulta por CPF 100% offline usando SQLite (sem tocar a API de busca).
        await offlineVm.processarCpf('52998224725');

        expect(credenciamento.cpfConsultado, isNull);
        expect(credenciamento.calls, 1);
        expect(credenciamento.lastEventoId, 15);
        expect(credenciamento.lastSubeventoId, 42);
        expect(credenciamento.lastParticipanteNome, 'Maria Santos da Silva');
        expect(offlineFeedbacks.first.type, ScannerFeedbackType.processando);
        expect(offlineFeedbacks.last.type, ScannerFeedbackType.offline);
        expect(
          offlineFeedbacks.last.message,
          'Salvo Offline: Maria Santos da Silva',
        );
      },
    );

    test(
      'bloqueia participante nao inscrito no subevento ativo usando cache local',
      () async {
        credenciamento.offline = true;
        final sessao = PortariaSession(
          eventoId: 15,
          eventoNome: 'Seminfo 2026',
          subeventoId: 42,
          subeventoNome: 'Curso de Python',
          totalParticipantesCarregados: 1,
          sincronizadoEm: now,
        );
        final offlineVm = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          sessaoAtiva: sessao,
          localParticipants: localRepo,
          clock: () => now,
        );
        final offlineFeedbacks = <ScannerFeedback>[];
        final sub = offlineVm.feedbacks.listen(offlineFeedbacks.add);
        addTearDown(() async {
          await sub.cancel();
          offlineVm.dispose();
        });

        await offlineVm.processarCpf('11144477735');

        expect(credenciamento.calls, 0);
        expect(offlineFeedbacks.first.type, ScannerFeedbackType.processando);
        expect(offlineFeedbacks.last.type, ScannerFeedbackType.naoInscrito);
        expect(
          offlineFeedbacks.last.message,
          'Não inscrito neste curso!',
        );
        expect(offlineFeedbacks.last.participantName, 'Joao Somente Geral');
      },
    );

    test(
      'bloqueia leitura concorrente enquanto consulta no cache local esta em andamento',
      () async {
        final pendingLookup = Completer<LocalParticipant?>();
        final slowLookup = _ControlledLocalLookup(pending: pendingLookup);
        final sessao = PortariaSession(
          eventoId: 15,
          eventoNome: 'Seminfo 2026',
          subeventoId: 42,
          subeventoNome: 'Curso de Python',
          totalParticipantesCarregados: 1,
          sincronizadoEm: now,
        );
        final vm = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          sessaoAtiva: sessao,
          localParticipants: slowLookup,
          clock: () => now,
        );
        addTearDown(vm.dispose);

        const qr1 =
            '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
        final qr2 = ParticipanteQrPayload.v1(
          pid: 2002,
          cpf: '11144477735',
          nome: 'Outro Participante',
          eid: 15,
          sid: const <int>[42],
        ).toRawJson();

        final primeira = vm.processarPayload(qr1);
        expect(vm.processando, isTrue);

        // Segunda leitura durante o lookup SQLite deve ser ignorada pelo lock.
        await vm.processarPayload(qr2);
        expect(slowLookup.lookups, 1);

        pendingLookup.complete(null);
        await primeira;

        expect(vm.processando, isFalse);
        expect(credenciamento.calls, 1);
        expect(credenciamento.lastParticipanteNome, 'Maria Santos');
      },
    );

    test(
      'continua validacao pelo payload do QR quando consulta ao cache local falha',
      () async {
        final failingLookup = _ControlledLocalLookup(throwError: true);
        final sessao = PortariaSession(
          eventoId: 15,
          eventoNome: 'Seminfo 2026',
          subeventoId: 42,
          subeventoNome: 'Curso de Python',
          totalParticipantesCarregados: 1,
          sincronizadoEm: now,
        );
        final vm = ScannerViewModel(
          credenciamento: credenciamento,
          tipoAcao: TipoAcao.entrada,
          dispositivoId: 'portaria-01',
          sessaoAtiva: sessao,
          localParticipants: failingLookup,
          clock: () => now,
        );
        final eventos = <ScannerFeedback>[];
        final sub = vm.feedbacks.listen(eventos.add);
        addTearDown(() async {
          await sub.cancel();
          vm.dispose();
        });

        const qrValido =
            '{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"0ec02205"}';
        await vm.processarPayload(qrValido);

        expect(vm.processando, isFalse);
        expect(credenciamento.calls, 1);
        expect(eventos.last.type, ScannerFeedbackType.sucesso);
      },
    );
  });
}

final class _ControlledLocalLookup implements LocalParticipantLookup {
  _ControlledLocalLookup({this.pending, this.throwError = false});

  final Completer<LocalParticipant?>? pending;
  final bool throwError;
  int lookups = 0;

  Future<LocalParticipant?> _resolve() {
    lookups++;
    if (throwError) {
      return Future<LocalParticipant?>.error(StateError('SQLite indisponivel'));
    }
    if (pending != null) {
      return pending!.future;
    }
    return Future<LocalParticipant?>.value(null);
  }

  @override
  Future<int> countByContext({required int eventoId, int? subeventoId}) async =>
      0;

  @override
  Future<LocalParticipant?> findByCpf(String cpf, {required int eventoId}) =>
      _resolve();

  @override
  Future<LocalParticipant?> findById(String id, {int? eventoId}) => _resolve();

  @override
  Future<LocalParticipant?> findByIdPessoa(
    int idPessoa, {
    required int eventoId,
  }) => _resolve();

  @override
  Future<List<LocalParticipant>> listByContext({
    required int eventoId,
    int? subeventoId,
  }) async => const <LocalParticipant>[];
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
  int? lastIdPessoa;
  int? lastIdEvento;
  int? lastIdSubEvento;
  int? lastEventoId;
  int? lastSubeventoId;
  Completer<OperationResult<RegistroResponse>>? pendingResult;
  Duration? artificialDelay;
  String? cpfConsultado;
  int? cpfEventoIdConsultado;
  bool cpfNaoEncontrado = false;
  ParticipanteResponse? cpfResposta;

  @override
  String get apiBaseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(
    String cpf, {
    int? eventoId,
  }) async {
    cpfConsultado = cpf;
    cpfEventoIdConsultado = eventoId;
    if (cpfNaoEncontrado) {
      throw const ApiRequestException(
        'Participante nao encontrado',
        statusCode: 404,
      );
    }
    return cpfResposta ??
        const ParticipanteResponse(
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
    int? idPessoa,
    int? idEvento,
    int? idSubEvento,
    int? eventoId,
    int? subeventoId,
    String? operadorUsuario,
  }) async {
    calls++;
    lastParticipanteId = participanteId;
    lastParticipanteNome = participanteNome;
    lastTipoAcao = tipoAcao;
    lastDispositivoId = dispositivoId;
    lastIdPessoa = idPessoa;
    lastIdEvento = idEvento;
    lastIdSubEvento = idSubEvento;
    lastEventoId = eventoId;
    lastSubeventoId = subeventoId;

    final delay = artificialDelay;
    if (delay != null) {
      await Future<void>.delayed(delay);
    }

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
