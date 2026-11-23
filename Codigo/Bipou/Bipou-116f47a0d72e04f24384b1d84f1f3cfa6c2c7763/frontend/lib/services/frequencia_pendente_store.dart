import 'package:bipou_frontend/models/frequencia_pendente.dart';
import 'package:bipou_frontend/services/frequencia_pendente_store_contract.dart';
import 'package:path/path.dart' as p;
import 'package:sqflite/sqflite.dart';

export 'package:bipou_frontend/services/frequencia_pendente_store_contract.dart';

final class SqliteFrequenciaPendenteStore implements FrequenciaPendenteStore {
  SqliteFrequenciaPendenteStore({
    String? databasePath,
    DatabaseFactory? databaseFactoryOverride,
  }) : _databasePath = databasePath,
       _databaseFactoryOverride = databaseFactoryOverride;

  static const String databaseName = 'bipou_frequencias_offline.db';
  static const String tableName = 'frequencias_pendentes';
  static const int _databaseVersion = 1;

  final String? _databasePath;
  final DatabaseFactory? _databaseFactoryOverride;

  Database? _database;
  Future<Database>? _opening;

  Future<Database> _getDatabase() async {
    final current = _database;
    if (current != null && current.isOpen) {
      return current;
    }

    final pendingOpen = _opening;
    if (pendingOpen != null) {
      return pendingOpen;
    }

    final openFuture = _openDatabase();
    _opening = openFuture;
    try {
      final db = await openFuture;
      _database = db;
      return db;
    } finally {
      if (identical(_opening, openFuture)) {
        _opening = null;
      }
    }
  }

  Future<Database> _openDatabase() async {
    try {
      final factory = _databaseFactoryOverride ?? databaseFactory;
      final dbPath =
          _databasePath ??
          p.join(await factory.getDatabasesPath(), databaseName);

      return await factory.openDatabase(
        dbPath,
        options: OpenDatabaseOptions(
          version: _databaseVersion,
          onCreate: (db, version) async {
            await db.execute('''
              CREATE TABLE $tableName (
                leitura_id TEXT PRIMARY KEY,
                id_pessoa INTEGER NOT NULL,
                participante_id TEXT NOT NULL,
                participante_nome TEXT NOT NULL,
                id_evento INTEGER NOT NULL,
                id_sub_evento INTEGER,
                tipo_acao TEXT NOT NULL,
                data_hora_lida TEXT NOT NULL,
                dispositivo_id TEXT NOT NULL,
                status_envio TEXT NOT NULL,
                tentativas_envio INTEGER NOT NULL DEFAULT 0,
                ultimo_erro TEXT
              )
            ''');
            await db.execute('''
              CREATE INDEX idx_frequencias_status_data
              ON $tableName (status_envio, data_hora_lida)
            ''');
          },
        ),
      );
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao abrir banco local de frequencias offline.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<void> salvarTransacional(FrequenciaPendente frequencia) async {
    try {
      final db = await _getDatabase();
      await db.transaction((txn) async {
        await txn.insert(
          tableName,
          frequencia.toMap(),
          conflictAlgorithm: ConflictAlgorithm.ignore,
        );
      });
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao persistir frequencia na fila offline.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<List<FrequenciaPendente>> listarParaEnvio({int limit = 50}) async {
    if (limit <= 0) {
      return const <FrequenciaPendente>[];
    }

    try {
      final db = await _getDatabase();
      final rows = await db.query(
        tableName,
        where: 'status_envio = ?',
        whereArgs: <Object?>[StatusEnvioFrequencia.pendente.apiValue],
        orderBy: 'data_hora_lida ASC, leitura_id ASC',
        limit: limit,
      );
      return rows.map(FrequenciaPendente.fromMap).toList(growable: false);
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao consultar fila offline de frequencias.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<List<FrequenciaPendente>> listarTodas() async {
    try {
      final db = await _getDatabase();
      final rows = await db.query(
        tableName,
        orderBy: 'data_hora_lida ASC, leitura_id ASC',
      );
      return rows.map(FrequenciaPendente.fromMap).toList(growable: false);
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao listar frequencias armazenadas.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<FrequenciaPendente?> buscarPorLeituraId(String leituraId) async {
    try {
      final db = await _getDatabase();
      final rows = await db.query(
        tableName,
        where: 'leitura_id = ?',
        whereArgs: <Object?>[leituraId],
        limit: 1,
      );
      if (rows.isEmpty) {
        return null;
      }
      return FrequenciaPendente.fromMap(rows.first);
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao consultar frequencia por leituraId.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<int> contarPendentes() async {
    try {
      final db = await _getDatabase();
      final rows = await db.rawQuery(
        'SELECT COUNT(*) FROM $tableName WHERE status_envio = ?',
        <Object?>[StatusEnvioFrequencia.pendente.apiValue],
      );
      return Sqflite.firstIntValue(rows) ?? 0;
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao contar frequencias pendentes.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<void> marcarEnviadosEmLote(List<String> leituraIds) async {
    if (leituraIds.isEmpty) {
      return;
    }

    try {
      final db = await _getDatabase();
      await db.transaction((txn) async {
        final batch = txn.batch();
        for (final leituraId in leituraIds) {
          batch.rawUpdate(
            'UPDATE $tableName '
            'SET status_envio = ?, '
            'tentativas_envio = tentativas_envio + 1, '
            'ultimo_erro = NULL '
            'WHERE leitura_id = ?',
            <Object?>[StatusEnvioFrequencia.enviado.apiValue, leituraId],
          );
        }
        await batch.commit(noResult: true);
      });
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao marcar frequencias como enviadas.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<void> marcarErroEmLote(Map<String, String> errosPorLeituraId) async {
    if (errosPorLeituraId.isEmpty) {
      return;
    }

    try {
      final db = await _getDatabase();
      await db.transaction((txn) async {
        final batch = txn.batch();
        for (final entry in errosPorLeituraId.entries) {
          batch.rawUpdate(
            'UPDATE $tableName '
            'SET status_envio = ?, '
            'tentativas_envio = tentativas_envio + 1, '
            'ultimo_erro = ? '
            'WHERE leitura_id = ?',
            <Object?>[
              StatusEnvioFrequencia.erro.apiValue,
              entry.value,
              entry.key,
            ],
          );
        }
        await batch.commit(noResult: true);
      });
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao marcar frequencias com erro.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<void> registrarTentativaFalhaRede(List<String> leituraIds) async {
    if (leituraIds.isEmpty) {
      return;
    }

    try {
      final db = await _getDatabase();
      await db.transaction((txn) async {
        final batch = txn.batch();
        for (final leituraId in leituraIds) {
          batch.rawUpdate(
            'UPDATE $tableName '
            'SET status_envio = ?, '
            'tentativas_envio = tentativas_envio + 1 '
            'WHERE leitura_id = ? AND status_envio != ?',
            <Object?>[
              StatusEnvioFrequencia.pendente.apiValue,
              leituraId,
              StatusEnvioFrequencia.enviado.apiValue,
            ],
          );
        }
        await batch.commit(noResult: true);
      });
    } on FrequenciaPendenteStoreException {
      rethrow;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        FrequenciaPendenteStoreException(
          'Falha ao registrar tentativa de reenvio na fila offline.',
          error,
        ),
        stackTrace,
      );
    }
  }

  @override
  Future<void> close() async {
    final current = _database;
    _database = null;
    if (current != null && current.isOpen) {
      await current.close();
    }
  }
}
