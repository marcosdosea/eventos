import 'package:bipou_frontend/models/tipo_acao.dart';

final class RecentScanRecord {
  const RecentScanRecord({
    required this.participanteId,
    required this.participanteNome,
    required this.tipoAcao,
    required this.registradoEm,
    this.offline = false,
  });

  final String participanteId;
  final String participanteNome;
  final TipoAcao tipoAcao;
  final DateTime registradoEm;
  final bool offline;

  String get statusLabel => tipoAcao == TipoAcao.entrada ? 'ENTRADA' : 'SAÍDA';
}
