import 'dart:async';

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/features/scanner/models/scanner_feedback.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:flutter/foundation.dart';

typedef ScannerClock = DateTime Function();

final class ScannerViewModel extends ChangeNotifier {
  ScannerViewModel({
    required this._credenciamento,
    required this.tipoAcao,
    required this.dispositivoId,
    this.sessaoAtiva,
    this.localParticipants,
    this.cooldown = const Duration(seconds: 5),
    ScannerClock? clock,
  }) : _clock = clock ?? DateTime.now;

  final CredenciamentoGateway _credenciamento;
  final ScannerClock _clock;
  final TipoAcao tipoAcao;
  final String dispositivoId;
  final PortariaSession? sessaoAtiva;
  final LocalParticipantLookup? localParticipants;
  final Duration cooldown;

  final StreamController<ScannerFeedback> _feedbackController =
      StreamController<ScannerFeedback>.broadcast(sync: true);
  final Map<String, DateTime> _ultimaLeituraPorParticipante =
      <String, DateTime>{};
  final Set<String> _participantesEmProcessamento = <String>{};

  bool _disposed = false;
  bool _leituraEmAndamento = false;

  Stream<ScannerFeedback> get feedbacks => _feedbackController.stream;
  int get leiturasEmProcessamento => _participantesEmProcessamento.length;
  bool get processando => _participantesEmProcessamento.isNotEmpty;

  Future<void> processarPayload(String? payload) async {
    if (_leituraEmAndamento) {
      return;
    }

    final participante = _extrairParticipante(payload);
    if (participante == null) {
      _emitir(
        const ScannerFeedback(
          message: 'QR Code invalido',
          type: ScannerFeedbackType.erro,
        ),
      );
      return;
    }

    final agora = _clock();
    _removerLeiturasExpiradas(agora);

    final ultimaLeitura = _ultimaLeituraPorParticipante[participante.id];
    if (_participantesEmProcessamento.contains(participante.id) ||
        (ultimaLeitura != null && agora.difference(ultimaLeitura) < cooldown)) {
      return;
    }

    _participantesEmProcessamento.add(participante.id);
    _leituraEmAndamento = true;
    _notificarMudanca();

    try {
      final sessao = sessaoAtiva;
      if (sessao != null &&
          participante.eid != null &&
          participante.eid != sessao.eventoId) {
        _emitir(
          const ScannerFeedback(
            message: 'Participante pertence a outro evento',
            type: ScannerFeedbackType.erro,
          ),
        );
        return;
      }

      final local = await _buscarNoCacheLocal(participante, sessao);
      final nomeIdentificado = local?.nome ?? participante.nome;
      final participanteId = local?.id ?? participante.id;

      if (sessao != null && sessao.subeventoId != null) {
        final subId = sessao.subeventoId!;
        final inscrito = local != null
            ? local.inscritoNoSubevento(subId)
            : participante.sid.contains(subId);
        if (!inscrito) {
          _emitir(
            ScannerFeedback(
              message: '$nomeIdentificado nao esta inscrito neste subevento',
              type: ScannerFeedbackType.erro,
            ),
          );
          return;
        }
      }

      _ultimaLeituraPorParticipante[participante.id] = agora;

      final result = await _credenciamento.registrarLeitura(
        participanteId: participanteId,
        participanteNome: nomeIdentificado,
        tipoAcao: tipoAcao,
        dispositivoId: dispositivoId,
        eventoId: sessao?.eventoId,
        subeventoId: sessao?.subeventoId,
        operadorUsuario: sessao?.operadorUsuario,
      );

      if (result.status == SyncStatus.salvoOffline) {
        _emitir(
          ScannerFeedback(
            message: 'Salvo Offline: $nomeIdentificado',
            type: ScannerFeedbackType.offline,
          ),
        );
        return;
      }

      final nomeConfirmado = result.data?.participanteNome ?? nomeIdentificado;
      _emitir(
        ScannerFeedback(
          message: 'Registro realizado: $nomeConfirmado',
          type: ScannerFeedbackType.sucesso,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(message: error.message, type: ScannerFeedbackType.erro),
      );
    } on AuditLogException {
      _emitir(
        const ScannerFeedback(
          message: 'Falha ao salvar a auditoria local',
          type: ScannerFeedbackType.erro,
        ),
      );
    } on ArgumentError {
      _emitir(
        const ScannerFeedback(
          message: 'Dados da leitura invalidos',
          type: ScannerFeedbackType.erro,
        ),
      );
    } on Object {
      _emitir(
        const ScannerFeedback(
          message: 'Nao foi possivel processar a leitura',
          type: ScannerFeedbackType.erro,
        ),
      );
    } finally {
      _participantesEmProcessamento.remove(participante.id);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }
  }

  Future<void> processarCpf(String cpfInformado) async {
    if (_leituraEmAndamento) {
      return;
    }

    final cpf = cpfInformado.trim();
    if (!CpfValidator.isValid(cpf)) {
      _emitir(
        const ScannerFeedback(
          message: 'CPF invalido',
          type: ScannerFeedbackType.erro,
        ),
      );
      return;
    }

    final chaveConsulta = 'cpf:$cpf';
    _leituraEmAndamento = true;
    _participantesEmProcessamento.add(chaveConsulta);
    _notificarMudanca();

    ParticipanteQrPayload? participante;
    try {
      final sessao = sessaoAtiva;
      final lookup = localParticipants;
      if (sessao != null && lookup != null) {
        final local = await lookup.findByCpf(cpf, eventoId: sessao.eventoId);
        if (local != null) {
          participante = local.toQrPayload();
        }
      }

      if (participante == null) {
        final encontrado = await _credenciamento.buscarParticipantePorCpf(cpf);
        participante = ParticipanteQrPayload(
          id: encontrado.id,
          nome: encontrado.nome,
        );
      }
    } on ApiUnavailableException {
      _emitir(
        const ScannerFeedback(
          message: 'Participante nao encontrado no cache local e API indisponivel',
          type: ScannerFeedbackType.erro,
        ),
      );
    } on ApiRequestException catch (error) {
      _emitir(
        ScannerFeedback(message: error.message, type: ScannerFeedbackType.erro),
      );
    } on Object {
      _emitir(
        const ScannerFeedback(
          message: 'Nao foi possivel consultar o CPF',
          type: ScannerFeedbackType.erro,
        ),
      );
    } finally {
      _participantesEmProcessamento.remove(chaveConsulta);
      _leituraEmAndamento = false;
      _notificarMudanca();
    }

    if (participante != null) {
      await processarPayload(participante.toRawJson());
    }
  }

  Future<LocalParticipant?> _buscarNoCacheLocal(
    ParticipanteQrPayload payload,
    PortariaSession? sessao,
  ) async {
    final lookup = localParticipants;
    if (lookup == null) {
      return null;
    }

    final eventoId = sessao?.eventoId ?? payload.eid;
    if (eventoId != null && payload.pid != null) {
      final porPessoa = await lookup.findByIdPessoa(
        payload.pid!,
        eventoId: eventoId,
      );
      if (porPessoa != null) {
        return porPessoa;
      }
    }

    if (eventoId != null && payload.cpf != null && payload.cpf != '00000000000') {
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
      ),
    );
  }

  ParticipanteQrPayload? _extrairParticipante(String? payload) {
    try {
      return ParticipanteQrPayload.fromRawJson(payload);
    } on FormatException {
      return null;
    }
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
