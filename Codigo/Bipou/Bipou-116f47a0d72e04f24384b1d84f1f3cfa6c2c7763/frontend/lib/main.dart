import 'package:bipou_frontend/core/config/api_config.dart';
import 'package:bipou_frontend/features/dashboard/views/dashboard_screen.dart';
import 'package:bipou_frontend/services/api_service.dart';
import 'package:bipou_frontend/services/audit_log_export_service.dart';
import 'package:bipou_frontend/services/audit_log_service.dart';
import 'package:bipou_frontend/services/borala_api_service.dart';
import 'package:bipou_frontend/services/cadastro_pendente_store.dart';
import 'package:bipou_frontend/services/connectivity_monitor.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:bipou_frontend/services/device_identity_service.dart';
import 'package:bipou_frontend/services/frequencia_pendente_store.dart';
import 'package:bipou_frontend/services/local_participant_repository.dart';
import 'package:bipou_frontend/services/qr_code_share_service.dart';
import 'package:flutter/material.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  try {
    final dispositivoId = await DeviceIdentityService().getOrCreate();
    final auditLog = AuditLogService();
    final credenciamento = CredenciamentoService(
      auditLog: auditLog,
      api: ApiService(),
      cadastroPendenteStore: CadastroPendenteFileStore(),
      frequenciaPendenteStore: SqliteFrequenciaPendenteStore(),
      connectivityMonitor: ConnectivityPlusMonitor(),
    );
    final localParticipantRepository = SqliteLocalParticipantRepository();
    final boralaApi = BoralaApiService();

    runApp(
      BipouApp(
        credenciamento: credenciamento,
        auditLogExporter: AuditLogExportService(auditLog: auditLog),
        qrCodeShare: QrCodeShareService(),
        dispositivoId: dispositivoId,
        localParticipantRepository: localParticipantRepository,
        boralaApi: boralaApi,
        initialBoralaBaseUrl: ApiConfig.boralaBaseUrl,
      ),
    );
  } on Object catch (error) {
    runApp(_StartupErrorApp(error: error));
  }
}

final class BipouApp extends StatelessWidget {
  const BipouApp({
    required this.credenciamento,
    required this.auditLogExporter,
    required this.qrCodeShare,
    required this.dispositivoId,
    this.localParticipantRepository,
    this.boralaApi,
    this.initialBoralaBaseUrl,
    super.key,
  });

  final CredenciamentoGateway credenciamento;
  final AuditLogExporter auditLogExporter;
  final QrCodeShareGateway qrCodeShare;
  final String dispositivoId;
  final LocalParticipantRepository? localParticipantRepository;
  final BoralaApiClient? boralaApi;
  final String? initialBoralaBaseUrl;

  @override
  Widget build(BuildContext context) {
    const seedColor = Color(0xFF175CD3);
    return MaterialApp(
      title: 'Bipou Credenciamento',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: seedColor),
        useMaterial3: true,
        inputDecorationTheme: const InputDecorationTheme(
          border: OutlineInputBorder(),
          filled: true,
        ),
      ),
      home: DashboardScreen(
        credenciamento: credenciamento,
        auditLogExporter: auditLogExporter,
        qrCodeShare: qrCodeShare,
        dispositivoId: dispositivoId,
        localParticipantRepository: localParticipantRepository,
        boralaApi: boralaApi,
        initialBoralaBaseUrl: initialBoralaBaseUrl,
      ),
    );
  }
}

final class _StartupErrorApp extends StatelessWidget {
  const _StartupErrorApp({required this.error});

  final Object error;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      home: Scaffold(
        body: SafeArea(
          child: Center(
            child: Padding(
              padding: EdgeInsets.all(32),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: <Widget>[
                  Icon(Icons.settings_ethernet, size: 64, color: Colors.red),
                  SizedBox(height: 20),
                  Text(
                    'Não foi possível iniciar o aplicativo.',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
                  ),
                  SizedBox(height: 12),
                  Text(
                    'Verifique a configuração API_BASE_URL e o armazenamento '
                    'local do dispositivo.',
                    textAlign: TextAlign.center,
                  ),
                  SizedBox(height: 16),
                  SelectableText(
                    error.toString(),
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 12),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
