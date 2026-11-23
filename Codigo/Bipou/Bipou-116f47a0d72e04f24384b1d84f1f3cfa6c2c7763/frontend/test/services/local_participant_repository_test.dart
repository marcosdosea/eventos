import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:bipou_frontend/models/borala_models.dart';
import 'package:bipou_frontend/models/local_participant.dart';
import 'package:bipou_frontend/models/participante_qr_payload.dart';
import 'package:bipou_frontend/models/portaria_session.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

void main() {
  sqfliteFfiInit();

  late SqliteLocalParticipantRepository repository;

  setUp(() {
    repository = SqliteLocalParticipantRepository(
      databaseFactory: databaseFactoryFfi,
      databasePath: inMemoryDatabasePath,
    );
  });

  tearDown(() async {
    await repository.close();
  });

  test(
    'armazena participantes no SQLite com hashValidacao e permite busca instantanea por id, idPessoa e CPF',
    () async {
      final maria = LocalParticipant.fromBorala(
        eventoId: 15,
        participante: BoralaParticipante(
          idPessoa: 1042,
          nome: 'Maria Santos',
          nomeCracha: 'Maria Santos',
          cpf: '123.456.789-01',
          subeventosInscritos: const <int>[42, 43],
        ),
      );
      final joao = LocalParticipant.fromBorala(
        eventoId: 15,
        participante: BoralaParticipante(
          idPessoa: 1043,
          nome: 'Joao Silva',
          cpf: '52998224725',
          subeventosInscritos: const <int>[43],
        ),
      );

      final inseridos = await repository.replaceAllForEvent(
        eventoId: 15,
        participants: <LocalParticipant>[maria, joao],
      );

      expect(inseridos, 2);
      expect(maria.hashValidacao, '0ec02205');
      expect(await repository.countByContext(eventoId: 15), 2);
      expect(await repository.countByContext(eventoId: 15, subeventoId: 42), 1);
      expect(await repository.countByContext(eventoId: 15, subeventoId: 43), 2);

      final porPessoa = await repository.findByIdPessoa(1042, eventoId: 15);
      expect(porPessoa?.nome, 'Maria Santos');
      expect(porPessoa?.cpf, '12345678901');
      expect(porPessoa?.subeventoIds, <int>[42, 43]);
      expect(porPessoa?.hashValidacao, '0ec02205');

      final porCpf = await repository.findByCpf('529.982.247-25', eventoId: 15);
      expect(porCpf?.idPessoa, 1043);
      expect(porCpf?.nome, 'Joao Silva');

      final porUuid = await repository.findById(maria.id, eventoId: 15);
      expect(porUuid?.idPessoa, 1042);

      final listaSub42 = await repository.listByContext(
        eventoId: 15,
        subeventoId: 42,
      );
      expect(listaSub42.map((p) => p.idPessoa), <int>[1042]);

      final payload = porPessoa!.toQrPayload();
      expect(payload.validarHash(), isTrue);
      expect(
        ParticipanteQrPayload.fromRawJson(payload.toRawJson()).pid,
        1042,
      );
    },
  );

  test(
    'substitui carga do evento sem afetar participantes de outros eventos',
    () async {
      await repository.replaceAllForEvent(
        eventoId: 10,
        participants: <LocalParticipant>[
          LocalParticipant.fromBorala(
            eventoId: 10,
            participante: BoralaParticipante(
              idPessoa: 1,
              nome: 'Evento 10 Antigo',
              cpf: '52998224725',
            ),
          ),
        ],
      );
      await repository.replaceAllForEvent(
        eventoId: 20,
        participants: <LocalParticipant>[
          LocalParticipant.fromBorala(
            eventoId: 20,
            participante: BoralaParticipante(
              idPessoa: 2,
              nome: 'Evento 20 Preservado',
              cpf: '11144477735',
            ),
          ),
        ],
      );

      await repository.replaceAllForEvent(
        eventoId: 10,
        participants: <LocalParticipant>[
          LocalParticipant.fromBorala(
            eventoId: 10,
            participante: BoralaParticipante(
              idPessoa: 3,
              nome: 'Evento 10 Novo',
              cpf: '52998224725',
            ),
          ),
        ],
      );

      expect(await repository.findByIdPessoa(1, eventoId: 10), isNull);
      expect(
        (await repository.findByIdPessoa(3, eventoId: 10))?.nome,
        'Evento 10 Novo',
      );
      expect(
        (await repository.findByIdPessoa(2, eventoId: 20))?.nome,
        'Evento 20 Preservado',
      );
    },
  );

  test(
    'persiste sessao ativa, catalogo em cache e ETag no banco SQLite',
    () async {
      final session = PortariaSession(
        eventoId: 15,
        eventoNome: 'Seminfo 2026',
        subeventoId: 42,
        subeventoNome: 'Flutter Avançado - Sala 102',
        totalParticipantesCarregados: 320,
        sincronizadoEm: DateTime.utc(2026, 11, 18, 12, 0),
        boralaBaseUrl: 'http://10.0.2.2:5000',
        operadorUsuario: 'colaborador@ufs.br',
      );

      await repository.saveSession(session);
      final loaded = await repository.loadSession();

      expect(loaded, isNotNull);
      expect(
        loaded!.contextoBanner,
        '[Evento: Seminfo 2026] » [Curso: Flutter Avançado - Sala 102]',
      );
      expect(
        loaded.resumoParticipantes,
        '320 participantes de Flutter Avançado - Sala 102 carregados localmente',
      );
      expect(loaded.boralaBaseUrl, 'http://10.0.2.2:5000');

      await repository.saveCachedCatalog(
        const CachedBoralaCatalog(
          eventos: <BoralaEvento>[
            BoralaEvento(idEvento: 15, nome: 'Seminfo 2026'),
          ],
          subeventosPorEvento: <int, List<BoralaSubevento>>{
            15: <BoralaSubevento>[
              BoralaSubevento(
                idSubEvento: 42,
                idEvento: 15,
                nome: 'Flutter Avançado - Sala 102',
              ),
            ],
          },
        ),
      );
      final catalog = await repository.loadCachedCatalog();
      expect(catalog?.eventos.single.nome, 'Seminfo 2026');
      expect(catalog?.subeventosPorEvento[15]?.single.idSubEvento, 42);

      await repository.saveEventEtag(eventoId: 15, etag: 'W/"abc123"');
      expect(await repository.loadEventEtag(15), 'W/"abc123"');

      await repository.clearSession();
      expect(await repository.loadSession(), isNull);
    },
  );

  test(
    'decodifica carga offline do Borala em JSON puro e compactado GZIP',
    () {
      const jsonText = '''
      {
        "versao": 1,
        "evento": { "idEvento": 15, "nome": "Seminfo 2026" },
        "subeventos": [
          { "idSubEvento": 42, "idEvento": 15, "nome": "Curso de Python" }
        ],
        "participantes": [
          {
            "idPessoa": 1042,
            "nome": "Maria Santos",
            "nomeCracha": "Maria Santos",
            "cpf": "12345678901",
            "statusInscricao": "A",
            "subeventosInscritos": [42]
          }
        ]
      }
      ''';

      final fromPlain = BoralaCargaOffline.fromRawBytes(
        Uint8List.fromList(utf8.encode(jsonText)),
      );
      expect(fromPlain.evento.nome, 'Seminfo 2026');
      expect(fromPlain.subeventos.single.nome, 'Curso de Python');
      expect(fromPlain.participantes.single.idPessoa, 1042);

      final gzBytes = Uint8List.fromList(gzip.encode(utf8.encode(jsonText)));
      final fromGzip = BoralaCargaOffline.fromRawBytes(gzBytes);
      expect(fromGzip.evento.idEvento, 15);
      expect(fromGzip.participantes.single.subeventosInscritos, <int>[42]);
    },
  );
}
