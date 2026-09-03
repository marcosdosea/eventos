import 'dart:io';

import 'package:bipou_frontend/models/audit_log_entry.dart';
import 'package:bipou_frontend/models/cadastro_pendente.dart';
import 'package:bipou_frontend/models/leitura_qr_code_request.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_request.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/models/registro_response.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/cadastro_pendente_store.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const uuid = '550e8400-e29b-41d4-a716-446655440000';
  const participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';
  final now = DateTime.utc(2026, 8, 22, 12, 30);

  test('grava a auditoria antes de enviar o registro', () async {
    final calls = <String>[];
    final audit = _AuditSpy(calls);
    final api = _ApiSpy(calls);
    final service = CredenciamentoService(
      auditLog: audit,
      api: api,
      cadastroPendenteStore: _CadastroPendenteStoreMemory(),
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    final result = await service.registrarLeitura(
      participanteId: participanteId,
      participanteNome: 'Participante Teste',
      tipoAcao: TipoAcao.entrada,
      dispositivoId: 'portaria-01',
    );

    expect(calls, <String>['audit', 'api.registrar']);
    expect(result.status, SyncStatus.enviado);
    expect(audit.entries.single.toLine(), contains(';ENTRADA;portaria-01'));
    expect(api.lastRegistro?.participanteId, participanteId);
    expect(api.lastRegistro?.leituraId, uuid);
  });

  test('retorna salvoOffline quando a API esta indisponivel', () async {
    final calls = <String>[];
    final service = CredenciamentoService(
      auditLog: _AuditSpy(calls),
      api: _ApiSpy(calls, unavailable: true),
      cadastroPendenteStore: _CadastroPendenteStoreMemory(),
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    final result = await service.registrarLeitura(
      participanteId: participanteId,
      participanteNome: 'Participante Teste',
      tipoAcao: TipoAcao.entrada,
      dispositivoId: 'portaria-01',
    );

    expect(calls, <String>['audit', 'api.registrar']);
    expect(result.status, SyncStatus.salvoOffline);
    expect(result.data, isNull);
  });

  test('nao chama a API quando a auditoria falha', () async {
    final calls = <String>[];
    final service = CredenciamentoService(
      auditLog: _AuditSpy(calls, shouldFail: true),
      api: _ApiSpy(calls),
      cadastroPendenteStore: _CadastroPendenteStoreMemory(),
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    await expectLater(
      service.registrarLeitura(
        participanteId: participanteId,
        participanteNome: 'Participante Teste',
        tipoAcao: TipoAcao.entrada,
        dispositivoId: 'portaria-01',
      ),
      throwsA(isA<AuditLogException>()),
    );
    expect(calls, <String>['audit']);
  });

  test(
    'expõe URL da API e testa conexão pelo endpoint de participantes',
    () async {
      final calls = <String>[];
      final api = _ApiSpy(calls);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: _CadastroPendenteStoreMemory(),
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      expect(service.apiBaseUrl, 'http://localhost:8080');

      await service.verificarConexao();

      expect(calls, <String>['api.listarParticipantes']);
    },
  );

  test(
    'mantem cadastro manual em fila quando a API esta indisponivel',
    () async {
      final calls = <String>[];
      final store = _CadastroPendenteStoreMemory();
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: _ApiSpy(calls, unavailable: true),
        cadastroPendenteStore: store,
        clock: () => now,
        uuidGenerator: () => uuid,
      );

      final result = await service.cadastrarParticipante(
        participante: ParticipanteRequest(
          nome: 'Participante Teste',
          cpf: '52998224725',
        ),
        dispositivoId: 'portaria-01',
      );

      expect(result.status, SyncStatus.salvoOffline);
      expect(calls, <String>['audit', 'api.cadastrarParticipante']);
      expect(store.items, hasLength(1));
      expect(store.items.single.cadastroId, uuid);
      expect(store.items.single.cpf, '52998224725');
    },
  );

  test(
    'sincroniza cadastro pendente com o mesmo id e remove da fila',
    () async {
      final store = _CadastroPendenteStoreMemory()
        ..items.add(
          CadastroPendente(
            cadastroId: uuid,
            nome: 'Participante Teste',
            cpf: '52998224725',
            dispositivoId: 'portaria-01',
            criadoEm: now,
          ),
        );
      final calls = <String>[];
      final api = _ApiSpy(calls);
      final service = CredenciamentoService(
        auditLog: _AuditSpy(calls),
        api: api,
        cadastroPendenteStore: store,
        clock: () => now,
        uuidGenerator: () => 'outro-id-nao-utilizado',
      );

      final result = await service.sincronizarCadastrosPendentes();

      expect(result.enviados, 1);
      expect(result.falhasPermanentes, 0);
      expect(result.restantes, 0);
      expect(store.items, isEmpty);
      expect(api.lastParticipante?.cadastroId, uuid);
    },
  );

  test('mantem na fila cadastro recusado para nao perder dados', () async {
    final store = _CadastroPendenteStoreMemory()
      ..items.add(
        CadastroPendente(
          cadastroId: uuid,
          nome: 'Participante Teste',
          cpf: '52998224725',
          dispositivoId: 'portaria-01',
          criadoEm: now,
        ),
      );
    final service = CredenciamentoService(
      auditLog: _AuditSpy(<String>[]),
      api: _ApiSpy(<String>[], rejectManual: true),
      cadastroPendenteStore: store,
      clock: () => now,
      uuidGenerator: () => uuid,
    );

    final result = await service.sincronizarCadastrosPendentes();

    expect(result.enviados, 0);
    expect(result.falhasPermanentes, 1);
    expect(result.restantes, 1);
    expect(store.items.single.cadastroId, uuid);
  });
}

final class _AuditSpy implements AuditLogWriter {
  _AuditSpy(this.calls, {this.shouldFail = false});

  final List<String> calls;
  final bool shouldFail;
  final List<AuditLogEntry> entries = <AuditLogEntry>[];

  @override
  Future<File> append(AuditLogEntry entry) async {
    calls.add('audit');
    if (shouldFail) {
      throw const AuditLogException('Falha simulada.');
    }
    entries.add(entry);
    return File('audit-spy.txt');
  }
}

final class _ApiSpy implements CredenciamentoApi {
  _ApiSpy(this.calls, {this.unavailable = false, this.rejectManual = false});

  final List<String> calls;
  final bool unavailable;
  final bool rejectManual;
  LeituraQrCodeRequest? lastRegistro;
  ParticipanteRequest? lastParticipante;

  @override
  String get baseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> cadastrarParticipante(
    ParticipanteRequest request,
  ) async {
    calls.add('api.cadastrarParticipante');
    lastParticipante = request;
    if (unavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    if (rejectManual) {
      throw const ApiRequestException('Cadastro recusado.', statusCode: 409);
    }
    return ParticipanteResponse(
      id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      nome: request.nome,
      cpf: request.cpf,
    );
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) async {
    calls.add('api.buscarParticipantePorCpf');
    if (unavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    return ParticipanteResponse(
      id: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      nome: 'Participante Teste',
      cpf: cpf,
    );
  }

  @override
  Future<RegistroResponse> registrar(LeituraQrCodeRequest request) async {
    calls.add('api.registrar');
    lastRegistro = request;
    if (unavailable) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    return RegistroResponse(
      id: 'registro-01',
      leituraId: request.leituraId,
      participanteId: 'participante-01',
      participanteNome: 'Participante',
      tipoAcao: request.tipoAcao,
      dataHora: request.dataHoraLidaNoCelular,
      dataHoraLidaNoCelular: request.dataHoraLidaNoCelular,
      dispositivoId: request.dispositivoId,
    );
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() {
    calls.add('api.listarParticipantes');
    return Future<List<ParticipanteResponse>>.value(
      const <ParticipanteResponse>[],
    );
  }
}

final class _CadastroPendenteStoreMemory implements CadastroPendenteStore {
  final List<CadastroPendente> items = <CadastroPendente>[];

  @override
  Future<List<CadastroPendente>> listar() async => List.of(items);

  @override
  Future<void> remover(String cadastroId) async {
    items.removeWhere((item) => item.cadastroId == cadastroId);
  }

  @override
  Future<void> salvar(CadastroPendente cadastro) async {
    final existente = items.any(
      (item) => item.cadastroId == cadastro.cadastroId,
    );
    if (!existente) {
      items.add(cadastro);
    }
  }
}
