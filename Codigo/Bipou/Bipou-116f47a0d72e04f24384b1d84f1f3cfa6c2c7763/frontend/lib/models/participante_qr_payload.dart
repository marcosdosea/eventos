import 'dart:convert';
import 'package:crypto/crypto.dart';
import 'package:uuid/uuid.dart';

final class ParticipanteQrPayload {
  ParticipanteQrPayload({
    required this.id,
    required String nome,
    this.v,
    this.pid,
    this.cpf,
    this.eid,
    this.sid = const <int>[],
    this.hash,
  }) : nome = nome.trim() {
    if (!isV1 && !_uuidPattern.hasMatch(id)) {
      throw ArgumentError.value(id, 'id', 'UUID invalido');
    }
    if (this.nome.isEmpty || this.nome.length > 150) {
      throw ArgumentError.value(
        nome,
        'nome',
        'O nome deve ter entre 1 e 150 caracteres',
      );
    }
  }

  /// Construtor para o formato unificado v1
  factory ParticipanteQrPayload.v1({
    required int pid,
    required String cpf,
    required String nome,
    required int eid,
    List<int> sid = const <int>[],
    String? hash,
    String? secretKey,
  }) {
    final cpfLimpo = cpf.replaceAll(RegExp(r'\D'), '');
    if (cpfLimpo.length != 11) {
      throw ArgumentError.value(cpf, 'cpf', 'CPF deve conter 11 digitos');
    }

    final sidOrdenado = sid.toSet().toList()..sort();
    final hashFinal = hash ??
        (secretKey != null
            ? calcularHash(
                v: 1,
                pid: pid,
                cpf: cpfLimpo,
                nome: nome.trim(),
                eid: eid,
                sid: sidOrdenado,
                secretKey: secretKey,
              )
            : '');

    final deterministicId = _gerarUuidV5(pid);

    return ParticipanteQrPayload(
      id: deterministicId,
      nome: nome,
      v: 1,
      pid: pid,
      cpf: cpfLimpo,
      eid: eid,
      sid: sidOrdenado,
      hash: hashFinal,
    );
  }

  /// Parser com suporte ao novo formato JSON v1, formato legado JSON com UUID,
  /// e fallback para o formato textual legado com colchetes do Boralá.
  factory ParticipanteQrPayload.fromRawJson(
    String? rawJson, {
    String? secretKey,
  }) {
    if (rawJson == null || rawJson.trim().isEmpty) {
      throw const FormatException('QR Code vazio.');
    }

    final trimmed = rawJson.trim();

    // Fallback: formato textual legado do Boralá "[1042] [Maria Santos] [15] [42] [43]"
    if (trimmed.startsWith('[')) {
      final matches = RegExp(r'\[(.*?)\]')
          .allMatches(trimmed)
          .map((m) => m.group(1)?.trim() ?? '')
          .toList();
      if (matches.length >= 3) {
        final pid = int.tryParse(matches[0]);
        final nome = matches[1];
        final eid = int.tryParse(matches[2]);
        if (pid != null && eid != null && nome.isNotEmpty) {
          final sids = <int>[];
          for (var i = 3; i < matches.length; i++) {
            final sid = int.tryParse(matches[i]);
            if (sid != null) {
              sids.add(sid);
            }
          }
          return ParticipanteQrPayload.v1(
            pid: pid,
            cpf: '00000000000',
            nome: nome,
            eid: eid,
            sid: sids,
            hash: '',
          );
        }
      }
      throw const FormatException('QR Code invalido.');
    }

    dynamic decoded;
    try {
      decoded = jsonDecode(trimmed);
    } on Object catch (e) {
      throw FormatException('QR Code invalido.', e);
    }

    if (decoded is! Map<String, dynamic>) {
      throw const FormatException('QR Code invalido.');
    }

    // Formato Unificado v1: contém "v" (ou "pid" e "eid")
    if ((decoded.containsKey('v') && decoded['v'] == 1) ||
        (decoded.containsKey('pid') && decoded.containsKey('eid'))) {
      final v = decoded['v'] is int ? decoded['v'] as int : 1;
      final rawPid = decoded['pid'];
      final rawCpf = decoded['cpf'];
      final rawNome = decoded['nome'];
      final rawEid = decoded['eid'];
      final rawSid = decoded['sid'];
      final rawHash = decoded['hash'];

      if (rawPid is! num || rawNome is! String || rawEid is! num) {
        throw const FormatException('QR Code invalido: campos obrigatorios ausentes.');
      }

      final pid = rawPid.toInt();
      final eid = rawEid.toInt();
      final nome = rawNome.trim();
      if (nome.isEmpty || nome.length > 150) {
        throw const FormatException('QR Code invalido: nome invalido.');
      }

      final cpfStr = rawCpf is String ? rawCpf : '';
      final cpfLimpo = cpfStr.replaceAll(RegExp(r'\D'), '');
      if (cpfLimpo.length != 11) {
        throw const FormatException('QR Code invalido: CPF deve conter 11 digitos.');
      }

      final sids = <int>[];
      if (rawSid is List) {
        for (final item in rawSid) {
          if (item is num) {
            sids.add(item.toInt());
          }
        }
      }

      final hashStr = rawHash is String ? rawHash.trim() : '';

      if (secretKey != null) {
        final hashEsperado = calcularHash(
          v: v,
          pid: pid,
          cpf: cpfLimpo,
          nome: nome,
          eid: eid,
          sid: sids,
          secretKey: secretKey,
        );
        if (hashStr.toLowerCase() != hashEsperado.toLowerCase()) {
          throw const FormatException('Assinatura do QR Code (hash) invalida.');
        }
      }

      return ParticipanteQrPayload.v1(
        pid: pid,
        cpf: cpfLimpo,
        nome: nome,
        eid: eid,
        sid: sids,
        hash: hashStr,
      );
    }

    // Formato Legado JSON: {"id": "UUID", "nome": "Nome"}
    final id = decoded['id'];
    final nome = decoded['nome'];
    if (id is! String || nome is! String) {
      throw const FormatException('QR Code invalido.');
    }

    try {
      return ParticipanteQrPayload(id: id, nome: nome);
    } on ArgumentError catch (error) {
      throw FormatException('QR Code invalido.', error);
    }
  }

  static final RegExp _uuidPattern = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
  );

  static String _gerarUuidV5(int pid) {
    const uuid = Uuid();
    return uuid.v5(Uuid.NAMESPACE_URL, 'borala:pessoa:$pid');
  }

  /// Gera a chave padrão derivada para o evento
  static String obterChavePadraoEvento(int idEvento, [String? salt]) {
    final prefixo = (salt == null || salt.trim().isEmpty) ? 'borala:evento' : salt.trim();
    return '$prefixo:$idEvento:secret';
  }

  /// String canônica determinística para assinatura HMAC-SHA256
  static String obterStringCanonizada({
    required int v,
    required int pid,
    required String cpf,
    required String nome,
    required int eid,
    required List<int> sid,
  }) {
    final sidOrdenado = sid.toSet().toList()..sort();
    final cpfLimpo = cpf.replaceAll(RegExp(r'\D'), '').padLeft(11, '0');
    return '$v|$pid|$cpfLimpo|${nome.trim()}|$eid|${sidOrdenado.join(',')}';
  }

  /// Calcula a assinatura HMAC-SHA256 truncada em 8 hex minúsculos
  static String calcularHash({
    required int v,
    required int pid,
    required String cpf,
    required String nome,
    required int eid,
    required List<int> sid,
    required String secretKey,
  }) {
    final canonical = obterStringCanonizada(
      v: v,
      pid: pid,
      cpf: cpf,
      nome: nome,
      eid: eid,
      sid: sid,
    );
    final keyBytes = utf8.encode(secretKey);
    final dataBytes = utf8.encode(canonical);
    final hmac = Hmac(sha256, keyBytes);
    final digest = hmac.convert(dataBytes);
    final hex = digest.toString().toLowerCase();
    return hex.length >= 8 ? hex.substring(0, 8) : hex;
  }

  /// Valida se o hash bate com a chave secreta informada
  bool validarHash(String secretKey) {
    if (!isV1 || hash == null || hash!.isEmpty || pid == null || eid == null || cpf == null) {
      return false;
    }
    final esperado = calcularHash(
      v: v ?? 1,
      pid: pid!,
      cpf: cpf!,
      nome: nome,
      eid: eid!,
      sid: sid,
      secretKey: secretKey,
    );
    return hash!.toLowerCase() == esperado.toLowerCase();
  }

  final String id;
  final String nome;
  final int? v;
  final int? pid;
  final String? cpf;
  final int? eid;
  final List<int> sid;
  final String? hash;

  bool get isV1 => v == 1 || pid != null;

  Map<String, dynamic> toJson() {
    if (isV1) {
      return <String, dynamic>{
        'v': v ?? 1,
        'pid': pid,
        'cpf': cpf,
        'nome': nome,
        'eid': eid,
        'sid': sid,
        'hash': hash ?? '',
      };
    }
    return <String, String>{'id': id, 'nome': nome};
  }

  String toRawJson() => jsonEncode(toJson());
}
