import 'dart:io';

import 'package:bipou_frontend/services/audit_log_export_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('compartilha o arquivo de auditoria existente', () async {
    final directory = await Directory.systemTemp.createTemp('bipou_export_');
    addTearDown(() => directory.delete(recursive: true));
    final file = File('${directory.path}/auditoria_evento.txt');
    await file.writeAsString('registro de auditoria');
    File? sharedFile;
    final exporter = AuditLogExportService(
      auditLog: _AuditLogReaderFake(file),
      shareFile: (file) async => sharedFile = file,
    );

    await exporter.export();

    expect(sharedFile, file);
  });

  test('nao abre compartilhamento sem registros', () async {
    var shared = false;
    final exporter = AuditLogExportService(
      auditLog: _AuditLogReaderFake(null),
      shareFile: (_) async => shared = true,
    );

    expect(
      exporter.export,
      throwsA(
        isA<AuditLogExportException>().having(
          (error) => error.message,
          'message',
          contains('Ainda nao existem registros'),
        ),
      ),
    );
    expect(shared, isFalse);
  });
}

final class _AuditLogReaderFake implements AuditLogReader {
  const _AuditLogReaderFake(this.file);

  final File? file;

  @override
  Future<File?> findExistingFile() async => file;
}
