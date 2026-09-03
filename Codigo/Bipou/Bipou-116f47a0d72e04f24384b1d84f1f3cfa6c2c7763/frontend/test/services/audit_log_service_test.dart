import 'dart:io';

import 'package:bipou_frontend/models/audit_log_entry.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('adiciona linhas sem sobrescrever os registros anteriores', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_audit_');
    addTearDown(() => directory.delete(recursive: true));

    final service = AuditLogService(
      documentsDirectoryProvider: () async => directory,
    );
    final firstDate = DateTime.utc(2026, 8, 22, 12, 30);
    final secondDate = DateTime.utc(2026, 8, 22, 12, 31);

    await Future.wait(<Future<File>>[
      service.append(
        AuditLogEntry(
          dataHora: firstDate,
          participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
          participanteNome: 'Participante Teste',
          tipoAcao: 'ENTRADA',
          dispositivoId: 'portaria-01',
        ),
      ),
      service.append(
        AuditLogEntry(
          dataHora: secondDate,
          participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
          participanteNome: 'Participante Teste',
          tipoAcao: 'SAIDA',
          dispositivoId: 'portaria-01',
        ),
      ),
    ]);

    final file = File(
      '${directory.path}${Platform.pathSeparator}${AuditLogService.fileName}',
    );
    expect(await file.readAsLines(), <String>[
      '2026-08-22T12:30:00.000Z;6ba7b810-9dad-41d1-80b4-00c04fd430c8;'
          'Participante Teste;ENTRADA;portaria-01',
      '2026-08-22T12:31:00.000Z;6ba7b810-9dad-41d1-80b4-00c04fd430c8;'
          'Participante Teste;SAIDA;portaria-01',
    ]);
    expect((await service.findExistingFile())?.path, file.path);
  });

  test('informa quando o arquivo ainda nao existe', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_audit_');
    addTearDown(() => directory.delete(recursive: true));

    final service = AuditLogService(
      documentsDirectoryProvider: () async => directory,
    );

    expect(await service.findExistingFile(), isNull);
  });

  test('escapa delimitadores no nome sem quebrar a linha de auditoria', () {
    final entry = AuditLogEntry(
      dataHora: DateTime.utc(2026, 8, 22, 12, 30),
      participanteId: '6ba7b810-9dad-41d1-80b4-00c04fd430c8',
      participanteNome: 'Nome;Teste\nLinha',
      tipoAcao: 'ENTRADA',
      dispositivoId: 'portaria-01',
    );

    expect(entry.toLine(), contains(';Nome%3BTeste%0ALinha;ENTRADA;'));
  });
}
