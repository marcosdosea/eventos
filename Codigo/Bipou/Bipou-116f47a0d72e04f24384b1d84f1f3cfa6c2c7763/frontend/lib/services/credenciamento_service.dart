import 'dart:async';

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
import 'package:uuid/uuid.dart';

typedef Clock = DateTime Function();
typedef UuidGenerator = String Function();

abstract interface class CredenciamentoGateway {
  String get apiBaseUrl;

  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
  });

  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  });

  Future<List<ParticipanteResponse>> listarParticipantes();

  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf);

  Future<void> verificarConexao();
}

abstract interface class CadastroPendenteSynchronizer {
  Future<CadastroSyncResult> sincronizarCadastrosPendentes();
}

final class CadastroSyncResult {
  const CadastroSyncResult({
    required this.enviados,
    required this.falhasPermanentes,
    required this.restantes,
  });

  final int enviados;
  final int falhasPermanentes;
  final int restantes;
}

final class CredenciamentoService
    implements CredenciamentoGateway, CadastroPendenteSynchronizer {
  factory CredenciamentoService({
    required AuditLogWriter auditLog,
    required CredenciamentoApi api,
    required CadastroPendenteStore cadastroPendenteStore,
    Clock? clock,
    UuidGenerator? uuidGenerator,
  }) {
    return CredenciamentoService._(
      auditLog: auditLog,
      api: api,
      cadastroPendenteStore: cadastroPendenteStore,
      clock: clock ?? DateTime.now,
      uuidGenerator: uuidGenerator ?? const Uuid().v4,
    );
  }

  CredenciamentoService._({
    required this._auditLog,
    required this._api,
    required this._cadastroPendenteStore,
    required this._clock,
    required this._uuidGenerator,
  });

  static const String cadastroManualAuditAction = 'CADASTRO_MANUAL';

  final AuditLogWriter _auditLog;
  final CredenciamentoApi _api;
  final CadastroPendenteStore _cadastroPendenteStore;
  final Clock _clock;
  final UuidGenerator _uuidGenerator;
  Future<CadastroSyncResult>? _sincronizacaoAtiva;

  @override
  String get apiBaseUrl => _api.baseUrl;

  @override
  Future<OperationResult<RegistroResponse>> registrarLeitura({
    required String participanteId,
    required String participanteNome,
    required TipoAcao tipoAcao,
    required String dispositivoId,
  }) async {
    final dataHora = _clock().toUtc();
    final request = LeituraQrCodeRequest(
      leituraId: _uuidGenerator(),
      participanteId: participanteId,
      tipoAcao: tipoAcao,
      dispositivoId: dispositivoId,
      dataHoraLidaNoCelular: dataHora,
    );

    await _auditLog.append(
      AuditLogEntry(
        dataHora: dataHora,
        participanteId: participanteId,
        participanteNome: participanteNome,
        tipoAcao: tipoAcao.apiValue,
        dispositivoId: dispositivoId,
      ),
    );

    try {
      final response = await _api.registrar(request);
      unawaited(sincronizarCadastrosPendentes());
      return OperationResult<RegistroResponse>.enviado(response);
    } on ApiUnavailableException {
      return const OperationResult<RegistroResponse>.salvoOffline();
    }
  }

  @override
  Future<OperationResult<ParticipanteResponse>> cadastrarParticipante({
    required ParticipanteRequest participante,
    required String dispositivoId,
  }) async {
    final dataHora = _clock().toUtc();
    final request = participante.cadastroId == null
        ? participante.withCadastroId(_uuidGenerator())
        : participante;

    await _auditLog.append(
      AuditLogEntry(
        dataHora: dataHora,
        participanteId: null,
        participanteNome: request.nome.trim(),
        tipoAcao: cadastroManualAuditAction,
        dispositivoId: dispositivoId,
      ),
    );

    final pendente = CadastroPendente(
      cadastroId: request.cadastroId!,
      nome: request.nome,
      cpf: request.cpf,
      dispositivoId: dispositivoId,
      criadoEm: dataHora,
    );
    await _cadastroPendenteStore.salvar(pendente);

    try {
      final response = await _api.cadastrarParticipante(request);
      await _removerCadastroConfirmado(request.cadastroId!);
      unawaited(sincronizarCadastrosPendentes());
      return OperationResult<ParticipanteResponse>.enviado(response);
    } on ApiUnavailableException {
      return const OperationResult<ParticipanteResponse>.salvoOffline();
    } on ApiRequestException {
      await _removerCadastroConfirmado(request.cadastroId!);
      rethrow;
    }
  }

  @override
  Future<List<ParticipanteResponse>> listarParticipantes() async {
    final participantes = await _api.listarParticipantes();
    unawaited(sincronizarCadastrosPendentes());
    return participantes;
  }

  @override
  Future<ParticipanteResponse> buscarParticipantePorCpf(String cpf) =>
      _api.buscarParticipantePorCpf(cpf);

  @override
  Future<void> verificarConexao() async {
    await _api.listarParticipantes();
    await sincronizarCadastrosPendentes();
  }

  @override
  Future<CadastroSyncResult> sincronizarCadastrosPendentes() async {
    final sincronizacaoExistente = _sincronizacaoAtiva;
    if (sincronizacaoExistente != null) {
      return sincronizacaoExistente;
    }

    final sincronizacao = _sincronizarAgora();
    _sincronizacaoAtiva = sincronizacao;
    try {
      return await sincronizacao;
    } finally {
      if (identical(_sincronizacaoAtiva, sincronizacao)) {
        _sincronizacaoAtiva = null;
      }
    }
  }

  Future<CadastroSyncResult> _sincronizarAgora() async {
    final pendentes = await _cadastroPendenteStore.listar();
    var enviados = 0;
    var falhasPermanentes = 0;

    for (final pendente in pendentes) {
      try {
        await _api.cadastrarParticipante(pendente.toRequest());
        await _removerCadastroConfirmado(pendente.cadastroId);
        enviados++;
      } on ApiUnavailableException {
        break;
      } on ApiRequestException {
        // Mantém o item para não perder silenciosamente um cadastro recusado.
        falhasPermanentes++;
      }
    }

    final restantes = (await _cadastroPendenteStore.listar()).length;
    return CadastroSyncResult(
      enviados: enviados,
      falhasPermanentes: falhasPermanentes,
      restantes: restantes,
    );
  }

  Future<void> _removerCadastroConfirmado(String cadastroId) async {
    try {
      await _cadastroPendenteStore.remover(cadastroId);
    } on CadastroPendenteStoreException {
      // O backend é idempotente: se a limpeza local falhar, o próximo reenvio
      // recupera a mesma resposta e tenta remover o item novamente.
    }
  }
}
