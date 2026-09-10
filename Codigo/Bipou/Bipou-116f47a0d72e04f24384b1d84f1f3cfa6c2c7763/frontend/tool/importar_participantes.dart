import 'dart:convert';
import 'dart:io';

import 'package:bipou_frontend/tools/lote_csv.dart';

Future<void> main(List<String> argumentos) async {
  try {
    final opcoes = _OpcoesImportacao.ler(argumentos);
    final arquivo = File(opcoes.arquivoCsv);
    if (!await arquivo.exists()) {
      throw UsageException('Arquivo não encontrado: ${opcoes.arquivoCsv}');
    }

    final participantes = lerParticipantesCsv(await arquivo.readAsString());
    if (opcoes.somenteValidar) {
      stdout.writeln(
        'CSV válido: ${participantes.length} participante(s), nenhum dado enviado.',
      );
      return;
    }

    final cliente = _ApiCliente(opcoes.apiBaseUrl);
    try {
      final cadastrados = await cliente.listarParticipantes();
      final porCpf = <String, Map<String, dynamic>>{
        for (final participante in cadastrados)
          if (participante['cpf'] case final String cpf) cpf: participante,
      };
      final novos = <ParticipanteImportacao>[];
      var ignorados = 0;

      for (final participante in participantes) {
        final existente = porCpf[participante.cpf];
        if (existente == null) {
          novos.add(participante);
          continue;
        }

        final nomeExistente = existente['nome'];
        if (nomeExistente is String &&
            nomeExistente.trim().toLowerCase() ==
                participante.nome.toLowerCase()) {
          ignorados++;
          continue;
        }
        throw StateError(
          'Linha ${participante.linha}: o CPF terminado em '
          '${participante.cpf.substring(7)} já pertence a outro nome.',
        );
      }

      if (novos.isNotEmpty) {
        await cliente.cadastrarLote(novos);
      }
      stdout.writeln(
        'Importação concluída: ${novos.length} cadastrado(s), '
        '$ignorados já existente(s).',
      );
    } finally {
      cliente.fechar();
    }
  } on UsageException catch (erro) {
    stderr.writeln(erro.message);
    stderr.writeln(_uso);
    exitCode = 64;
  } on FormatException catch (erro) {
    stderr.writeln('CSV inválido: ${erro.message}');
    exitCode = 65;
  } on Object catch (erro) {
    stderr.writeln('Falha na importação: $erro');
    exitCode = 1;
  }
}

const _uso = '''
Uso:
  dart run tool/importar_participantes.dart participantes.csv [opções]

Opções:
  --api=http://localhost:8080  URL do backend
  --validar                     Apenas valida o CSV, sem acessar a API
''';

final class _OpcoesImportacao {
  const _OpcoesImportacao({
    required this.arquivoCsv,
    required this.apiBaseUrl,
    required this.somenteValidar,
  });

  factory _OpcoesImportacao.ler(List<String> argumentos) {
    String? arquivo;
    var api = 'http://localhost:8080';
    var somenteValidar = false;
    for (final argumento in argumentos) {
      if (argumento.startsWith('--api=')) {
        api = argumento.substring('--api='.length);
      } else if (argumento == '--validar') {
        somenteValidar = true;
      } else if (argumento.startsWith('-')) {
        throw UsageException('Opção desconhecida: $argumento');
      } else if (arquivo == null) {
        arquivo = argumento;
      } else {
        throw const UsageException('Informe apenas um arquivo CSV.');
      }
    }
    if (arquivo == null) {
      throw const UsageException('Informe o caminho do arquivo CSV.');
    }
    _validarApi(api);
    return _OpcoesImportacao(
      arquivoCsv: arquivo,
      apiBaseUrl: api.replaceFirst(RegExp(r'/+$'), ''),
      somenteValidar: somenteValidar,
    );
  }

  final String arquivoCsv;
  final String apiBaseUrl;
  final bool somenteValidar;
}

final class _ApiCliente {
  _ApiCliente(this.baseUrl) : _http = HttpClient() {
    _http.connectionTimeout = const Duration(seconds: 8);
  }

  final String baseUrl;
  final HttpClient _http;

  Future<List<Map<String, dynamic>>> listarParticipantes() async {
    final resposta = await _requisitar('GET', '/api/participantes');
    final json = jsonDecode(resposta);
    if (json is! List) {
      throw const FormatException('A API retornou uma lista inválida.');
    }
    return json.map((item) {
      if (item is! Map) {
        throw const FormatException('A API retornou um participante inválido.');
      }
      return Map<String, dynamic>.from(item);
    }).toList();
  }

  Future<void> cadastrarLote(List<ParticipanteImportacao> participantes) async {
    final resposta = await _requisitar(
      'POST',
      '/api/participantes/lote',
      corpo: <String, Object>{
        'participantes': participantes.map((item) => item.toJson()).toList(),
      },
      statusEsperado: 201,
    );
    final json = jsonDecode(resposta);
    if (json is! List || json.length != participantes.length) {
      throw const FormatException(
        'A API retornou um resultado de lote inválido.',
      );
    }
  }

  Future<String> _requisitar(
    String metodo,
    String caminho, {
    Object? corpo,
    int statusEsperado = 200,
  }) async {
    final requisicao = await _http.openUrl(
      metodo,
      Uri.parse('$baseUrl$caminho'),
    );
    requisicao.headers.contentType = ContentType.json;
    requisicao.headers.set(HttpHeaders.acceptHeader, ContentType.json.mimeType);
    if (corpo != null) {
      requisicao.write(jsonEncode(corpo));
    }
    final resposta = await requisicao.close().timeout(
      const Duration(seconds: 30),
    );
    final texto = await utf8.decoder.bind(resposta).join();
    if (resposta.statusCode != statusEsperado) {
      throw HttpException(
        'API respondeu HTTP ${resposta.statusCode}: ${_mensagemApi(texto)}',
        uri: Uri.parse('$baseUrl$caminho'),
      );
    }
    return texto;
  }

  void fechar() => _http.close(force: true);
}

String _mensagemApi(String corpo) {
  try {
    final json = jsonDecode(corpo);
    if (json case {'mensagem': final String mensagem}) {
      return mensagem;
    }
  } on FormatException {
    // Mantém uma mensagem segura quando a resposta não for JSON.
  }
  return 'resposta recusada pelo backend';
}

void _validarApi(String api) {
  final uri = Uri.tryParse(api);
  if (uri == null ||
      !(uri.scheme == 'http' || uri.scheme == 'https') ||
      uri.host.isEmpty ||
      uri.hasQuery ||
      uri.hasFragment) {
    throw UsageException('URL da API inválida: $api');
  }
}

final class UsageException implements Exception {
  const UsageException(this.message);

  final String message;
}
