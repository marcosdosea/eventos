import 'dart:io';

import 'package:path_provider/path_provider.dart';

Future<Directory> getDocumentsDirectory() {
  return getApplicationDocumentsDirectory();
}
