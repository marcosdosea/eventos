import 'dart:async';

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/features/scanner/models/recent_scan_record.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_context.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/frequencia_pendente_store_contract.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:flutter/foundation.dart';

typedef ScannerClock = DateTime Function();

final class ScannerViewModel extends ChangeNotifier {
  ScannerViewModel({
    required this._credenciamento,
    required this.dispositivoId,
    this.tipoAcao = TipoAcao.entrada,
    this.contextoAtivo,
    this.sessaoAtiva,
    this.localParticipants,
    this.cooldown = const Duration(seconds: 5),
    ScannerClock? clock,
  }) : _clock = clock ?? DateTime.now;

  static const int maxHistoricoRecente = 5;

  final CredenciamentoGateway _credenciamento;
  final ScannerClock _clock;
  final TipoAcao tipoAcao;
  final String dispositivoId;
  final ScannerSessionContext? contextoAtivo;
  final PortariaSession? sessaoAtiva;
  final LocalParticipantLookup? localParticipants;
  final Duration cooldown;

  final StreamController<ScannerFeedback> _feedbackController =
      StreamController<ScannerFeedback>.broadcast(sync: true);
  final Map<String, DateTime> _ultimaLeituraPorParticipante =
      <String, DateTime>{};
  final Set<String> _participantesEmProcessamento = <String>{};
  final List<RecentScanRecord> _historicoRecente = <RecentScanRecord>[];

  bool _disposed = false;
  bool _leituraEmAndamento = false;

  Stream<ScannerFeedback> get feedbacks => _feedbackController.stream;
  int get leiturasEmProcessamento => _participantesEmProcessamento.length;
  bool get processando =>
      _leituraEmAndamento || _participantesEmProcessamento.isNotEmpty;
  List<RecentScanRecord> get historicoRecente =>
      List<RecentScanRecord>.unmodifiable(_historicoRecente);

  int? get _eventoIdAtivo => contextoAtivo?.eventoId ?? sessaoAtiva?.eventoId;

  int? get _subeventoIdAtivo =>
      contextoAtivo != null ? contextoAtivo!.subeventoId : sessaoAtiva?.subeventoId;

  bool get _isSubeventoAtivo => _subeventoIdAtivo != null;

  String get _destinoAtivoLabel {
    if (contextoAtivo != null) {
      return contextoAtivo!.destinoLabel;
    }
    if (sessaoAtiva != null) {
      return sessaoAtiva!.destinoLabel;
    }
    return 'Evento Geral';
  }

  Future<void> processarPayload(String? rawPayload) async {
    if (_leituraEmAndamento) {
      return;
    }

    final stopwatch = Stopwatch()..start();
    final extracao = _extrairParticipanteDetalhado(rawPayload);
    final participante = extracao.participante;
    if (participante == null) {
      _emitir(
        ScannerFeedback(
          message: 'QR Code invalido',
          type: ScannerFeedbackType.erro,
          statusLabel: 'QR CODE INVÁLIDO',
          detail: extracao.errorDetail,
          processingDuration: stopwatch.elapsed,
        ),
      );
      return;
    }

    await _validarEProcessarParticipante(participante, stopwatch: stopwatch);
  }

  Future<void> processarCpf(String cpfInformado) async {
    if (_leituraEmAndamento) {
      return;
    }

    final stopwatch = Stopwatch()..start();
    final cpf = cpfInformado.trim();
    if (!CpfValidator.isValid(cpf)) {
      _emitir(
        ScannerFeedback(
          message: 'CPF invalido',
          type: ScannerFeedbackType.erro,
          statusLabel: 'CPF INVÁLIDO',
          processingDuration: stopwatch.elapsed,
        ),
      );
      return;
    }

    final chaveConsulta = 'cpf:$cpf';
    _leituraEmAndamento = true;
    _participantesEmProcessamento.add(chaveConsulta);
    _notificarMudanca();

    _emitir(
      ScannerFeedback(
        message: 'Consultando CPF...',
        type: ScannerFeedbackType.processando,
        statusLabel: 'PROCESSANDO LEITURA',
        detail: 'Validando inscrição do participante...',
        processingDuration: stopwatch.elapsed,
      ),
    );

    ParticipanteQrPayload? participante;
    LocalParticipant? localEncontrado;
    try {
      final eventoAtivo = _eventoIdAtivo;
      final lookup = localParticipants;
      if (eventoAtivo != null && lookup != null) {
        final local = await lookup.findByCpf(cpf, eventoId: eventoAtivo);
        if (local != null) {
          localEncontrado = local;
          participante = local.toQrPayload();
        }
      }

      if (participante == null) {
        final encontrado = await _credenciamento.buscarParticipantePorCpf(
          cpf,
          eventoId: eventoAtivo,
        );
        participante = ParticipanteQrPayload(
          id: encontrado.id,
          nome: encontrado.nome,
          pid: encontrado.idPessoaBorala,
          cpf: encontrado.cpf,
          eid: encontrado.eventoId,
          sid: encontrado.subeventoIds,
        );
      }
    } on ApiUnavailableException {
      _emitir(
        ScannerFeedback(
          message: localParticipants != null
              ? 'Participante nao encontrado no cache local e API indisponivel'
              : 'API indisponivel para consultar o CPF',
          type: ScannerFeedbackType.erro,
          statusLabel: 'API INDISPONÍVEL',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(
          message: error.message,
          type: ScannerFeedbackType.erro,
          statusLabel: 'CPF NÃO ENCONTRADO',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } on Object {
      _emitir(
        ScannerFeedback(
          message: 'Nao foi possivel consultar o CPF',
          type: ScannerFeedbackType.erro,
          statusLabel: 'ERRO NA CONSULTA',
          processingDuration: stopwatch.elapsed,
        ),
      );
    } finally {
      _participantesEmProcessamento.remove(chaveConsulta);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }

    if (participante != null) {
      await _validarEProcessarParticipante(
        participante,
        stopwatch: stopwatch,
        localPrecarregado: localEncontrado,
        consultarCacheLocal: false,
      );
    }
  }

  Future<LocalParticipant?> _buscarNoCacheLocal(
    ParticipanteQrPayload payload,
  ) async {
    final lookup = localParticipants;
    if (lookup == null) {
      return null;
    }

    final eventoId = _eventoIdAtivo ?? payload.eid;
    if (eventoId != null && payload.pid != null) {
      final porPessoa = await lookup.findByIdPessoa(
        payload.pid!,
        eventoId: eventoId,
      );
      if (porPessoa != null) {
        return porPessoa;
      }
    }

    if (eventoId != null &&
        payload.cpf != null &&
        payload.cpf != '00000000000') {
      final porCpf = await lookup.findByCpf(payload.cpf!, eventoId: eventoId);
      if (porCpf != null) {
        return porCpf;
      }
    }

    return lookup.findById(payload.id, eventoId: eventoId);
  }

  void reportarErroDaCamera() {
    _emitir(
      const ScannerFeedback(
        message: 'Falha na camera',
        type: ScannerFeedbackType.erro,
        statusLabel: 'FALHA NA CÂMERA',
      ),
    );
  }

  Future<void> _validarEProcessarParticipante(
    ParticipanteQrPayload participante, {
    required Stopwatch stopwatch,
    LocalParticipant? localPrecarregado,
    bool consultarCacheLocal = true,
  }) async {
    if (_leituraEmAndamento) {
      return;
    }

    _leituraEmAndamento = true;
    String? participanteIdEmProcessamento;
    var registroEfetivado = false;

    try {
      var local = localPrecarregado;
      if (local == null && consultarCacheLocal && localParticipants != null) {
        try {
          local = await _buscarNoCacheLocal(participante);
        } on Object {
          // Falha pontual no cache local nao deve impedir a validacao pelo payload do QR.
          local = null;
        }
      }

      final nomeIdentificado = local?.nome ?? participante.nome;
      final participanteId = local?.id ?? participante.id;
      final eventoParticipante = participante.eid ?? local?.eventoId;
      final subeventosParticipante =
          local != null ? local.subeventoIds : participante.sid;

      final eventoAtivo = _eventoIdAtivo;
      if (eventoAtivo != null &&
          eventoParticipante != null &&
          eventoParticipante != eventoAtivo) {
        _emitir(
          ScannerFeedback(
            message: _isSubeventoAtivo
                ? 'Não inscrito neste curso!'
                : 'Não inscrito neste evento!',
            type: ScannerFeedbackType.naoInscrito,
            participantName: nomeIdentificado,
            statusLabel: 'ACESSO NEGADO',
            detail: 'Crachá pertence a outro evento',
            processingDuration: stopwatch.elapsed,
          ),
        );
        return;
      }

      final subeventoAtivo = _subeventoIdAtivo;
      if (subeventoAtivo != null &&
          !subeventosParticipante.contains(subeventoAtivo)) {
        _emitir(
          ScannerFeedback(
            message: 'Não inscrito neste curso!',
            type: ScannerFeedbackType.naoInscrito,
            participantName: nomeIdentificado,
            statusLabel: 'ACESSO NEGADO',
            detail: 'Curso ativo: $_destinoAtivoLabel',
            processingDuration: stopwatch.elapsed,
          ),
        );
        return;
      }

      final agora = _clock();
      _removerLeiturasExpiradas(agora);

      if (_participantesEmProcessamento.contains(participanteId)) {
        return;
      }

      final ultimaLeitura = _ultimaLeituraPorParticipante[participanteId];
      if (ultimaLeitura != null && agora.difference(ultimaLeitura) < cooldown) {
        _emitir(
          ScannerFeedback(
            message: 'Aguarde para bipar novamente',
            type: ScannerFeedbackType.duplicado,
            participantName: nomeIdentificado,
            statusLabel: 'LEITURA DUPLICADA',
            detail:
                'Crachá lido recentemente (aguarde ${cooldown.inSeconds}s)',
            processingDuration: stopwatch.elapsed,
          ),
        );
        return;
      }

      _ultimaLeituraPorParticipante[participanteId] = agora;
      _participantesEmProcessamento.add(participanteId);
      participanteIdEmProcessamento = participanteId;
      _notificarMudanca();

      _emitir(
        ScannerFeedback(
          message: 'Processando leitura: $nomeIdentificado',
          type: ScannerFeedbackType.processando,
          participantName: nomeIdentificado,
          statusLabel: 'PROCESSANDO LEITURA',
          detail: 'Aguardando confirmação...',
          processingDuration: stopwatch.elapsed,
        ),
      );

      try {
        final eventoResolvido = eventoAtivo ?? eventoParticipante;
        final result = await _credenciamento.registrarLeitura(
          participanteId: participanteId,
          participanteNome: nomeIdentificado,
          tipoAcao: tipoAcao,
          dispositivoId: dispositivoId,
          idPessoa: local?.idPessoa ?? participante.pid,
          idEvento: eventoResolvido,
          idSubEvento: subeventoAtivo,
          eventoId: eventoResolvido,
          subeventoId: subeventoAtivo,
          operadorUsuario: sessaoAtiva?.operadorUsuario,
          ultimaAcaoLista: local?.ultimaAcao,
        );

        final statusAcao = _statusAcaoRegistrada(tipoAcao);

        if (result.status == SyncStatus.salvoOffline) {
          registroEfetivado = true;
          _adicionarAoHistorico(
            RecentScanRecord(
              participanteId: participanteId,
              participanteNome: nomeIdentificado,
              tipoAcao: tipoAcao,
              registradoEm: agora,
              offline: true,
            ),
          );
          _emitir(
            ScannerFeedback(
              message: 'Salvo Offline: $nomeIdentificado',
              type: ScannerFeedbackType.offline,
              participantName: nomeIdentificado,
              statusLabel: statusAcao,
              detail: 'Salvo offline na auditoria local',
              processingDuration: stopwatch.elapsed,
            ),
          );
          return;
        }

        final nomeRetornado = result.data?.participanteNome.trim();
        final nomeConfirmado =
            (nomeRetornado != null && nomeRetornado.isNotEmpty)
            ? nomeRetornado
            : nomeIdentificado;
        registroEfetivado = true;
        _adicionarAoHistorico(
          RecentScanRecord(
            participanteId: participanteId,
            participanteNome: nomeConfirmado,
            tipoAcao: tipoAcao,
            registradoEm: agora,
            offline: false,
          ),
        );
        _emitir(
          ScannerFeedback(
            message: '$statusAcao: $nomeConfirmado',
            type: ScannerFeedbackType.sucesso,
            participantName: nomeConfirmado,
            statusLabel: statusAcao,
            processingDuration: stopwatch.elapsed,
          ),
        );
      } on ApiRequestException catch (error) {
        _emitir(
          ScannerFeedback(
            message: error.message,
            type: ScannerFeedbackType.erro,
            participantName: nomeIdentificado,
            statusLabel: 'ERRO NO REGISTRO',
            processingDuration: stopwatch.elapsed,
          ),
        );
      } on AuditLogException {
        _emitir(
          ScannerFeedback(
            message: 'Falha ao salvar a auditoria local',
            type: ScannerFeedbackType.erro,
            participantName: nomeIdentificado,
            statusLabel: 'ERRO DE AUDITORIA',
            processingDuration: stopwatch.elapsed,
          ),
        );
      } on FrequenciaPendenteStoreException {
        _emitir(
          ScannerFeedback(
            message: 'Falha ao salvar a leitura na fila local',
            type: ScannerFeedbackType.erro,
            participantName: nomeIdentificado,
            statusLabel: 'ERRO NA FILA LOCAL',
            processingDuration: stopwatch.elapsed,
          ),
        );
      } on ArgumentError {
        _emitir(
          ScannerFeedback(
            message: 'Dados da leitura invalidos',
            type: ScannerFeedbackType.erro,
            participantName: nomeIdentificado,
            statusLabel: 'DADOS INVÁLIDOS',
            processingDuration: stopwatch.elapsed,
          ),
        );
      } on Object {
        _emitir(
          ScannerFeedback(
            message: 'Nao foi possivel processar a leitura',
            type: ScannerFeedbackType.erro,
            participantName: nomeIdentificado,
            statusLabel: 'ERRO NO REGISTRO',
            processingDuration: stopwatch.elapsed,
          ),
        );
      }
    } finally {
      if (participanteIdEmProcessamento != null) {
        if (!registroEfetivado) {
          _ultimaLeituraPorParticipante.remove(participanteIdEmProcessamento);
        }
        _participantesEmProcessamento.remove(participanteIdEmProcessamento);
      }
      _leituraEmAndamento = false;
      _notificarMudanca();
    }
  }

  ({ParticipanteQrPayload? participante, String errorDetail})
  _extrairParticipanteDetalhado(String? payload) {
    try {
      final parsed = ParticipanteQrPayload.fromRawJson(
        payload,
        secretKey: contextoAtivo?.secretKey,
      );
      return (participante: parsed, errorDetail: '');
    } on FormatException catch (error) {
      final msg = error.message.toLowerCase();
      final isHashError = msg.contains('hash') || msg.contains('assinatura');
      return (
        participante: null,
        errorDetail: isHashError
            ? 'Assinatura hash divergente'
            : 'QR Code corrompido ou inválido',
      );
    }
  }

  void _adicionarAoHistorico(RecentScanRecord record) {
    _historicoRecente.insert(0, record);
    if (_historicoRecente.length > maxHistoricoRecente) {
      _historicoRecente.removeRange(
        maxHistoricoRecente,
        _historicoRecente.length,
      );
    }
  }

  String _statusAcaoRegistrada(TipoAcao acao) {
    return switch (acao) {
      TipoAcao.entrada => 'ENTRADA REGISTRADA',
      TipoAcao.saida => 'SAÍDA REGISTRADA',
      TipoAcao.auto => 'FREQUÊNCIA REGISTRADA',
    };
  }

  void _removerLeiturasExpiradas(DateTime agora) {
    _ultimaLeituraPorParticipante.removeWhere(
      (_, leitura) => agora.difference(leitura) >= cooldown,
    );
  }

  void _emitir(ScannerFeedback feedback) {
    if (!_disposed) {
      _feedbackController.add(feedback);
    }
  }

  void _notificarMudanca() {
    if (!_disposed) {
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _disposed = true;
    unawaited(_feedbackController.close());
    super.dispose();
  }
}
