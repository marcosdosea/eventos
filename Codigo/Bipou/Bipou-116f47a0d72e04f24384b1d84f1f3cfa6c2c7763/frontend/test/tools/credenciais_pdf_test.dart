import 'dart:convert';
import 'dart:io';

import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/tools/credenciais_pdf.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('gera PDF A4 com quinze credenciais por página', () {
    final participantes = List<ParticipanteResponse>.generate(
      16,
      (indice) => ParticipanteResponse(
        id: '550e8400-e29b-41d4-a716-${indice.toString().padLeft(12, '0')}',
        nome: 'Participante ${indice + 1}',
        cpf: '52998224725',
      ),
    );

    final pdf = gerarPdfCredenciais(
      participantes,
      logoUfsPng: File('logo-ufs-azul.png').readAsBytesSync(),
      marcaDsiPng: File('MarcaDSI2026.png').readAsBytesSync(),
    );
    final texto = latin1.decode(pdf, allowInvalid: true);

    expect(texto, startsWith('%PDF-1.4'));
    expect(credenciaisPorPagina, 15);
    expect(texto, contains('/MediaBox [0 0 595.28 841.89]'));
    expect(texto, contains('/BaseFont /Helvetica-Bold'));
    expect(texto, contains('/F2 4 0 R'));
    expect(texto, contains('/LogoUfs 5 0 R'));
    expect(texto, contains('/MarcaDsi 7 0 R'));
    expect(RegExp(r'/Subtype /Image').allMatches(texto), hasLength(4));
    expect(texto, isNot(contains('4249504f55'))); // BIPOU
    expect(RegExp(r'/Type /Page ').allMatches(texto), hasLength(2));
    expect(texto, contains('xref'));
    expect(texto, endsWith('%%EOF\n'));
  });
}
