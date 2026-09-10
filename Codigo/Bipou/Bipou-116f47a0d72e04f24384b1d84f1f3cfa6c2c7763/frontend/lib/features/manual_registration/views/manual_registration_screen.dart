import 'package:bipou_frontend/core/validation/cpf_validator.dart';
import 'package:bipou_frontend/features/manual_registration/view_models/manual_registration_view_model.dart';
import 'package:bipou_frontend/models/operation_result.dart';
import 'package:bipou_frontend/models/participante_response.dart';
import 'package:bipou_frontend/services/credenciamento_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

final class ManualRegistrationScreen extends StatefulWidget {
  const ManualRegistrationScreen({
    required this.credenciamento,
    required this.dispositivoId,
    super.key,
  });

  final CredenciamentoGateway credenciamento;
  final String dispositivoId;

  @override
  State<ManualRegistrationScreen> createState() =>
      _ManualRegistrationScreenState();
}

final class _ManualRegistrationScreenState
    extends State<ManualRegistrationScreen> {
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  final TextEditingController _nomeController = TextEditingController();
  final TextEditingController _cpfController = TextEditingController();
  late final ManualRegistrationViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = ManualRegistrationViewModel(
      credenciamento: widget.credenciamento,
      dispositivoId: widget.dispositivoId,
    );
  }

  @override
  void dispose() {
    _nomeController.dispose();
    _cpfController.dispose();
    _viewModel.dispose();
    super.dispose();
  }

  Future<void> _salvar() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final result = await _viewModel.salvar(
      nome: _nomeController.text,
      cpf: _cpfController.text,
    );
    if (!mounted) {
      return;
    }

    if (result != null) {
      Navigator.of(context).pop<OperationResult<ParticipanteResponse>>(result);
      return;
    }

    final message = _viewModel.errorMessage;
    if (message != null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(message),
            backgroundColor: const Color(0xFFB42318),
          ),
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _viewModel,
      builder: (context, _) {
        return PopScope(
          canPop: !_viewModel.salvando,
          child: Scaffold(
            appBar: AppBar(title: const Text('Cadastro Manual')),
            body: SafeArea(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: Form(
                  key: _formKey,
                  child: AutofillGroup(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: <Widget>[
                        TextFormField(
                          controller: _nomeController,
                          autofocus: true,
                          enabled: !_viewModel.salvando,
                          textCapitalization: TextCapitalization.words,
                          textInputAction: TextInputAction.next,
                          autofillHints: const <String>[AutofillHints.name],
                          maxLength: 150,
                          decoration: const InputDecoration(
                            labelText: 'Nome',
                            prefixIcon: Icon(Icons.person),
                          ),
                          validator: _validarNome,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _cpfController,
                          enabled: !_viewModel.salvando,
                          keyboardType: TextInputType.number,
                          textInputAction: TextInputAction.done,
                          autofillHints: const <String>[AutofillHints.username],
                          maxLength: 11,
                          inputFormatters: <TextInputFormatter>[
                            FilteringTextInputFormatter.digitsOnly,
                            LengthLimitingTextInputFormatter(11),
                          ],
                          decoration: const InputDecoration(
                            labelText: 'CPF',
                            hintText: '***.***.***-**',
                            prefixIcon: Icon(Icons.badge),
                          ),
                          validator: _validarCpf,
                          onFieldSubmitted: (_) => _salvar(),
                        ),
                        const SizedBox(height: 24),
                        FilledButton.icon(
                          onPressed: _viewModel.salvando ? null : _salvar,
                          icon: _viewModel.salvando
                              ? const SizedBox.square(
                                  dimension: 22,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : const Icon(Icons.save),
                          label: Text(
                            _viewModel.salvando ? 'Salvando...' : 'Salvar',
                          ),
                          style: FilledButton.styleFrom(
                            minimumSize: const Size.fromHeight(64),
                            textStyle: const TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        );
      },
    );
  }

  String? _validarNome(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Informe o nome';
    }
    return null;
  }

  String? _validarCpf(String? value) {
    final cpf = value?.trim() ?? '';
    if (!RegExp(r'^\d{11}$').hasMatch(cpf)) {
      return 'Informe os 11 dígitos do CPF';
    }
    if (!CpfValidator.isValid(cpf)) {
      return 'CPF inválido';
    }
    return null;
  }
}
