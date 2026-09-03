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

Future<void> main() async {
  await _verifyAppendOnly();
  await _verifyAuditBeforeApi();
  await _verifyOfflineResult();
  stdout.writeln('Etapa 2 verificada com sucesso.');
}

Future<void> _verifyAppendOnly() async {
  final directory = await Directory.systemTemp.createTemp('bipou_verify_');
  try {
    final service = AuditLogService(
      documentsDirectoryProvider: () async => directory,
    );

    await Future.wait(<Future<File>>[
      service.append(
        AuditLogEntry(
          dataHora: DateTime.utc(2026, 8, 22, 12, 30),
          participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
          participanteNome: 'Participante Teste',
          tipoAcao: 'ENTRADA',
          dispositivoId: 'portaria-01',
        ),
      ),
      service.append(
        AuditLogEntry(
          dataHora: DateTime.utc(2026, 8, 22, 12, 31),
          participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
          participanteNome: 'Participante Teste',
          tipoAcao: 'SAIDA',
          dispositivoId: 'portaria-01',
        ),
      ),
    ]);

    final file = File(
      '${directory.path}${Platform.pathSeparator}${AuditLogService.fileName}',
    );
    final lines = await file.readAsLines();
    _check(lines.length == 2, 'O log deveria conter duas linhas.');
    _check(lines.first.contains(';ENTRADA;'), 'A primeira linha foi perdida.');
    _check(lines.last.contains(';SAIDA;'), 'A segunda linha foi perdida.');
  } finally {
    await directory.delete(recursive: true);
  }
}

Future<void> _verifyAuditBeforeApi() async {
  final calls = <String>[];
  final api = _ApiSpy(calls);
  final service = _service(calls: calls, api: api);

  final result = await service.registrarLeitura(
    participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
    participanteNome: 'Participante Teste',
    tipoAcao: TipoAcao.entrada,
    dispositivoId: 'portaria-01',
  );

  _check(
    calls.join(',') == 'audit,api',
    'A auditoria deve ocorrer antes da API.',
  );
  _check(result.status == SyncStatus.enviado, 'O envio deveria ter sucesso.');
}

Future<void> _verifyOfflineResult() async {
  final calls = <String>[];
  final service = _service(calls: calls, api: _ApiSpy(calls, offline: true));

  final result = await service.registrarLeitura(
    participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
    participanteNome: 'Participante Teste',
    tipoAcao: TipoAcao.entrada,
    dispositivoId: 'portaria-01',
  );

  _check(
    result.status == SyncStatus.salvoOffline,
    'A indisponibilidade deveria retornar salvoOffline.',
  );
}

CredenciamentoService _service({
  required List<String> calls,
  required CredenciamentoApi api,
}) {
  return CredenciamentoService(
    auditLog: _AuditSpy(calls),
    api: api,
    cadastroPendenteStore: _CadastroPendenteStoreFake(),
    clock: () => DateTime.utc(2026, 8, 22, 12, 30),
    uuidGenerator: () => '550e8400-e29b-41d4-a716-446655440000',
  );
}

final class _CadastroPendenteStoreFake implements CadastroPendenteStore {
  final List<CadastroPendente> _cadastros = <CadastroPendente>[];

  @override
  Future<List<CadastroPendente>> listar() async =>
      List<CadastroPendente>.unmodifiable(_cadastros);

  @override
  Future<void> remover(String cadastroId) async {
    _cadastros.removeWhere((cadastro) => cadastro.cadastroId == cadastroId);
  }

  @override
  Future<void> salvar(CadastroPendente cadastro) async {
    _cadastros.add(cadastro);
  }
}

void _check(bool condition, String message) {
  if (!condition) {
    throw StateError(message);
  }
}

final class _AuditSpy implements AuditLogWriter {
  _AuditSpy(this.calls);

  final List<String> calls;

  @override
  Future<File> append(AuditLogEntry entry) async {
    calls.add('audit');
    return File('audit-spy.txt');
  }
}

final class _ApiSpy implements CredenciamentoApi {
  _ApiSpy(this.calls, {this.offline = false});

  final List<String> calls;
  final bool offline;

  @override
  String get baseUrl => 'http://localhost:8080';

  @override
  Future<ParticipanteResponse> cadastrarParticipante(
    ParticipanteRequest request,
  ) {
    throw UnimplementedError();
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) {
    throw UnimplementedError();
  }

  @override
  Future<RegistroResponse> registrar(LeituraQrCodeRequest request) async {
    calls.add('api');
    if (offline) {
      throw const ApiUnavailableException('Falha simulada.');
    }
    return RegistroResponse(
      id: 'registro-01',
      leituraId: request.leituraId,
      participanteId: request.participanteId,
      participanteNome: 'Participante',
      tipoAcao: request.tipoAcao,
      dataHora: request.dataHoraLidaNoCelular,
      dataHoraLidaNoCelular: request.dataHoraLidaNoCelular,
      dispositivoId: request.dispositivoId,
    );
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() {
    throw UnimplementedError();
  }
}
