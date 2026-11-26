import 'dart:convert';

import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:path/path.dart' as p;
import 'package:sqflite/sqflite.dart';

final class CachedBoralaCatalog {
  const CachedBoralaCatalog({
    required this.eventos,
    required this.subeventosPorEvento,
  });

  factory CachedBoralaCatalog.fromJson(Map<String, dynamic> json) {
    final rawEventos = json['eventos'];
    final eventos = <BoralaEvento>[];
    if (rawEventos is List) {
      for (final item in rawEventos) {
        if (item is Map) {
          eventos.add(BoralaEvento.fromJson(Map<String, dynamic>.from(item)));
        }
      }
    }

    final subeventosPorEvento = <int, List<BoralaSubevento>>{};
    final rawSubs = json['subeventosPorEvento'];
    if (rawSubs is Map) {
      rawSubs.forEach((key, value) {
        final eventoId = int.tryParse(key.toString());
        if (eventoId == null || eventoId <= 0 || value is! List) {
          return;
        }
        final lista = <BoralaSubevento>[];
        for (final item in value) {
          if (item is Map) {
            lista.add(
              BoralaSubevento.fromJson(Map<String, dynamic>.from(item)),
            );
          }
        }
        subeventosPorEvento[eventoId] = List<BoralaSubevento>.unmodifiable(
          lista,
        );
      });
    }

    return CachedBoralaCatalog(
      eventos: List<BoralaEvento>.unmodifiable(eventos),
      subeventosPorEvento: Map<int, List<BoralaSubevento>>.unmodifiable(
        subeventosPorEvento,
      ),
    );
  }

  final List<BoralaEvento> eventos;
  final Map<int, List<BoralaSubevento>> subeventosPorEvento;

  Map<String, Object?> toJson() => <String, Object?>{
    'eventos': eventos.map((e) => e.toJson()).toList(),
    'subeventosPorEvento': subeventosPorEvento.map(
      (key, value) => MapEntry<String, Object?>(
        key.toString(),
        value.map((s) => s.toJson()).toList(),
      ),
    ),
  };
}

abstract interface class LocalParticipantLookup {
  Future<LocalParticipant?> findById(String id, {int? eventoId});

  Future<LocalParticipant?> findByIdPessoa(
    int idPessoa, {
    required int eventoId,
  });

  Future<LocalParticipant?> findByCpf(String cpf, {required int eventoId});

  Future<List<LocalParticipant>> listByContext({
    required int eventoId,
    int? subeventoId,
  });

  Future<int> countByContext({required int eventoId, int? subeventoId});
}

abstract interface class LocalParticipantRepository
    implements LocalParticipantLookup {
  Future<int> replaceAllForEvent({
    required int eventoId,
    required List<LocalParticipant> participants,
  });

  Future<void> saveSession(PortariaSession session);

  Future<PortariaSession?> loadSession();

  Future<void> clearSession();

  Future<void> saveCachedCatalog(CachedBoralaCatalog catalog);

  Future<CachedBoralaCatalog?> loadCachedCatalog();

  Future<void> saveEventEtag({required int eventoId, String? etag});

  Future<String?> loadEventEtag(int eventoId);

  Future<void> close();
}

final class SqliteLocalParticipantRepository
    implements LocalParticipantRepository {
  SqliteLocalParticipantRepository({
    this._databaseFactory,
    this._databasePath,
  });

  static const String defaultDatabaseFileName = 'bipou_local_cache.db';
  static const int _databaseVersion = 2;
  static const String _participantsTable = 'local_participants';
  static const String _stateTable = 'portaria_state';
  static const String _activeSessionKey = 'active_session';
  static const String _cachedCatalogKey = 'cached_catalog';

  final DatabaseFactory? _databaseFactory;
  final String? _databasePath;
  Database? _db;

  Future<Database> _openDatabase() async {
    final existing = _db;
    if (existing != null && existing.isOpen) {
      return existing;
    }

    final factory = _databaseFactory ?? databaseFactory;
    final path =
        _databasePath ??
        p.join(await factory.getDatabasesPath(), defaultDatabaseFileName);

    final db = await factory.openDatabase(
      path,
      options: OpenDatabaseOptions(
        version: _databaseVersion,
        onCreate: (db, version) async {
          await db.execute('''
            CREATE TABLE $_participantsTable (
              id TEXT NOT NULL,
              id_pessoa INTEGER NOT NULL,
              nome TEXT NOT NULL,
              nome_cracha TEXT,
              cpf TEXT NOT NULL,
              evento_id INTEGER NOT NULL,
              subevento_ids TEXT NOT NULL,
              hash_validacao TEXT NOT NULL,
              status_inscricao TEXT NOT NULL DEFAULT 'A',
              ultima_acao TEXT,
              PRIMARY KEY (evento_id, id_pessoa)
            )
          ''');
          await db.execute(
            'CREATE INDEX idx_local_participants_id '
            'ON $_participantsTable (id)',
          );
          await db.execute(
            'CREATE INDEX idx_local_participants_evento_cpf '
            'ON $_participantsTable (evento_id, cpf)',
          );
          await db.execute('''
            CREATE TABLE $_stateTable (
              key TEXT PRIMARY KEY,
              value TEXT NOT NULL
            )
          ''');
        },
        onUpgrade: (db, oldVersion, newVersion) async {
          if (oldVersion < 2) {
            await db.execute(
              'ALTER TABLE $_participantsTable ADD COLUMN ultima_acao TEXT',
            );
          }
        },
      ),
    );
    _db = db;
    return db;
  }

  @override
  Future<int> replaceAllForEvent({
    required int eventoId,
    required List<LocalParticipant> participants,
  }) async {
    final db = await _openDatabase();
    await db.transaction((txn) async {
      await txn.delete(
        _participantsTable,
        where: 'evento_id = ?',
        whereArgs: <Object>[eventoId],
      );

      final batch = txn.batch();
      for (final participant in participants) {
        batch.insert(
          _participantsTable,
          participant.toMap(),
          conflictAlgorithm: ConflictAlgorithm.replace,
        );
      }
      await batch.commit(noResult: true);
    });
    return participants.length;
  }

  @override
  Future<LocalParticipant?> findById(String id, {int? eventoId}) async {
    final normalized = id.trim();
    if (normalized.isEmpty) {
      return null;
    }

    final db = await _openDatabase();
    final rows = eventoId != null
        ? await db.query(
            _participantsTable,
            where: 'id = ? AND evento_id = ?',
            whereArgs: <Object>[normalized, eventoId],
            limit: 1,
          )
        : await db.query(
            _participantsTable,
            where: 'id = ?',
            whereArgs: <Object>[normalized],
            limit: 1,
          );
    if (rows.isEmpty) {
      return null;
    }
    return LocalParticipant.fromMap(rows.first);
  }

  @override
  Future<LocalParticipant?> findByIdPessoa(
    int idPessoa, {
    required int eventoId,
  }) async {
    if (idPessoa <= 0 || eventoId <= 0) {
      return null;
    }

    final db = await _openDatabase();
    final rows = await db.query(
      _participantsTable,
      where: 'evento_id = ? AND id_pessoa = ?',
      whereArgs: <Object>[eventoId, idPessoa],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }
    return LocalParticipant.fromMap(rows.first);
  }

  @override
  Future<LocalParticipant?> findByCpf(
    String cpf, {
    required int eventoId,
  }) async {
    if (eventoId <= 0) {
      return null;
    }
    final cpfLimpo = ParticipanteQrPayload.sanitizarCpf(cpf);
    if (cpfLimpo == '00000000000') {
      return null;
    }

    final db = await _openDatabase();
    final rows = await db.query(
      _participantsTable,
      where: 'evento_id = ? AND cpf = ?',
      whereArgs: <Object>[eventoId, cpfLimpo],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }
    return LocalParticipant.fromMap(rows.first);
  }

  @override
  Future<List<LocalParticipant>> listByContext({
    required int eventoId,
    int? subeventoId,
  }) async {
    if (eventoId <= 0) {
      return const <LocalParticipant>[];
    }

    final db = await _openDatabase();
    final rows = await db.query(
      _participantsTable,
      where: 'evento_id = ?',
      whereArgs: <Object>[eventoId],
      orderBy: 'LOWER(nome) ASC',
    );
    final participants = rows.map(LocalParticipant.fromMap).toList();
    if (subeventoId == null) {
      return participants;
    }
    return participants
        .where((participant) => participant.inscritoNoSubevento(subeventoId))
        .toList();
  }

  @override
  Future<int> countByContext({required int eventoId, int? subeventoId}) async {
    if (eventoId <= 0) {
      return 0;
    }

    if (subeventoId == null) {
      final db = await _openDatabase();
      final result = await db.rawQuery(
        'SELECT COUNT(*) AS total FROM $_participantsTable WHERE evento_id = ?',
        <Object>[eventoId],
      );
      return (result.first['total'] as num?)?.toInt() ?? 0;
    }

    final filtered = await listByContext(
      eventoId: eventoId,
      subeventoId: subeventoId,
    );
    return filtered.length;
  }

  @override
  Future<void> saveSession(PortariaSession session) async {
    final db = await _openDatabase();
    await db.insert(
      _stateTable,
      <String, Object?>{
        'key': _activeSessionKey,
        'value': jsonEncode(session.toJson()),
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<PortariaSession?> loadSession() async {
    final db = await _openDatabase();
    final rows = await db.query(
      _stateTable,
      where: 'key = ?',
      whereArgs: <Object>[_activeSessionKey],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }

    try {
      final decoded = jsonDecode(rows.first['value']! as String);
      if (decoded is! Map) {
        return null;
      }
      return PortariaSession.fromJson(Map<String, dynamic>.from(decoded));
    } on Object {
      return null;
    }
  }

  @override
  Future<void> clearSession() async {
    final db = await _openDatabase();
    await db.delete(
      _stateTable,
      where: 'key = ?',
      whereArgs: <Object>[_activeSessionKey],
    );
  }

  @override
  Future<void> saveCachedCatalog(CachedBoralaCatalog catalog) async {
    final db = await _openDatabase();
    await db.insert(
      _stateTable,
      <String, Object?>{
        'key': _cachedCatalogKey,
        'value': jsonEncode(catalog.toJson()),
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<CachedBoralaCatalog?> loadCachedCatalog() async {
    final db = await _openDatabase();
    final rows = await db.query(
      _stateTable,
      where: 'key = ?',
      whereArgs: <Object>[_cachedCatalogKey],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }

    try {
      final decoded = jsonDecode(rows.first['value']! as String);
      if (decoded is! Map) {
        return null;
      }
      return CachedBoralaCatalog.fromJson(Map<String, dynamic>.from(decoded));
    } on Object {
      return null;
    }
  }

  @override
  Future<void> saveEventEtag({required int eventoId, String? etag}) async {
    final db = await _openDatabase();
    final key = 'etag_evento_$eventoId';
    if (etag == null || etag.trim().isEmpty) {
      await db.delete(_stateTable, where: 'key = ?', whereArgs: <Object>[key]);
      return;
    }
    await db.insert(
      _stateTable,
      <String, Object?>{'key': key, 'value': etag.trim()},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<String?> loadEventEtag(int eventoId) async {
    final db = await _openDatabase();
    final rows = await db.query(
      _stateTable,
      where: 'key = ?',
      whereArgs: <Object>['etag_evento_$eventoId'],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }
    return rows.first['value'] as String?;
  }

  @override
  Future<void> close() async {
    final db = _db;
    _db = null;
    if (db != null && db.isOpen) {
      await db.close();
    }
  }
}
