# Frontend Bipou

Aplicativo Flutter Android usado na portaria do evento. Ele faz leitura continua de QR Code, cadastro manual e auditoria local antes de enviar qualquer dado para a API.

## Requisitos

- Flutter configurado para Android
- Android SDK
- Um aparelho Android fisico ou emulador
- Backend Bipou acessivel por IP local ou Tailscale

## Instalar dependencias

```bash
flutter pub get
```

## Configurar URL da API

O app nao acessa o PostgreSQL diretamente. Ele conversa apenas com a API Spring Boot.

Na maquina do backend, descubra o IP Tailscale:

```bash
tailscale ip -4
```

Use esse endereco no `--dart-define`:

```text
API_BASE_URL=http://100.x.y.z:8080
```

Nao precisa passar node ID, chave ou token do Tailscale para o app. O que importa e o celular estar conectado a mesma tailnet e conseguir acessar a porta `8080` da maquina do backend.

## Rodar em desenvolvimento

```bash
flutter run --dart-define=API_BASE_URL=http://100.x.y.z:8080
```

## Rodar testes

```bash
flutter test
```

## Analise estatica

```bash
flutter analyze
```

## Gerar APK

Para instalar rapido em aparelho interno:

```bash
flutter build apk --debug --dart-define=API_BASE_URL=http://100.x.y.z:8080
```

Arquivo gerado:

```text
build/app/outputs/flutter-apk/app-debug.apk
```

Para instalar via USB:

```bash
adb install -r build/app/outputs/flutter-apk/app-debug.apk
```

Para gerar uma build release:

```bash
flutter build apk --release --dart-define=API_BASE_URL=http://100.x.y.z:8080
```

Como o app e para uso interno, o APK debug pode ser suficiente para teste operacional. Se a URL da API mudar, gere um novo APK porque `API_BASE_URL` fica embutida na build.

## Auditoria local

Antes de enviar para a API, o app grava uma linha no arquivo local `auditoria_evento.txt`:

```text
[DataHoraIso8601];[ParticipanteId];[ParticipanteNome];[TipoAcao];[DispositivoId]
```

No cadastro ainda nao enviado, `ParticipanteId` fica vazio. Caracteres de controle e delimitadores do nome sao escapados no arquivo para preservar uma operacao por linha.

Se a rede cair, a leitura nao e bloqueada. O operador recebe feedback de salvo offline e o arquivo local permanece como base de auditoria/recuperacao.

Cadastros manuais também são gravados antes do envio no arquivo privado `cadastros_pendentes.json`. Cada item possui um `cadastroId` estável: quando a conexão volta, o app reenvia automaticamente sem criar participantes duplicados e remove da fila somente depois da confirmação da API.

## Fluxos principais

- Dashboard com modos de entrada, saida e cadastro manual.
- QR Code no formato `{"id":"UUID","nome":"Nome do participante"}`.
- Scanner continuo com debounce para evitar leitura duplicada do mesmo participante.
- Entrada e saída também podem ser registradas digitando o CPF; o app consulta o UUID e o nome antes de usar o mesmo fluxo do QR Code.
- Cadastro manual com nome e CPF obrigatórios.
- Envio HTTP para a API configurada por `API_BASE_URL`.

## Importação e impressão em lote

Os utilitários de linha de comando para importar participantes por CSV e exportar todas as credenciais em PDF A4 estão documentados em [tool/README.md](tool/README.md).
