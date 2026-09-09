import 'package:bipou_frontend/tools/lote_csv.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('lerParticipantesCsv', () {
    test('lê CSV separado por ponto e vírgula e normaliza CPF', () {
      final participantes = lerParticipantesCsv('''
nome;cpf
Maria da Silva;529.982.247-25
"João, dos Santos";11144477735
''');

      expect(participantes, hasLength(2));
      expect(participantes.first.nome, 'Maria da Silva');
      expect(participantes.first.cpf, '52998224725');
      expect(participantes.last.nome, 'João, dos Santos');
    });

    test('junta nome e sobrenome antes da importação', () {
      final participantes = lerParticipantesCsv('''
cpf,nome,sobrenome
529.982.247-25,Maria,da Silva
11144477735,João,dos Santos
''');

      expect(participantes, hasLength(2));
      expect(participantes.first.nome, 'Maria da Silva');
      expect(participantes.first.cpf, '52998224725');
      expect(participantes.last.nome, 'João dos Santos');
    });

    test('rejeita sobrenome vazio quando a coluna está presente', () {
      expect(
        () => lerParticipantesCsv('cpf,nome,sobrenome\n52998224725,Maria,'),
        throwsA(
          isA<FormatException>().having(
            (erro) => erro.message,
            'message',
            contains('sobrenome é obrigatório'),
          ),
        ),
      );
    });

    test('rejeita CPF repetido antes do envio', () {
      expect(
        () => lerParticipantesCsv('''
nome,cpf
Pessoa Um,52998224725
Pessoa Dois,529.982.247-25
'''),
        throwsA(
          isA<FormatException>().having(
            (erro) => erro.message,
            'message',
            contains('repetido'),
          ),
        ),
      );
    });

    test('rejeita arquivo sem cabeçalho esperado', () {
      expect(
        () => lerParticipantesCsv('pessoa;documento\nMaria;52998224725'),
        throwsFormatException,
      );
    });

    test('rejeita CPF com dígitos verificadores inválidos', () {
      expect(
        () => lerParticipantesCsv('nome;cpf\nMaria;12345678900'),
        throwsA(
          isA<FormatException>().having(
            (erro) => erro.message,
            'message',
            contains('CPF informado é inválido'),
          ),
        ),
      );
    });
  });
}
