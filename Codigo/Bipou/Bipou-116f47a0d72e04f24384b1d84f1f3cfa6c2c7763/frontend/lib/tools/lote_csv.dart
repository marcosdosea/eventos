import 'package:bipou_frontend/core/validation/cpf_validator.dart';

final class ParticipanteImportacao {
  const ParticipanteImportacao({
    required this.nome,
    required this.cpf,
    required this.linha,
  });

  final String nome;
  final String cpf;
  final int linha;

  Map<String, String> toJson() => <String, String>{'nome': nome, 'cpf': cpf};
}

List<ParticipanteImportacao> lerParticipantesCsv(String conteudo) {
  final texto = conteudo.startsWith('\uFEFF')
      ? conteudo.substring(1)
      : conteudo;
  final delimitador = _detectarDelimitador(texto);
  final registros = _lerRegistros(texto, delimitador)
      .where(
        (registro) => registro.campos.any((campo) => campo.trim().isNotEmpty),
      )
      .toList();

  if (registros.isEmpty) {
    throw const FormatException('O arquivo CSV está vazio.');
  }

  final cabecalho = registros.first.campos
      .map((campo) => campo.trim().toLowerCase())
      .toList();
  final indiceNome = cabecalho.indexOf('nome');
  final indiceSobrenome = cabecalho.indexOf('sobrenome');
  final indiceCpf = cabecalho.indexOf('cpf');
  if (indiceNome < 0 || indiceCpf < 0) {
    throw const FormatException(
      'O cabeçalho do CSV precisa conter as colunas "nome" e "cpf".',
    );
  }

  final participantes = <ParticipanteImportacao>[];
  final cpfs = <String>{};
  for (final registro in registros.skip(1)) {
    final maiorIndice = <int>[
      indiceNome,
      indiceSobrenome,
      indiceCpf,
    ].reduce((atual, proximo) => atual > proximo ? atual : proximo);
    if (registro.campos.length <= maiorIndice) {
      throw FormatException(
        'Linha ${registro.linha}: quantidade de colunas inválida.',
      );
    }

    final primeiroNome = registro.campos[indiceNome].trim();
    final sobrenome = indiceSobrenome >= 0
        ? registro.campos[indiceSobrenome].trim()
        : '';
    if (indiceSobrenome >= 0 && sobrenome.isEmpty) {
      throw FormatException(
        'Linha ${registro.linha}: o sobrenome é obrigatório.',
      );
    }
    final nome = [
      primeiroNome,
      sobrenome,
    ].where((parte) => parte.isNotEmpty).join(' ');
    if (nome.isEmpty || nome.length > 150) {
      throw FormatException(
        'Linha ${registro.linha}: o nome completo deve ter entre 1 e 150 caracteres.',
      );
    }

    final cpfInformado = registro.campos[indiceCpf].trim();
    if (!RegExp(r'^[0-9.\-\s]+$').hasMatch(cpfInformado)) {
      throw FormatException(
        'Linha ${registro.linha}: o CPF contém caracteres inválidos.',
      );
    }
    final cpf = cpfInformado.replaceAll(RegExp(r'\D'), '');
    if (cpf.length != 11) {
      throw FormatException(
        'Linha ${registro.linha}: o CPF deve conter exatamente 11 dígitos.',
      );
    }
    if (!CpfValidator.isValid(cpf)) {
      throw FormatException(
        'Linha ${registro.linha}: o CPF informado é inválido.',
      );
    }
    if (!cpfs.add(cpf)) {
      throw FormatException(
        'Linha ${registro.linha}: o CPF está repetido no arquivo.',
      );
    }

    participantes.add(
      ParticipanteImportacao(nome: nome, cpf: cpf, linha: registro.linha),
    );
  }

  if (participantes.isEmpty) {
    throw const FormatException('O CSV não contém participantes.');
  }
  if (participantes.length > 5000) {
    throw const FormatException(
      'O CSV pode conter no máximo 5000 participantes.',
    );
  }

  return participantes;
}

String _detectarDelimitador(String texto) {
  var virgulas = 0;
  var pontosEVirgulas = 0;
  var entreAspas = false;
  for (var indice = 0; indice < texto.length; indice++) {
    final caractere = texto[indice];
    if (caractere == '"') {
      if (entreAspas && indice + 1 < texto.length && texto[indice + 1] == '"') {
        indice++;
      } else {
        entreAspas = !entreAspas;
      }
    } else if (!entreAspas && (caractere == '\n' || caractere == '\r')) {
      break;
    } else if (!entreAspas && caractere == ',') {
      virgulas++;
    } else if (!entreAspas && caractere == ';') {
      pontosEVirgulas++;
    }
  }
  return pontosEVirgulas > virgulas ? ';' : ',';
}

List<_RegistroCsv> _lerRegistros(String texto, String delimitador) {
  final registros = <_RegistroCsv>[];
  var campos = <String>[];
  final campo = StringBuffer();
  var entreAspas = false;
  var linha = 1;
  var inicioRegistro = 1;

  void concluirRegistro() {
    campos.add(campo.toString());
    campo.clear();
    registros.add(_RegistroCsv(campos, inicioRegistro));
    campos = <String>[];
    inicioRegistro = linha + 1;
  }

  for (var indice = 0; indice < texto.length; indice++) {
    final caractere = texto[indice];
    if (caractere == '"') {
      if (entreAspas && indice + 1 < texto.length && texto[indice + 1] == '"') {
        campo.write('"');
        indice++;
      } else if (entreAspas || campo.toString().trim().isEmpty) {
        entreAspas = !entreAspas;
      } else {
        throw FormatException('Linha $linha: aspas em posição inválida.');
      }
      continue;
    }

    if (!entreAspas && caractere == delimitador) {
      campos.add(campo.toString());
      campo.clear();
    } else if (!entreAspas && (caractere == '\n' || caractere == '\r')) {
      if (caractere == '\r' &&
          indice + 1 < texto.length &&
          texto[indice + 1] == '\n') {
        indice++;
      }
      concluirRegistro();
      linha++;
    } else {
      campo.write(caractere);
      if (caractere == '\n') {
        linha++;
      }
    }
  }

  if (entreAspas) {
    throw FormatException(
      'Linha $inicioRegistro: campo entre aspas não foi fechado.',
    );
  }
  if (campo.isNotEmpty || campos.isNotEmpty) {
    concluirRegistro();
  }
  return registros;
}

final class _RegistroCsv {
  const _RegistroCsv(this.campos, this.linha);

  final List<String> campos;
  final int linha;
}
