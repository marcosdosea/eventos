import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:bipou_frontend/models/participante_qr_payload.dart';

final class BoralaEvento {
  const BoralaEvento({
    required this.idEvento,
    required this.nome,
    this.dataInicio,
    this.dataFim,
  });

  factory BoralaEvento.fromJson(Map<String, dynamic> json) {
    final id = _positiveInt(json, 'idEvento');
    final nome = _nonEmptyString(json, 'nome');
    return BoralaEvento(
      idEvento: id,
      nome: nome,
      dataInicio: _optionalDateTime(json['dataInicio']),
      dataFim: _optionalDateTime(json['dataFim']),
    );
  }

  final int idEvento;
  final String nome;
  final DateTime? dataInicio;
  final DateTime? dataFim;

  Map<String, Object?> toJson() => <String, Object?>{
    'idEvento': idEvento,
    'nome': nome,
    if (dataInicio != null) 'dataInicio': dataInicio!.toIso8601String(),
    if (dataFim != null) 'dataFim': dataFim!.toIso8601String(),
  };
}

final class BoralaSubevento {
  const BoralaSubevento({
    required this.idSubEvento,
    required this.idEvento,
    required this.nome,
    this.dataInicio,
    this.dataFim,
  });

  factory BoralaSubevento.fromJson(Map<String, dynamic> json) {
    final idSubEvento = _positiveInt(json, 'idSubEvento');
    final idEvento = _positiveInt(json, 'idEvento');
    final nome = _nonEmptyString(json, 'nome');
    return BoralaSubevento(
      idSubEvento: idSubEvento,
      idEvento: idEvento,
      nome: nome,
      dataInicio: _optionalDateTime(json['dataInicio']),
      dataFim: _optionalDateTime(json['dataFim']),
    );
  }

  final int idSubEvento;
  final int idEvento;
  final String nome;
  final DateTime? dataInicio;
  final DateTime? dataFim;

  Map<String, Object?> toJson() => <String, Object?>{
    'idSubEvento': idSubEvento,
    'idEvento': idEvento,
    'nome': nome,
    if (dataInicio != null) 'dataInicio': dataInicio!.toIso8601String(),
    if (dataFim != null) 'dataFim': dataFim!.toIso8601String(),
  };
}

final class BoralaParticipante {
  BoralaParticipante({
    required this.idPessoa,
    required String nome,
    String? nomeCracha,
    required String cpf,
    String statusInscricao = 'A',
    List<int> subeventosInscritos = const <int>[],
  }) : nome = nome.trim(),
       nomeCracha = _normalizeOptionalString(nomeCracha),
       cpf = ParticipanteQrPayload.sanitizarCpf(cpf),
       statusInscricao = statusInscricao.trim().isEmpty
           ? 'A'
           : statusInscricao.trim(),
       subeventosInscritos = List<int>.unmodifiable(
         subeventosInscritos.where((id) => id > 0).toSet().toList()..sort(),
       );

  factory BoralaParticipante.fromJson(Map<String, dynamic> json) {
    final idPessoa = _positiveInt(json, 'idPessoa');
    final nome = _nonEmptyString(json, 'nome');
    final rawCracha = json['nomeCracha'];
    final rawCpf = json['cpf'];
    final rawStatus = json['statusInscricao'];
    final rawSubIds = json['subeventosInscritos'] ?? json['subeventoIds'];

    final subIds = <int>[];
    if (rawSubIds is List) {
      for (final item in rawSubIds) {
        if (item is num && item.toInt() > 0) {
          subIds.add(item.toInt());
        } else if (item is String) {
          final parsed = int.tryParse(item.trim());
          if (parsed != null && parsed > 0) {
            subIds.add(parsed);
          }
        }
      }
    }

    return BoralaParticipante(
      idPessoa: idPessoa,
      nome: nome,
      nomeCracha: rawCracha is String ? rawCracha : null,
      cpf: rawCpf is String ? rawCpf : '',
      statusInscricao: rawStatus is String ? rawStatus : 'A',
      subeventosInscritos: subIds,
    );
  }

  final int idPessoa;
  final String nome;
  final String? nomeCracha;
  final String cpf;
  final String statusInscricao;
  final List<int> subeventosInscritos;

  String get nomeExibicao => nomeCracha ?? nome;

  Map<String, Object?> toJson() => <String, Object?>{
    'idPessoa': idPessoa,
    'nome': nome,
    if (nomeCracha != null) 'nomeCracha': nomeCracha,
    'cpf': cpf,
    'statusInscricao': statusInscricao,
    'subeventosInscritos': subeventosInscritos,
  };
}

final class BoralaCargaOffline {
  const BoralaCargaOffline({
    required this.versao,
    required this.evento,
    required this.subeventos,
    required this.participantes,
  });

  factory BoralaCargaOffline.fromJson(Map<String, dynamic> json) {
    final rawEvento = json['evento'];
    if (rawEvento is! Map) {
      throw const FormatException(
        'Arquivo de carga invalido: campo "evento" ausente.',
      );
    }

    final rawSubeventos = json['subeventos'];
    final rawParticipantes = json['participantes'];
    if (rawParticipantes is! List) {
      throw const FormatException(
        'Arquivo de carga invalido: campo "participantes" ausente.',
      );
    }

    final versao = json['versao'] is num ? (json['versao'] as num).toInt() : 1;
    final evento = BoralaEvento.fromJson(Map<String, dynamic>.from(rawEvento));

    final subeventos = <BoralaSubevento>[];
    if (rawSubeventos is List) {
      for (final item in rawSubeventos) {
        if (item is! Map) {
          throw const FormatException('Subevento invalido no arquivo de carga.');
        }
        subeventos.add(
          BoralaSubevento.fromJson(Map<String, dynamic>.from(item)),
        );
      }
    }

    final participantes = <BoralaParticipante>[];
    for (final item in rawParticipantes) {
      if (item is! Map) {
        throw const FormatException(
          'Participante invalido no arquivo de carga.',
        );
      }
      participantes.add(
        BoralaParticipante.fromJson(Map<String, dynamic>.from(item)),
      );
    }

    return BoralaCargaOffline(
      versao: versao,
      evento: evento,
      subeventos: List<BoralaSubevento>.unmodifiable(subeventos),
      participantes: List<BoralaParticipante>.unmodifiable(participantes),
    );
  }

  /// Decodifica um pacote exportado pelo Boralá, aceitando tanto JSON puro
  /// (UTF-8) quanto arquivo compactado em GZIP (`.json.gz`).
  factory BoralaCargaOffline.fromRawBytes(Uint8List bytes) {
    if (bytes.isEmpty) {
      throw const FormatException('Arquivo de carga vazio.');
    }

    final isGzip = bytes.length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B;
    final List<int> decodedBytes;
    if (isGzip) {
      try {
        decodedBytes = gzip.decode(bytes);
      } on Object catch (error) {
        throw FormatException('Falha ao descompactar arquivo GZIP.', error);
      }
    } else {
      decodedBytes = bytes;
    }

    final rawText = utf8.decode(decodedBytes, allowMalformed: false);
    return BoralaCargaOffline.fromRawJson(rawText);
  }

  factory BoralaCargaOffline.fromRawJson(String rawJson) {
    final trimmed = rawJson.trim();
    if (trimmed.isEmpty) {
      throw const FormatException('Conteudo JSON vazio.');
    }

    final Object? decoded;
    try {
      decoded = jsonDecode(trimmed);
    } on Object catch (error) {
      throw FormatException('Conteudo JSON invalido.', error);
    }

    if (decoded is! Map) {
      throw const FormatException(
        'O arquivo de carga deve conter um objeto JSON valido.',
      );
    }

    return BoralaCargaOffline.fromJson(Map<String, dynamic>.from(decoded));
  }

  final int versao;
  final BoralaEvento evento;
  final List<BoralaSubevento> subeventos;
  final List<BoralaParticipante> participantes;

  Map<String, Object?> toJson() => <String, Object?>{
    'versao': versao,
    'evento': evento.toJson(),
    'subeventos': subeventos.map((s) => s.toJson()).toList(),
    'participantes': participantes.map((p) => p.toJson()).toList(),
  };
}

final class BoralaSessaoToken {
  const BoralaSessaoToken({
    required this.accessToken,
    this.tokenType = 'Bearer',
    this.expiresIn,
    this.refreshToken,
  });

  factory BoralaSessaoToken.fromJson(Map<String, dynamic> json) {
    final token = _nonEmptyString(json, 'accessToken');
    final tokenType = json['tokenType'] is String &&
            (json['tokenType'] as String).trim().isNotEmpty
        ? (json['tokenType'] as String).trim()
        : 'Bearer';
    final expiresIn = json['expiresIn'] is num
        ? (json['expiresIn'] as num).toInt()
        : null;
    final refreshToken = json['refreshToken'] is String
        ? json['refreshToken'] as String
        : null;
    return BoralaSessaoToken(
      accessToken: token,
      tokenType: tokenType,
      expiresIn: expiresIn,
      refreshToken: refreshToken,
    );
  }

  final String accessToken;
  final String tokenType;
  final int? expiresIn;
  final String? refreshToken;
}

int _positiveInt(Map<String, dynamic> json, String key) {
  final value = json[key];
  final int? parsed;
  if (value is num) {
    parsed = value.toInt();
  } else if (value is String) {
    parsed = int.tryParse(value.trim());
  } else {
    parsed = null;
  }

  if (parsed == null || parsed <= 0) {
    throw FormatException('Campo "$key" ausente ou invalido.');
  }
  return parsed;
}

String _nonEmptyString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! String || value.trim().isEmpty) {
    throw FormatException('Campo "$key" ausente ou invalido.');
  }
  return value.trim();
}

DateTime? _optionalDateTime(Object? raw) {
  if (raw is! String || raw.trim().isEmpty) {
    return null;
  }
  return DateTime.tryParse(raw.trim());
}

String? _normalizeOptionalString(String? raw) {
  if (raw == null) {
    return null;
  }
  final trimmed = raw.trim();
  return trimmed.isEmpty ? null : trimmed;
}
