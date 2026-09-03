enum ScannerFeedbackType { sucesso, offline, erro }

final class ScannerFeedback {
  const ScannerFeedback({required this.message, required this.type});

  final String message;
  final ScannerFeedbackType type;
}
