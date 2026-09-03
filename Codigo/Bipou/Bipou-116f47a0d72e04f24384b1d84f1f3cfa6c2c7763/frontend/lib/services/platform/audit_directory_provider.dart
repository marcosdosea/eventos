import 'dart:io';

import 'package:bipou_frontend/services/platform/audit_directory_provider_stub.dart'
    if (dart.library.ui) 'package:bipou_frontend/services/platform/audit_directory_provider_flutter.dart'
    as platform;

Future<Directory> defaultDocumentsDirectoryProvider() {
  return platform.getDocumentsDirectory();
}
