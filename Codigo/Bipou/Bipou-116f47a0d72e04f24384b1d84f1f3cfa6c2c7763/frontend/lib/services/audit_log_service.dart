import 'dart:async';
import 'dart:io';

import 'package:bipou_frontend/models/audit_log_entry.dart';
import 'package:bipou_frontend/services/platform/audit_directory_provider.dart';

abstract interface class AuditLogWriter {
  Future<File> append(AuditLogEntry entry);
}

abstract interface class AuditLogReader {
  Future<File?> findExistingFile();
}

typedef DocumentsDirectoryProvider = Future<Directory> Function();

final class AuditLogService implements AuditLogWriter, AuditLogReader {
  AuditLogService({DocumentsDirectoryProvider? documentsDirectoryProvider})
    : _documentsDirectoryProvider =
          documentsDirectoryProvider ?? defaultDocumentsDirectoryProvider;

  static const String fileName = 'auditoria_evento.txt';

  final DocumentsDirectoryProvider _documentsDirectoryProvider;
  Future<void> _writeQueue = Future<void>.value();

  @override
  Future<File?> findExistingFile() async {
    try {
      final file = await _auditFile();
      return await file.exists() ? file : null;
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        AuditLogException('Nao foi possivel acessar a auditoria local.', error),
        stackTrace,
      );
    }
  }

  @override
  Future<File> append(AuditLogEntry entry) {
    final write = _writeQueue.then((_) => _appendToFile(entry));

    // Uma falha nao pode interromper permanentemente as proximas escritas.
    _writeQueue = write.then<void>((_) {}, onError: (_, _) {});
    return write;
  }

  Future<File> _appendToFile(AuditLogEntry entry) async {
    try {
      final file = await _auditFile();

      return await file.writeAsString(
        '${entry.toLine()}\n',
        mode: FileMode.append,
        flush: true,
      );
    } on Object catch (error, stackTrace) {
      Error.throwWithStackTrace(
        AuditLogException('Nao foi possivel gravar a auditoria local.', error),
        stackTrace,
      );
    }
  }

  Future<File> _auditFile() async {
    final directory = await _documentsDirectoryProvider();
    return File('${directory.path}${Platform.pathSeparator}$fileName');
  }
}

final class AuditLogException implements Exception {
  const AuditLogException(this.message, [this.cause]);

  final String message;
  final Object? cause;

  @override
  String toString() => 'AuditLogException: $message';
}
