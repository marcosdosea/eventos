import 'dart:convert';
import 'dart:io';
import 'dart:math' as math;
import 'dart:typed_data';

import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:qr/qr.dart';

const int credenciaisPorPagina = 15;

Uint8List gerarPdfCredenciais(
  List<ParticipanteResponse> participantes, {
  required Uint8List logoUfsPng,
  required Uint8List marcaDsiPng,
  void Function(int geradas, int total)? aoProgresso,
}) {
  if (participantes.isEmpty) {
    throw ArgumentError.value(
      participantes,
      'participantes',
      'A lista está vazia',
    );
  }

  final logoUfs = _ImagemPdf.dePng(logoUfsPng);
  final marcaDsi = _ImagemPdf.dePng(marcaDsiPng);
  final paginas = <String>[];
  for (
    var inicio = 0;
    inicio < participantes.length;
    inicio += credenciaisPorPagina
  ) {
    final fim = (inicio + credenciaisPorPagina).clamp(0, participantes.length);
    paginas.add(_conteudoPagina(participantes.sublist(inicio, fim)));
    aoProgresso?.call(fim, participantes.length);
  }
  return _PdfSimples(paginas, logoUfs, marcaDsi).gerar();
}

String _conteudoPagina(List<ParticipanteResponse> participantes) {
  const larguraPagina = 595.28;
  const alturaPagina = 841.89;
  const margem = 18.0;
  const colunas = 3;
  const linhas = 5;
  const larguraCelula = (larguraPagina - margem * 2) / colunas;
  const alturaCelula = (alturaPagina - margem * 2) / linhas;
  final comandos = StringBuffer()
    ..writeln('1 g')
    ..writeln('0 0 ${_n(larguraPagina)} ${_n(alturaPagina)} re f')
    ..writeln('0.75 w')
    ..writeln('0.82 G');

  for (var indice = 0; indice < participantes.length; indice++) {
    final coluna = indice % colunas;
    final linha = indice ~/ colunas;
    final x = margem + coluna * larguraCelula;
    final y = alturaPagina - margem - (linha + 1) * alturaCelula;
    _desenharCredencial(
      comandos,
      participantes[indice],
      x + 3,
      y + 3,
      larguraCelula - 6,
      alturaCelula - 6,
    );
  }
  return comandos.toString();
}

void _desenharCredencial(
  StringBuffer comandos,
  ParticipanteResponse participante,
  double x,
  double y,
  double largura,
  double altura,
) {
  comandos
    ..writeln('1 1 1 rg')
    ..writeln('${_cor(208, 213, 221)} RG')
    ..writeln('0.7 w')
    ..writeln('${_retanguloArredondado(x, y, largura, altura, 9)} B');

  final centroX = x + largura / 2;
  const larguraLogoUfs = 48.0;
  const larguraMarcaDsi = 64.0;
  const alturaLogos = 25.0;
  const espacoLogos = 9.0;
  final inicioLogos =
      centroX - (larguraLogoUfs + espacoLogos + larguraMarcaDsi) / 2;
  _desenharImagem(
    comandos,
    'LogoUfs',
    x: inicioLogos,
    y: y + altura - 30,
    largura: larguraLogoUfs,
    altura: alturaLogos,
    proporcao: 1974 / 1627,
  );
  _desenharImagem(
    comandos,
    'MarcaDsi',
    x: inicioLogos + larguraLogoUfs + espacoLogos,
    y: y + altura - 30,
    largura: larguraMarcaDsi,
    altura: alturaLogos,
    proporcao: 2454 / 1480,
  );
  _desenharTextoCentralizado(
    comandos,
    'CREDENCIAL DO PARTICIPANTE',
    centroX: centroX,
    y: y + altura - 39,
    tamanho: 5.8,
    fonte: 'F2',
    cor: _cor(102, 112, 133),
    espacamento: 0.25,
  );

  final payload = ParticipanteQrPayload(
    id: participante.id,
    nome: participante.nome,
  ).toRawJson();
  final codigo = QrCode.fromData(
    data: payload,
    errorCorrectLevel: QrErrorCorrectLevel.M,
  );
  final imagem = QrImage(codigo);
  const margemQr = 4;
  const tamanhoQr = 76.0;
  final totalModulos = imagem.moduleCount + margemQr * 2;
  final modulo = tamanhoQr / totalModulos;
  final qrX = x + (largura - tamanhoQr) / 2;
  final qrY = y + 14;

  comandos.writeln('0 g');
  for (var linha = 0; linha < imagem.moduleCount; linha++) {
    var inicio = -1;
    for (var coluna = 0; coluna <= imagem.moduleCount; coluna++) {
      final escuro =
          coluna < imagem.moduleCount && imagem.isDark(linha, coluna);
      if (escuro && inicio < 0) {
        inicio = coluna;
      } else if (!escuro && inicio >= 0) {
        final larguraFaixa = (coluna - inicio) * modulo;
        final moduloX = qrX + (margemQr + inicio) * modulo;
        final moduloY =
            qrY + (margemQr + imagem.moduleCount - 1 - linha) * modulo;
        comandos.writeln(
          '${_n(moduloX)} ${_n(moduloY)} ${_n(larguraFaixa)} ${_n(modulo)} re f',
        );
        inicio = -1;
      }
    }
  }

  const tamanhoNome = 9.4;
  final linhasNome = _quebrarNome(participante.nome, largura - 16, tamanhoNome);
  var textoY = y + (linhasNome.length == 1 ? 101 : 105);
  for (final linha in linhasNome) {
    _desenharTextoCentralizado(
      comandos,
      linha,
      centroX: centroX,
      y: textoY,
      tamanho: tamanhoNome,
      fonte: 'F2',
      cor: _cor(16, 24, 40),
    );
    textoY -= 10.5;
  }

  _desenharTextoCentralizado(
    comandos,
    'Apresente este QR Code na portaria',
    centroX: centroX,
    y: y + 4,
    tamanho: 5.4,
    fonte: 'F2',
    cor: _cor(71, 84, 103),
  );
}

void _desenharImagem(
  StringBuffer comandos,
  String recurso, {
  required double x,
  required double y,
  required double largura,
  required double altura,
  required double proporcao,
}) {
  var larguraFinal = largura;
  var alturaFinal = larguraFinal / proporcao;
  if (alturaFinal > altura) {
    alturaFinal = altura;
    larguraFinal = alturaFinal * proporcao;
  }
  final imagemX = x + (largura - larguraFinal) / 2;
  final imagemY = y + (altura - alturaFinal) / 2;
  comandos.writeln(
    'q ${_n(larguraFinal)} 0 0 ${_n(alturaFinal)} '
    '${_n(imagemX)} ${_n(imagemY)} cm /$recurso Do Q',
  );
}

List<String> _quebrarNome(String nome, double larguraMaxima, double tamanho) {
  final palavras = nome.trim().split(RegExp(r'\s+'));
  final linhas = <String>[];
  var indicePalavra = 0;

  while (indicePalavra < palavras.length && linhas.length < 2) {
    var linha = '';
    while (indicePalavra < palavras.length) {
      final candidata = linha.isEmpty
          ? palavras[indicePalavra]
          : '$linha ${palavras[indicePalavra]}';
      if (_larguraTexto(candidata, tamanho, negrito: true) <= larguraMaxima) {
        linha = candidata;
        indicePalavra++;
      } else {
        break;
      }
    }

    if (linha.isEmpty) {
      linha = _truncarTexto(
        palavras[indicePalavra],
        larguraMaxima,
        tamanho,
        comReticencias: true,
      );
      indicePalavra++;
    }
    linhas.add(linha);
  }

  if (indicePalavra < palavras.length) {
    linhas[1] = _truncarTexto(
      '${linhas[1]}...',
      larguraMaxima,
      tamanho,
      comReticencias: true,
    );
  }
  return linhas;
}

String _truncarTexto(
  String texto,
  double larguraMaxima,
  double tamanho, {
  required bool comReticencias,
}) {
  var base = texto.replaceFirst(RegExp(r'\.{3}$'), '');
  final sufixo = comReticencias ? '...' : '';
  while (base.isNotEmpty &&
      _larguraTexto('$base$sufixo', tamanho, negrito: true) > larguraMaxima) {
    base = base.substring(0, base.length - 1);
  }
  return '$base$sufixo';
}

void _desenharTextoCentralizado(
  StringBuffer comandos,
  String texto, {
  required double centroX,
  required double y,
  required double tamanho,
  required String fonte,
  required String cor,
  double espacamento = 0,
}) {
  final largura = _larguraTexto(
    texto,
    tamanho,
    negrito: fonte == 'F2',
    espacamento: espacamento,
  );
  _desenharTexto(
    comandos,
    texto,
    x: centroX - largura / 2,
    y: y,
    tamanho: tamanho,
    fonte: fonte,
    cor: cor,
    espacamento: espacamento,
  );
}

void _desenharTexto(
  StringBuffer comandos,
  String texto, {
  required double x,
  required double y,
  required double tamanho,
  required String fonte,
  required String cor,
  double espacamento = 0,
}) {
  comandos.writeln(
    '$cor rg BT /$fonte ${_n(tamanho)} Tf ${_n(espacamento)} Tc '
    '${_n(x)} ${_n(y)} Td ${_textoPdf(texto)} Tj ET',
  );
}

double _larguraTexto(
  String texto,
  double tamanho, {
  required bool negrito,
  double espacamento = 0,
}) {
  final larguraEmUnidades = texto.runes.fold<int>(
    0,
    (total, caractere) =>
        total + _largurasHelvetica[negrito]![_normalizarCaractere(caractere)]!,
  );
  return larguraEmUnidades * tamanho / 1000 +
      math.max(0, texto.runes.length - 1) * espacamento;
}

int _normalizarCaractere(int caractere) {
  const equivalencias = <int, int>{
    0x00c0: 0x41,
    0x00c1: 0x41,
    0x00c2: 0x41,
    0x00c3: 0x41,
    0x00c7: 0x43,
    0x00c8: 0x45,
    0x00c9: 0x45,
    0x00ca: 0x45,
    0x00cc: 0x49,
    0x00cd: 0x49,
    0x00d2: 0x4f,
    0x00d3: 0x4f,
    0x00d4: 0x4f,
    0x00d5: 0x4f,
    0x00d9: 0x55,
    0x00da: 0x55,
    0x00e0: 0x61,
    0x00e1: 0x61,
    0x00e2: 0x61,
    0x00e3: 0x61,
    0x00e7: 0x63,
    0x00e8: 0x65,
    0x00e9: 0x65,
    0x00ea: 0x65,
    0x00ec: 0x69,
    0x00ed: 0x69,
    0x00f2: 0x6f,
    0x00f3: 0x6f,
    0x00f4: 0x6f,
    0x00f5: 0x6f,
    0x00f9: 0x75,
    0x00fa: 0x75,
  };
  final normalizado = equivalencias[caractere] ?? caractere;
  return _largurasHelvetica[false]!.containsKey(normalizado)
      ? normalizado
      : 0x3f;
}

final Map<bool, Map<int, int>> _largurasHelvetica = <bool, Map<int, int>>{
  false: _criarLargurasHelvetica(negrito: false),
  true: _criarLargurasHelvetica(negrito: true),
};

Map<int, int> _criarLargurasHelvetica({required bool negrito}) {
  final larguras = <int, int>{
    0x20: 278,
    0x2c: 278,
    0x2d: negrito ? 333 : 333,
    0x2e: 278,
    0x2f: 278,
    0x3a: 278,
    0x3b: 278,
    0x3f: negrito ? 611 : 556,
  };
  for (var caractere = 0x30; caractere <= 0x39; caractere++) {
    larguras[caractere] = 556;
  }

  const maiusculasRegular = <int>[
    667,
    667,
    722,
    722,
    667,
    611,
    778,
    722,
    278,
    500,
    667,
    556,
    833,
    722,
    778,
    667,
    778,
    722,
    667,
    611,
    722,
    667,
    944,
    667,
    667,
    611,
  ];
  const maiusculasNegrito = <int>[
    722,
    722,
    722,
    722,
    667,
    611,
    778,
    722,
    278,
    556,
    722,
    611,
    833,
    722,
    778,
    667,
    778,
    722,
    667,
    611,
    722,
    667,
    944,
    667,
    667,
    611,
  ];
  const minusculasRegular = <int>[
    556,
    556,
    500,
    556,
    556,
    278,
    556,
    556,
    222,
    222,
    500,
    222,
    833,
    556,
    556,
    556,
    556,
    333,
    500,
    278,
    556,
    500,
    722,
    500,
    500,
    500,
  ];
  const minusculasNegrito = <int>[
    556,
    611,
    556,
    611,
    556,
    333,
    611,
    611,
    278,
    278,
    556,
    278,
    889,
    611,
    611,
    611,
    611,
    389,
    556,
    333,
    611,
    556,
    778,
    556,
    556,
    500,
  ];
  final maiusculas = negrito ? maiusculasNegrito : maiusculasRegular;
  final minusculas = negrito ? minusculasNegrito : minusculasRegular;
  for (var indice = 0; indice < 26; indice++) {
    larguras[0x41 + indice] = maiusculas[indice];
    larguras[0x61 + indice] = minusculas[indice];
  }
  return larguras;
}

String _retanguloArredondado(
  double x,
  double y,
  double largura,
  double altura,
  double raio,
) {
  const k = 0.5522847498;
  final controle = raio * k;
  return '${_n(x + raio)} ${_n(y)} m '
      '${_n(x + largura - raio)} ${_n(y)} l '
      '${_n(x + largura - raio + controle)} ${_n(y)} '
      '${_n(x + largura)} ${_n(y + raio - controle)} '
      '${_n(x + largura)} ${_n(y + raio)} c '
      '${_n(x + largura)} ${_n(y + altura - raio)} l '
      '${_n(x + largura)} ${_n(y + altura - raio + controle)} '
      '${_n(x + largura - raio + controle)} ${_n(y + altura)} '
      '${_n(x + largura - raio)} ${_n(y + altura)} c '
      '${_n(x + raio)} ${_n(y + altura)} l '
      '${_n(x + raio - controle)} ${_n(y + altura)} '
      '${_n(x)} ${_n(y + altura - raio + controle)} '
      '${_n(x)} ${_n(y + altura - raio)} c '
      '${_n(x)} ${_n(y + raio)} l '
      '${_n(x)} ${_n(y + raio - controle)} '
      '${_n(x + raio - controle)} ${_n(y)} '
      '${_n(x + raio)} ${_n(y)} c h';
}

String _cor(int vermelho, int verde, int azul) =>
    '${_n(vermelho / 255)} ${_n(verde / 255)} ${_n(azul / 255)}';

String _textoPdf(String texto) {
  final bytes = <int>[];
  for (final ponto in texto.runes) {
    bytes.add(ponto <= 0x7f || (ponto >= 0xa0 && ponto <= 0xff) ? ponto : 0x3f);
  }
  return '<${bytes.map((byte) => byte.toRadixString(16).padLeft(2, '0')).join()}>';
}

String _n(num valor) => valor.toStringAsFixed(3);

final class _ImagemPdf {
  const _ImagemPdf({
    required this.largura,
    required this.altura,
    required this.rgbComprimido,
    required this.alphaComprimido,
  });

  factory _ImagemPdf.dePng(Uint8List png) {
    const assinatura = <int>[137, 80, 78, 71, 13, 10, 26, 10];
    var assinaturaValida = png.length >= 33;
    for (var indice = 0; indice < 8 && assinaturaValida; indice++) {
      assinaturaValida = png[indice] == assinatura[indice];
    }
    if (!assinaturaValida) {
      throw const FormatException('Arquivo de imagem PNG inválido.');
    }

    var cursor = 8;
    var larguraOriginal = 0;
    var alturaOriginal = 0;
    var profundidade = 0;
    var tipoCor = 0;
    var entrelacado = 0;
    final idat = BytesBuilder(copy: false);

    while (cursor + 12 <= png.length) {
      final tamanho = _uint32(png, cursor);
      final tipo = ascii.decode(png.sublist(cursor + 4, cursor + 8));
      final inicioDados = cursor + 8;
      final fimDados = inicioDados + tamanho;
      if (fimDados + 4 > png.length) {
        throw const FormatException('Estrutura do PNG está incompleta.');
      }
      if (tipo == 'IHDR') {
        larguraOriginal = _uint32(png, inicioDados);
        alturaOriginal = _uint32(png, inicioDados + 4);
        profundidade = png[inicioDados + 8];
        tipoCor = png[inicioDados + 9];
        entrelacado = png[inicioDados + 12];
      } else if (tipo == 'IDAT') {
        idat.add(png.sublist(inicioDados, fimDados));
      } else if (tipo == 'IEND') {
        break;
      }
      cursor = fimDados + 4;
    }

    if (larguraOriginal <= 0 ||
        alturaOriginal <= 0 ||
        profundidade != 8 ||
        tipoCor != 6 ||
        entrelacado != 0 ||
        idat.length == 0) {
      throw const FormatException(
        'A imagem precisa ser um PNG RGBA de 8 bits sem entrelaçamento.',
      );
    }

    const bytesPorPixel = 4;
    final bytesPorLinha = larguraOriginal * bytesPorPixel;
    final filtrado = zlib.decode(idat.takeBytes());
    final tamanhoEsperado = (bytesPorLinha + 1) * alturaOriginal;
    if (filtrado.length != tamanhoEsperado) {
      throw const FormatException('Dados internos do PNG estão inválidos.');
    }

    final rgba = Uint8List(bytesPorLinha * alturaOriginal);
    for (var linha = 0; linha < alturaOriginal; linha++) {
      final inicioFiltrado = linha * (bytesPorLinha + 1);
      final filtro = filtrado[inicioFiltrado];
      if (filtro > 4) {
        throw const FormatException('Filtro PNG não suportado.');
      }
      for (var colunaByte = 0; colunaByte < bytesPorLinha; colunaByte++) {
        final destino = linha * bytesPorLinha + colunaByte;
        final esquerda = colunaByte >= bytesPorPixel
            ? rgba[destino - bytesPorPixel]
            : 0;
        final acima = linha > 0 ? rgba[destino - bytesPorLinha] : 0;
        final acimaEsquerda = linha > 0 && colunaByte >= bytesPorPixel
            ? rgba[destino - bytesPorLinha - bytesPorPixel]
            : 0;
        final previsor = switch (filtro) {
          0 => 0,
          1 => esquerda,
          2 => acima,
          3 => (esquerda + acima) ~/ 2,
          4 => _paeth(esquerda, acima, acimaEsquerda),
          _ => 0,
        };
        rgba[destino] =
            (filtrado[inicioFiltrado + 1 + colunaByte] + previsor) & 0xff;
      }
    }

    final escala = math.max(
      1,
      math.max((larguraOriginal / 500).ceil(), (alturaOriginal / 350).ceil()),
    );
    final largura = (larguraOriginal + escala - 1) ~/ escala;
    final altura = (alturaOriginal + escala - 1) ~/ escala;
    final rgb = Uint8List(largura * altura * 3);
    final alpha = Uint8List(largura * altura);
    var destinoRgb = 0;
    var destinoAlpha = 0;
    for (var y = 0; y < altura; y++) {
      final origemY = math.min(y * escala, alturaOriginal - 1);
      for (var x = 0; x < largura; x++) {
        final origemX = math.min(x * escala, larguraOriginal - 1);
        final origem = (origemY * larguraOriginal + origemX) * 4;
        rgb[destinoRgb++] = rgba[origem];
        rgb[destinoRgb++] = rgba[origem + 1];
        rgb[destinoRgb++] = rgba[origem + 2];
        alpha[destinoAlpha++] = rgba[origem + 3];
      }
    }

    return _ImagemPdf(
      largura: largura,
      altura: altura,
      rgbComprimido: Uint8List.fromList(zlib.encode(rgb)),
      alphaComprimido: Uint8List.fromList(zlib.encode(alpha)),
    );
  }

  final int largura;
  final int altura;
  final Uint8List rgbComprimido;
  final Uint8List alphaComprimido;

  static int _uint32(Uint8List bytes, int indice) =>
      bytes[indice] << 24 |
      bytes[indice + 1] << 16 |
      bytes[indice + 2] << 8 |
      bytes[indice + 3];

  static int _paeth(int esquerda, int acima, int acimaEsquerda) {
    final estimativa = esquerda + acima - acimaEsquerda;
    final distanciaEsquerda = (estimativa - esquerda).abs();
    final distanciaAcima = (estimativa - acima).abs();
    final distanciaDiagonal = (estimativa - acimaEsquerda).abs();
    if (distanciaEsquerda <= distanciaAcima &&
        distanciaEsquerda <= distanciaDiagonal) {
      return esquerda;
    }
    return distanciaAcima <= distanciaDiagonal ? acima : acimaEsquerda;
  }
}

final class _PdfSimples {
  const _PdfSimples(this.paginas, this.logoUfs, this.marcaDsi);

  final List<String> paginas;
  final _ImagemPdf logoUfs;
  final _ImagemPdf marcaDsi;

  Uint8List gerar() {
    final objetos = <int, List<int>>{};
    final idsPaginas = <int>[];
    for (var indice = 0; indice < paginas.length; indice++) {
      idsPaginas.add(9 + indice * 2);
    }

    objetos[1] = ascii.encode('<< /Type /Catalog /Pages 2 0 R >>');
    objetos[2] = ascii.encode(
      '<< /Type /Pages /Count ${paginas.length} /Kids [${idsPaginas.map((id) => '$id 0 R').join(' ')}] >>',
    );
    objetos[3] = ascii.encode(
      '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>',
    );
    objetos[4] = ascii.encode(
      '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>',
    );
    objetos[5] = _objetoImagem(logoUfs, alphaId: 6);
    objetos[6] = _objetoAlpha(logoUfs);
    objetos[7] = _objetoImagem(marcaDsi, alphaId: 8);
    objetos[8] = _objetoAlpha(marcaDsi);

    for (var indice = 0; indice < paginas.length; indice++) {
      final paginaId = idsPaginas[indice];
      final conteudoId = paginaId + 1;
      final conteudo = ascii.encode(paginas[indice]);
      objetos[paginaId] = ascii.encode(
        '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.28 841.89] '
        '/Resources << /Font << /F1 3 0 R /F2 4 0 R >> '
        '/XObject << /LogoUfs 5 0 R /MarcaDsi 7 0 R >> >> '
        '/Contents $conteudoId 0 R >>',
      );
      objetos[conteudoId] = <int>[
        ...ascii.encode('<< /Length ${conteudo.length} >>\nstream\n'),
        ...conteudo,
        ...ascii.encode('endstream'),
      ];
    }

    final bytes = BytesBuilder(copy: false)
      ..add(ascii.encode('%PDF-1.4\n'))
      ..add(<int>[0x25, 0xe2, 0xe3, 0xcf, 0xd3, 0x0a]);
    final offsets = <int>[0];
    for (var id = 1; id <= objetos.length; id++) {
      offsets.add(bytes.length);
      bytes
        ..add(ascii.encode('$id 0 obj\n'))
        ..add(objetos[id]!)
        ..add(ascii.encode('\nendobj\n'));
    }

    final inicioXref = bytes.length;
    bytes.add(ascii.encode('xref\n0 ${objetos.length + 1}\n'));
    bytes.add(ascii.encode('0000000000 65535 f \n'));
    for (final offset in offsets.skip(1)) {
      bytes.add(
        ascii.encode('${offset.toString().padLeft(10, '0')} 00000 n \n'),
      );
    }
    bytes.add(
      ascii.encode(
        'trailer\n<< /Size ${objetos.length + 1} /Root 1 0 R >>\n'
        'startxref\n$inicioXref\n%%EOF\n',
      ),
    );
    return bytes.takeBytes();
  }

  List<int> _objetoImagem(_ImagemPdf imagem, {required int alphaId}) =>
      _objetoStream(
        '<< /Type /XObject /Subtype /Image /Width ${imagem.largura} '
        '/Height ${imagem.altura} /ColorSpace /DeviceRGB '
        '/BitsPerComponent 8 /Filter /FlateDecode /Interpolate true '
        '/SMask $alphaId 0 R /Length ${imagem.rgbComprimido.length} >>',
        imagem.rgbComprimido,
      );

  List<int> _objetoAlpha(_ImagemPdf imagem) => _objetoStream(
    '<< /Type /XObject /Subtype /Image /Width ${imagem.largura} '
    '/Height ${imagem.altura} /ColorSpace /DeviceGray '
    '/BitsPerComponent 8 /Filter /FlateDecode '
    '/Length ${imagem.alphaComprimido.length} >>',
    imagem.alphaComprimido,
  );

  List<int> _objetoStream(String dicionario, Uint8List dados) => <int>[
    ...ascii.encode('$dicionario\nstream\n'),
    ...dados,
    ...ascii.encode('\nendstream'),
  ];
}
