import 'dart:io';

import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:share_plus/share_plus.dart';

abstract interface class AuditLogExporter {
  Future<void> export();
}

typedef AuditFileShare = Future<void> Function(File file);

final class AuditLogExportService implements AuditLogExporter {
  AuditLogExportService({required this.auditLog, AuditFileShare? shareFile})
    : _shareFile = shareFile ?? _shareAuditFile;

  final AuditLogReader auditLog;
  final AuditFileShare _shareFile;

  @override
  Future<void> export() async {
    final file = await auditLog.findExistingFile();
    if (file == null || await file.length() == 0) {
      throw const AuditLogExportException(
        'Ainda nao existem registros de auditoria para exportar.',
      );
    }

    try {
      await _shareFile(file);
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        AuditLogExportException(
          'Nao foi possivel abrir o compartilhamento do log.',
          error,
        ),
        stackTrace,
      );
    }
  }
}

Future<void> _shareAuditFile(File file) async {
  await SharePlus.instance.share(
    ShareParams(
      files: <XFile>[XFile(file.path, mimeType: 'text/plain')],
      title: 'Exportar auditoria do Bipou',
      subject: 'Auditoria de credenciamento do Bipou',
    ),
  );
}

final class AuditLogExportException implements Exception {
  const AuditLogExportException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => message;
}
