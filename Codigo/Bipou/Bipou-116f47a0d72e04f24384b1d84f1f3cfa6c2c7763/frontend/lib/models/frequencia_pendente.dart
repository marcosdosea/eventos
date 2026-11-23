import 'package:bipou_frontend/models/leitura_qr_code_request.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';

enum StatusEnvioFrequencia {
  pendente('PENDENTE'),
  enviado('ENVIADO'),
  erro('ERRO');

  const StatusEnvioFrequencia(this.apiValue);

  final String apiValue;

  static StatusEnvioFrequencia fromValue(String value) {
    final normalized = value.trim().toUpperCase();
    for (final status in StatusEnvioFrequencia.values) {
      if (status.apiValue == normalized) {
        return status;
      }
    }
    throw FormatException('StatusEnvioFrequencia invalido: $value');
  }
}

final class FrequenciaPendente {
  FrequenciaPendente({
    required this.leituraId,
    required this.idPessoa,
    required this.participanteId,
    required String participanteNome,
    required this.idEvento,
    required this.tipoAcao,
    required DateTime dataHoraLida,
    required String dispositivoId,
    this.idSubEvento,
    this.statusEnvio = StatusEnvioFrequencia.pendente,
    this.tentativasEnvio = 0,
    this.ultimoErro,
  }) : participanteNome = participanteNome.trim(),
       dispositivoId = dispositivoId.trim(),
       dataHoraLida = dataHoraLida.toUtc() {
    _validarUuid(leituraId, fieldName: 'leituraId');
    _validarUuid(participanteId, fieldName: 'participanteId');
    if (idPessoa < 0) {
      throw ArgumentError.value(
        idPessoa,
        'idPessoa',
        'O identificador da pessoa nao pode ser negativo',
      );
    }
    if (idEvento < 0) {
      throw ArgumentError.value(
        idEvento,
        'idEvento',
        'O identificador do evento nao pode ser negativo',
      );
    }
    if (idSubEvento != null && idSubEvento! <= 0) {
      throw ArgumentError.value(
        idSubEvento,
        'idSubEvento',
        'O identificador do subevento deve ser positivo',
      );
    }
    if (this.dispositivoId.isEmpty || this.dispositivoId.length > 100) {
      throw ArgumentError.value(
        dispositivoId,
        'dispositivoId',
        'O identificador do dispositivo deve ter entre 1 e 100 caracteres',
      );
    }
    if (tentativasEnvio < 0) {
      throw ArgumentError.value(
        tentativasEnvio,
        'tentativasEnvio',
        'O numero de tentativas nao pode ser negativo',
      );
    }
  }

  factory FrequenciaPendente.fromMap(Map<String, Object?> map) {
    final leituraId = map['leitura_id'];
    final idPessoa = map['id_pessoa'];
    final participanteId = map['participante_id'];
    final participanteNome = map['participante_nome'];
    final idEvento = map['id_evento'];
    final idSubEvento = map['id_sub_evento'];
    final tipoAcao = map['tipo_acao'];
    final dataHoraLida = map['data_hora_lida'];
    final dispositivoId = map['dispositivo_id'];
    final statusEnvio = map['status_envio'];
    final tentativasEnvio = map['tentativas_envio'];
    final ultimoErro = map['ultimo_erro'];

    if (leituraId is! String ||
        idPessoa is! int ||
        participanteId is! String ||
        participanteNome is! String ||
        idEvento is! int ||
        tipoAcao is! String ||
        dataHoraLida is! String ||
        dispositivoId is! String ||
        statusEnvio is! String ||
        tentativasEnvio is! int) {
      throw const FormatException('Registro de frequencia pendente invalido.');
    }

    if (idSubEvento != null && idSubEvento is! int) {
      throw const FormatException('Campo id_sub_evento invalido.');
    }
    if (ultimoErro != null && ultimoErro is! String) {
      throw const FormatException('Campo ultimo_erro invalido.');
    }

    return FrequenciaPendente(
      leituraId: leituraId,
      idPessoa: idPessoa,
      participanteId: participanteId,
      participanteNome: participanteNome,
      idEvento: idEvento,
      idSubEvento: idSubEvento as int?,
      tipoAcao: _parseTipoAcao(tipoAcao),
      dataHoraLida: DateTime.parse(dataHoraLida),
      dispositivoId: dispositivoId,
      statusEnvio: StatusEnvioFrequencia.fromValue(statusEnvio),
      tentativasEnvio: tentativasEnvio,
      ultimoErro: ultimoErro as String?,
    );
  }

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
  );

  final String leituraId;
  final int idPessoa;
  final String participanteId;
  final String participanteNome;
  final int idEvento;
  final int? idSubEvento;
  final TipoAcao tipoAcao;
  final DateTime dataHoraLida;
  final String dispositivoId;
  final StatusEnvioFrequencia statusEnvio;
  final int tentativasEnvio;
  final String? ultimoErro;

  bool get possuiContextoBorala => idPessoa > 0 && idEvento > 0;

  FrequenciaPendente copyWith({
    StatusEnvioFrequencia? statusEnvio,
    int? tentativasEnvio,
    String? ultimoErro,
    bool limparUltimoErro = false,
  }) {
    return FrequenciaPendente(
      leituraId: leituraId,
      idPessoa: idPessoa,
      participanteId: participanteId,
      participanteNome: participanteNome,
      idEvento: idEvento,
      idSubEvento: idSubEvento,
      tipoAcao: tipoAcao,
      dataHoraLida: dataHoraLida,
      dispositivoId: dispositivoId,
      statusEnvio: statusEnvio ?? this.statusEnvio,
      tentativasEnvio: tentativasEnvio ?? this.tentativasEnvio,
      ultimoErro: limparUltimoErro ? null : (ultimoErro ?? this.ultimoErro),
    );
  }

  Map<String, Object?> toMap() => <String, Object?>{
    'leitura_id': leituraId,
    'id_pessoa': idPessoa,
    'participante_id': participanteId,
    'participante_nome': participanteNome,
    'id_evento': idEvento,
    'id_sub_evento': idSubEvento,
    'tipo_acao': tipoAcao.apiValue,
    'data_hora_lida': dataHoraLida.toIso8601String(),
    'dispositivo_id': dispositivoId,
    'status_envio': statusEnvio.apiValue,
    'tentativas_envio': tentativasEnvio,
    'ultimo_erro': ultimoErro,
  };

  Map<String, Object?> toBipouLoteJson() => <String, Object?>{
    'leituraId': leituraId,
    'idPessoa': idPessoa,
    'idEvento': idEvento,
    if (idSubEvento != null) 'idSubEvento': idSubEvento,
    'tipoAcao': tipoAcao.apiValue,
    'dataHoraLida': dataHoraLida.toIso8601String(),
    'dispositivoId': dispositivoId,
  };

  LeituraQrCodeRequest toLeituraQrCodeRequest() => LeituraQrCodeRequest(
    leituraId: leituraId,
    participanteId: participanteId,
    tipoAcao: tipoAcao,
    dispositivoId: dispositivoId,
    dataHoraLidaNoCelular: dataHoraLida,
  );

  static TipoAcao _parseTipoAcao(String value) {
    final normalized = value.trim().toUpperCase();
    for (final acao in TipoAcao.values) {
      if (acao.apiValue == normalized) {
        return acao;
      }
    }
    throw FormatException('TipoAcao invalido: $value');
  }

  static void _validarUuid(String value, {required String fieldName}) {
    if (!_uuidPattern.hasMatch(value)) {
      throw ArgumentError.value(value, fieldName, 'UUID invalido');
    }
  }
}
