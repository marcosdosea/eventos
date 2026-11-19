import 'dart:convert';

import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_response.dart';

final class LocalParticipant {
  LocalParticipant({
    required this.id,
    required this.idPessoa,
    required String nome,
    String? nomeCracha,
    required String cpf,
    required this.eventoId,
    List<int> subeventoIds = const <int>[],
    required String hashValidacao,
    String statusInscricao = 'A',
  }) : nome = nome.trim(),
       nomeCracha = _normalizeOptional(nomeCracha),
       cpf = ParticipanteQrPayload.sanitizarCpf(cpf),
       subeventoIds = List<int>.unmodifiable(
         subeventoIds.where((sid) => sid > 0).toSet().toList()..sort(),
       ),
       hashValidacao = hashValidacao.trim().toLowerCase(),
       statusInscricao = statusInscricao.trim().isEmpty
           ? 'A'
           : statusInscricao.trim() {
    if (id.trim().isEmpty) {
      throw ArgumentError.value(id, 'id', 'Identificador obrigatorio.');
    }
    if (idPessoa <= 0) {
      throw ArgumentError.value(
        idPessoa,
        'idPessoa',
        'idPessoa deve ser positivo.',
      );
    }
    if (this.nome.isEmpty) {
      throw ArgumentError.value(nome, 'nome', 'Nome obrigatorio.');
    }
    if (eventoId <= 0) {
      throw ArgumentError.value(
        eventoId,
        'eventoId',
        'eventoId deve ser positivo.',
      );
    }
  }

  factory LocalParticipant.fromBorala({
    required int eventoId,
    required BoralaParticipante participante,
    String? secretKey,
  }) {
    final nomeParaHash = participante.nomeExibicao;
    final payload = ParticipanteQrPayload.v1(
      pid: participante.idPessoa,
      cpf: participante.cpf,
      nome: nomeParaHash,
      eid: eventoId,
      sid: participante.subeventosInscritos,
      secretKey: secretKey,
    );

    return LocalParticipant(
      id: payload.id,
      idPessoa: participante.idPessoa,
      nome: participante.nome,
      nomeCracha: participante.nomeCracha,
      cpf: participante.cpf,
      eventoId: eventoId,
      subeventoIds: participante.subeventosInscritos,
      hashValidacao: payload.hash ?? '',
      statusInscricao: participante.statusInscricao,
    );
  }

  factory LocalParticipant.fromMap(Map<String, Object?> map) {
    final rawSubIds = map['subevento_ids'];
    final subIds = <int>[];
    if (rawSubIds is String && rawSubIds.trim().isNotEmpty) {
      final decoded = jsonDecode(rawSubIds);
      if (decoded is List) {
        for (final item in decoded) {
          if (item is num && item.toInt() > 0) {
            subIds.add(item.toInt());
          }
        }
      }
    }

    return LocalParticipant(
      id: map['id']! as String,
      idPessoa: (map['id_pessoa']! as num).toInt(),
      nome: map['nome']! as String,
      nomeCracha: map['nome_cracha'] as String?,
      cpf: map['cpf']! as String,
      eventoId: (map['evento_id']! as num).toInt(),
      subeventoIds: subIds,
      hashValidacao: (map['hash_validacao'] as String?) ?? '',
      statusInscricao: (map['status_inscricao'] as String?) ?? 'A',
    );
  }

  final String id;
  final int idPessoa;
  final String nome;
  final String? nomeCracha;
  final String cpf;
  final int eventoId;
  final List<int> subeventoIds;
  final String hashValidacao;
  final String statusInscricao;

  String get nomeExibicao => nomeCracha ?? nome;

  bool inscritoNoSubevento(int subeventoId) =>
      subeventoIds.contains(subeventoId);

  ParticipanteQrPayload toQrPayload() {
    return ParticipanteQrPayload.v1(
      pid: idPessoa,
      cpf: cpf,
      nome: nomeExibicao,
      eid: eventoId,
      sid: subeventoIds,
      hash: hashValidacao.isEmpty ? null : hashValidacao,
    );
  }

  ParticipanteResponse toParticipanteResponse() {
    return ParticipanteResponse(id: id, nome: nome, cpf: cpf);
  }

  Map<String, Object?> toMap() => <String, Object?>{
    'id': id,
    'id_pessoa': idPessoa,
    'nome': nome,
    'nome_cracha': nomeCracha,
    'cpf': cpf,
    'evento_id': eventoId,
    'subevento_ids': jsonEncode(subeventoIds),
    'hash_validacao': hashValidacao,
    'status_inscricao': statusInscricao,
  };

  static String? _normalizeOptional(String? raw) {
    if (raw == null) {
      return null;
    }
    final trimmed = raw.trim();
    return trimmed.isEmpty ? null : trimmed;
  }
}
