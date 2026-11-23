import 'dart:io';

import 'package:bipou_frontend/models/frequencia_pendente.dart';
import 'package:bipou_frontend/models/tipo_acao.dart';
import 'package:bipou_frontend/services/frequencia_pendente_store.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:path/path.dart' as p;
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

void main() {
  sqfliteFfiInit();

  const participanteId = '6ba7b810-9dad-41d1-80b4-00c04fd430c8';
  late Directory tempDir;
  late String dbPath;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('bipou_freq_store_');
    dbPath = p.join(tempDir.path, 'frequencias_test.db');
  });

  tearDown(() async {
    if (tempDir.existsSync()) {
      await tempDir.delete(recursive: true);
    }
  });

  FrequenciaPendente criarFrequencia(
    int index, {
    DateTime? dataHoraLida,
    TipoAcao tipoAcao = TipoAcao.entrada,
  }) {
    final sufixo = index.toString().padLeft(4, '0');
    return FrequenciaPendente(
      leituraId: '550e8400-e29b-41d4-a716-44665544$sufixo',
      idPessoa: 1000 + index,
      participanteId: participanteId,
      participanteNome: 'Participante $index',
      idEvento: 15,
      idSubEvento: 42,
      tipoAcao: tipoAcao,
      dataHoraLida:
          dataHoraLida ??
          DateTime.utc(2026, 8, 22, 12, 0).add(Duration(seconds: index)),
      dispositivoId: 'portaria-01',
    );
  }

  test(
    'persiste transacionalmente no SQLite e preserva 100% apos reabrir o banco',
    () async {
      final storeAntesCrash = SqliteFrequenciaPendenteStore(
        databasePath: dbPath,
        databaseFactoryOverride: databaseFactoryFfi,
      );

      for (var i = 1; i <= 5; i++) {
        await storeAntesCrash.salvarTransacional(
          criarFrequencia(
            i,
            tipoAcao: i.isEven ? TipoAcao.saida : TipoAcao.entrada,
          ),
        );
      }

      expect(await storeAntesCrash.contarPendentes(), 5);
      await storeAntesCrash.close();

      // Simula reinicializacao do app / dispositivo reabrindo o mesmo arquivo SQLite.
      final storeAposReinicio = SqliteFrequenciaPendenteStore(
        databasePath: dbPath,
        databaseFactoryOverride: databaseFactoryFfi,
      );
      try {
        expect(await storeAposReinicio.contarPendentes(), 5);
        final recuperadas = await storeAposReinicio.listarParaEnvio(limit: 50);
        expect(recuperadas, hasLength(5));
        expect(
          recuperadas.first.leituraId,
          '550e8400-e29b-41d4-a716-446655440001',
        );
        expect(recuperadas.first.idPessoa, 1001);
        expect(recuperadas.first.idEvento, 15);
        expect(recuperadas.first.idSubEvento, 42);
        expect(recuperadas.first.tipoAcao, TipoAcao.entrada);
        expect(recuperadas.first.statusEnvio, StatusEnvioFrequencia.pendente);
        expect(recuperadas.first.tentativasEnvio, 0);
        expect(recuperadas[1].tipoAcao, TipoAcao.saida);
      } finally {
        await storeAposReinicio.close();
      }
    },
  );

  test(
    'garante idempotencia ao inserir o mesmo leituraId mais de uma vez',
    () async {
      final store = SqliteFrequenciaPendenteStore(
        databasePath: dbPath,
        databaseFactoryOverride: databaseFactoryFfi,
      );
      try {
        final leitura = criarFrequencia(1);
        await store.salvarTransacional(leitura);
        await store.salvarTransacional(leitura);

        expect(await store.contarPendentes(), 1);
        expect(await store.listarTodas(), hasLength(1));
      } finally {
        await store.close();
      }
    },
  );

  test(
    'respeita o limite de 50 registros por lote ordenados cronologicamente',
    () async {
      final store = SqliteFrequenciaPendenteStore(
        databasePath: dbPath,
        databaseFactoryOverride: databaseFactoryFfi,
      );
      try {
        for (var i = 65; i >= 1; i--) {
          await store.salvarTransacional(criarFrequencia(i));
        }

        final lote = await store.listarParaEnvio(limit: 50);
        expect(lote, hasLength(50));
        expect(lote.first.leituraId, '550e8400-e29b-41d4-a716-446655440001');
        expect(lote.last.leituraId, '550e8400-e29b-41d4-a716-446655440050');
      } finally {
        await store.close();
      }
    },
  );

  test(
    'atualiza status para ENVIADO, ERRO e incrementa tentativasEnvio',
    () async {
      final store = SqliteFrequenciaPendenteStore(
        databasePath: dbPath,
        databaseFactoryOverride: databaseFactoryFfi,
      );
      try {
        final f1 = criarFrequencia(1);
        final f2 = criarFrequencia(2);
        final f3 = criarFrequencia(3);
        await store.salvarTransacional(f1);
        await store.salvarTransacional(f2);
        await store.salvarTransacional(f3);

        await store.registrarTentativaFalhaRede(<String>[f1.leituraId]);
        final f1AposRede = await store.buscarPorLeituraId(f1.leituraId);
        expect(f1AposRede?.statusEnvio, StatusEnvioFrequencia.pendente);
        expect(f1AposRede?.tentativasEnvio, 1);
        expect(await store.contarPendentes(), 3);

        await store.marcarEnviadosEmLote(<String>[f1.leituraId]);
        final f1Enviada = await store.buscarPorLeituraId(f1.leituraId);
        expect(f1Enviada?.statusEnvio, StatusEnvioFrequencia.enviado);
        expect(f1Enviada?.tentativasEnvio, 2);
        expect(await store.contarPendentes(), 2);

        await store.marcarErroEmLote(<String, String>{
          f2.leituraId: 'Inscricao nao localizada',
        });
        final f2Erro = await store.buscarPorLeituraId(f2.leituraId);
        expect(f2Erro?.statusEnvio, StatusEnvioFrequencia.erro);
        expect(f2Erro?.tentativasEnvio, 1);
        expect(f2Erro?.ultimoErro, 'Inscricao nao localizada');
        expect(await store.contarPendentes(), 1);

        final restantes = await store.listarParaEnvio(limit: 50);
        expect(restantes.single.leituraId, f3.leituraId);
      } finally {
        await store.close();
      }
    },
  );
}
