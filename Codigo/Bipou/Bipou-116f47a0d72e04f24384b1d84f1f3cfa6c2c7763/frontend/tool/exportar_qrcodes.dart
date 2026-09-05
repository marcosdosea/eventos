import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/tools/credenciais_pdf.dart';

Future<void> main(List<String> argumentos) async {
  HttpClient? cliente;
  try {
    final opcoes = _OpcoesExportacao.ler(argumentos);
    cliente = HttpClient()..connectionTimeout = const Duration(seconds: 8);
    stdout.writeln('Consultando participantes em ${opcoes.apiBaseUrl}...');
    final requisicao = await cliente.getUrl(
      Uri.parse('${opcoes.apiBaseUrl}/api/participantes'),
    );
    requisicao.headers.set(HttpHeaders.acceptHeader, ContentType.json.mimeType);
    final resposta = await requisicao.close().timeout(
      const Duration(seconds: 30),
    );
    final texto = await utf8.decoder.bind(resposta).join();
    if (resposta.statusCode != 200) {
      throw HttpException('API respondeu HTTP ${resposta.statusCode}.');
    }

    final json = jsonDecode(texto);
    if (json is! List) {
      throw const FormatException('A API retornou uma lista inválida.');
    }
    final todosParticipantes = json.map((item) {
      if (item is! Map) {
        throw const FormatException('A API retornou um participante inválido.');
      }
      return ParticipanteResponse.fromJson(Map<String, dynamic>.from(item));
    }).toList();
    final participantes =
        todosParticipantes
            .where(
              (participante) =>
                  opcoes.cpfs == null ||
                  opcoes.cpfs!.contains(participante.cpf),
            )
            .toList()
          ..sort(
            (a, b) => a.nome.toLowerCase().compareTo(b.nome.toLowerCase()),
          );

    if (opcoes.cpfs case final cpfs?) {
      final encontrados = participantes.map((participante) => participante.cpf);
      final ausentes = cpfs.difference(encontrados.toSet()).toList()..sort();
      if (ausentes.isNotEmpty) {
        throw StateError(
          'CPF(s) não encontrado(s): ${ausentes.join(', ')}. '
          'Nenhum PDF foi gerado.',
        );
      }
    }
    if (participantes.isEmpty) {
      throw StateError('Não há participantes cadastrados para exportar.');
    }
    stdout.writeln(
      '${participantes.length} participante(s) encontrado(s). Gerando PDF...',
    );
    final logoUfsPng = await _lerImagem('logo-ufs-azul.png');
    final marcaDsiPng = await _lerImagem('MarcaDSI2026.png');
    final arquivo = File(opcoes.arquivoSaida);
    final pdf = gerarPdfCredenciais(
      participantes,
      logoUfsPng: logoUfsPng,
      marcaDsiPng: marcaDsiPng,
      aoProgresso: (geradas, total) {
        stdout.writeln('Credenciais geradas: $geradas/$total');
      },
    );
    stdout.writeln('Salvando ${arquivo.absolute.path}...');
    await arquivo.writeAsBytes(pdf, flush: true);
    final paginas =
        (participantes.length + credenciaisPorPagina - 1) ~/
        credenciaisPorPagina;
    stdout.writeln(
      'PDF criado em ${arquivo.absolute.path}: ${participantes.length} '
      'QR Code(s) em $paginas página(s) A4.',
    );
  } on UsageException catch (erro) {
    stderr.writeln(erro.message);
    stderr.writeln(_uso);
    exitCode = 64;
  } on Object catch (erro) {
    stderr.writeln('Falha na exportação: $erro');
    exitCode = 1;
  } finally {
    cliente?.close(force: true);
  }
}

const _uso = '''
Uso:
  ./tool/exportar_qrcodes.sh [opções]

Opções:
  --api=http://localhost:8080       URL do backend
  --saida=credenciais-bipou.pdf    Caminho do PDF de saída
  --cpfs=CPF1,CPF2                 Exporta somente os CPFs informados
''';

Future<Uint8List> _lerImagem(String nome) async {
  final arquivo = File(nome);
  if (!await arquivo.exists()) {
    throw StateError(
      'Imagem $nome não encontrada. Execute o comando dentro da pasta frontend.',
    );
  }
  return arquivo.readAsBytes();
}

final class _OpcoesExportacao {
  const _OpcoesExportacao({
    required this.apiBaseUrl,
    required this.arquivoSaida,
    required this.cpfs,
  });

  factory _OpcoesExportacao.ler(List<String> argumentos) {
    var api = 'http://localhost:8080';
    var saida = 'credenciais-bipou.pdf';
    Set<String>? cpfs;
    for (final argumento in argumentos) {
      if (argumento.startsWith('--api=')) {
        api = argumento.substring('--api='.length);
      } else if (argumento.startsWith('--saida=')) {
        saida = argumento.substring('--saida='.length);
      } else if (argumento.startsWith('--cpfs=')) {
        if (cpfs != null) {
          throw const UsageException('Informe a opção --cpfs apenas uma vez.');
        }
        cpfs = _lerCpfs(argumento.substring('--cpfs='.length));
      } else {
        throw UsageException('Opção desconhecida: $argumento');
      }
    }
    final uri = Uri.tryParse(api);
    if (uri == null ||
        !(uri.scheme == 'http' || uri.scheme == 'https') ||
        uri.host.isEmpty ||
        uri.hasQuery ||
        uri.hasFragment) {
      throw UsageException('URL da API inválida: $api');
    }
    if (saida.trim().isEmpty || !saida.toLowerCase().endsWith('.pdf')) {
      throw const UsageException(
        'O arquivo de saída precisa terminar em .pdf.',
      );
    }
    return _OpcoesExportacao(
      apiBaseUrl: api.replaceFirst(RegExp(r'/+$'), ''),
      arquivoSaida: saida,
      cpfs: cpfs,
    );
  }

  final String apiBaseUrl;
  final String arquivoSaida;
  final Set<String>? cpfs;
}

Set<String> _lerCpfs(String valor) {
  final informados = valor
      .split(RegExp(r'[,;]'))
      .map((cpf) => cpf.trim())
      .where((cpf) => cpf.isNotEmpty)
      .toList();
  if (informados.isEmpty) {
    throw const UsageException('Informe pelo menos um CPF em --cpfs.');
  }

  final cpfs = <String>{};
  for (final informado in informados) {
    if (!RegExp(r'^[0-9.\-\s]+$').hasMatch(informado)) {
      throw UsageException('CPF contém caracteres inválidos: $informado');
    }
    final cpf = informado.replaceAll(RegExp(r'\D'), '');
    if (!CpfValidator.isValid(cpf)) {
      throw UsageException('CPF inválido: $informado');
    }
    if (!cpfs.add(cpf)) {
      throw UsageException('CPF repetido em --cpfs: $informado');
    }
  }
  return cpfs;
}

final class UsageException implements Exception {
  const UsageException(this.message);

  final String message;
}
