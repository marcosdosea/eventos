import 'dart:io';

import 'package:bipou_frontend/models/cadastro_pendente.dart';
import 'package:bipou_frontend/services/cadastro_pendente_store.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const primeiroId = '550e8400-e29b-41d4-a716-446655440000';
  const segundoId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';
  final criadoEm = DateTime.utc(2026, 8, 25, 12);

  test('persiste concorrente e recupera fila depois de reiniciar', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_pending_');
    addTearDown(() => directory.delete(recursive: true));
    final store = CadastroPendenteFileStore(
      documentsDirectoryProvider: () async => directory,
    );

    await Future.wait(<Future<void>>[
      store.salvar(
        CadastroPendente(
          cadastroId: primeiroId,
          nome: 'Participante Um',
          cpf: '52998224725',
          dispositivoId: 'portaria-01',
          criadoEm: criadoEm,
        ),
      ),
      store.salvar(
        CadastroPendente(
          cadastroId: segundoId,
          nome: 'Participante Dois',
          cpf: '11144477735',
          dispositivoId: 'portaria-01',
          criadoEm: criadoEm.add(const Duration(minutes: 1)),
        ),
      ),
    ]);

    final depoisDoReinicio = CadastroPendenteFileStore(
      documentsDirectoryProvider: () async => directory,
    );
    final recuperados = await depoisDoReinicio.listar();

    expect(recuperados, hasLength(2));
    expect(
      recuperados.map((item) => item.cadastroId),
      containsAll(<String>[primeiroId, segundoId]),
    );

    await depoisDoReinicio.remover(primeiroId);
    final terceiraInstancia = CadastroPendenteFileStore(
      documentsDirectoryProvider: () async => directory,
    );
    expect((await terceiraInstancia.listar()).single.cadastroId, segundoId);
  });

  test('nao duplica o mesmo cadastro idempotente', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_pending_');
    addTearDown(() => directory.delete(recursive: true));
    final store = CadastroPendenteFileStore(
      documentsDirectoryProvider: () async => directory,
    );
    final cadastro = CadastroPendente(
      cadastroId: primeiroId,
      nome: 'Participante Um',
      cpf: '52998224725',
      dispositivoId: 'portaria-01',
      criadoEm: criadoEm,
    );

    await store.salvar(cadastro);
    await store.salvar(cadastro);

    expect(await store.listar(), hasLength(1));
  });

  test('recusa arquivo corrompido sem apagar os dados', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_pending_');
    addTearDown(() => directory.delete(recursive: true));
    final file = File(
      '${directory.path}${Platform.pathSeparator}'
      '${CadastroPendenteFileStore.fileName}',
    );
    await file.writeAsString('{arquivo-invalido', flush: true);
    final store = CadastroPendenteFileStore(
      documentsDirectoryProvider: () async => directory,
    );

    await expectLater(
      store.listar(),
      throwsA(isA<CadastroPendenteStoreException>()),
    );
    expect(await file.readAsString(), '{arquivo-invalido');
  });
}
