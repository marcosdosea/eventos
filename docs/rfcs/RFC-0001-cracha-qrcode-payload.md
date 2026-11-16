# RFC 0001: Padronização do Payload Unificado de QR Code para Crachás e Credenciamento (Boralá ⟷ Bipou)

- **Status:** Aprovado
- **Versão do Protocolo:** 1.0 (v1)
- **Data de Aprovação:** 16 de Novembro de 2026
- **Sistemas Afetados:** Boralá (Web/Backend) e Bipou (Mobile/Frontend)

---

## 1. Contexto e Motivação

Historicamente, o ecossistema Boralá e Bipou apresentava discrepâncias no formato do payload codificado nos QR Codes impressos nos crachás de participantes:

1. **Formato textual do Boralá:**
   ```text
   [{IdPessoa}] [{NomeCracha}] [{IdEvento}] [{IdSubEvento1}] [{IdSubEvento2}]...
   ```
   *Problemas:* Fragilidade sintática na leitura, ausência de validação de integridade criptográfica (vulnerável a crachás falsificados/forjados) e dificuldade de interoperabilidade direta com o leitor mobile.

2. **Formato preliminar do Bipou mobile:**
   ```json
   {"id": "UUID", "nome": "Nome do participante"}
   ```
   *Problemas:* Não continha informações do evento nem dos subeventos (cursos/oficinas) em que o participante estava inscrito, impedindo validação offline em salas e estandes específicos.

Esta RFC estabelece o **contrato oficial unificado de payload v1** em formato JSON compacto, assinado via HMAC-SHA256, com interoperabilidade garantida entre o gerador do Boralá (C#) e o leitor do Bipou (Dart/Flutter).

---

## 2. Especificação do Payload Unificado (v1)

O QR Code impresso no crachá deve conter estritamente a seguinte estrutura JSON compacta:

```json
{
  "v": 1,
  "pid": 1042,
  "cpf": "12345678901",
  "nome": "Maria Santos",
  "eid": 15,
  "sid": [42, 43],
  "hash": "58663eab"
}
```

### 2.1. Dicionário de Campos

| Campo | Tipo | Descrição | Exemplo |
|---|---|---|---|
| `v` | `integer` | Versão do esquema do payload (atualmente `1`). | `1` |
| `pid` | `integer` | ID da Pessoa no Boralá (`IdPessoa`). Inteiro positivo maior que zero. | `1042` |
| `cpf` | `string` | CPF sanitizado (exatamente 11 dígitos numéricos, sem pontuação). | `"12345678901"` |
| `nome` | `string` | Nome a ser exibido no crachá (`NomeCracha` ou `Nome`). Entre 1 e 150 caracteres. | `"Maria Santos"` |
| `eid` | `integer` | ID do Evento principal (`IdEvento`). Inteiro positivo maior que zero. | `15` |
| `sid` | `array[integer]` | Lista com os IDs dos subeventos (minicursos/oficinas) matriculados. Ordenado e sem duplicatas. | `[42, 43]` |
| `hash` | `string` | Assinatura HMAC-SHA256 truncada em 8 caracteres hexadecimais em caixa baixa. | `"58663eab"` |

---

## 3. Algoritmo de Assinatura e Integridade (HMAC-SHA256)

Para prevenir fraude, onde participantes possam editar o JSON e imprimir crachás com acesso a cursos não matriculados ou com identificadores forjados, o payload é assinado criptograficamente.

### 3.1. Representação Canônica

A assinatura é calculada sobre a representação canônica textual determinística formada pela concatenação dos campos com o delimitador pipe (`|`), garantindo que tanto o C# quanto o Dart produzam exatamente os mesmos bytes UTF-8:

```text
{v}|{pid}|{cpf}|{nome}|{eid}|{sid_ordenados_separados_por_virgula}
```

#### Regras de Canonização:
- `v`: Representação inteira em base 10.
- `pid`: Representação inteira em base 10.
- `cpf`: Apenas os 11 dígitos numéricos (`\D` removido).
- `nome`: String com `trim()` aplicado nas extremidades.
- `eid`: Representação inteira em base 10.
- `sid`: Elementos únicos (distinct), ordenados numericamente de forma ascendente e separados por vírgula `,`. Caso a lista seja vazia, a representação é vazia após o pipe.

### 3.2. Chave Secreta do Evento (`secretKey`)

O HMAC-SHA256 utiliza uma chave secreta associada ao evento. Por padrão, para evitar alterações estruturais no banco de dados, utiliza-se a chave derivada determinística:
```text
borala:evento:{idEvento}:secret
```
Sistemas administrativos e organizadores podem opcionalmente fornecer uma chave customizada por evento via configuração segura.

### 3.3. Truncamento da Assinatura (`hash`)

1. Calcula-se o digest HMAC-SHA256 completo (32 bytes).
2. Converte-se para string hexadecimal em caixa baixa (64 caracteres).
3. Trunca-se nos **primeiros 8 caracteres hexadecimais** (32 bits de entropia, $\approx 4{,}29 \times 10^9$ combinações).

---

## 4. Vetor Golden de Teste (Cross-Platform)

| Propriedade | Valor |
|---|---|
| `v` | `1` |
| `pid` | `1042` |
| `cpf` | `"12345678901"` |
| `nome` | `"Maria Santos"` |
| `eid` | `15` |
| `sid` | `[42, 43]` |
| `secretKey` | `"chave_teste_123"` |
| **String Canônica** | `"1|1042|12345678901|Maria Santos|15|42,43"` |
| **Hash Truncado (`hash`)** | `"58663eab"` |
| **Payload JSON Compacto** | `{"v":1,"pid":1042,"cpf":"12345678901","nome":"Maria Santos","eid":15,"sid":[42,43],"hash":"58663eab"}` |

---

## 5. Retrocompatibilidade

O parser `ParticipanteQrPayload.fromRawJson` no Bipou Mobile suporta nativamente três formatos:

1. **Formato Unificado v1 (Oficial):** Detectado pela presença de `"v": 1` ou campos `pid` e `eid`.
2. **Formato JSON Legado:** `{"id": "<UUID>", "nome": "<Nome>"}`. Mantido para testes existentes e compatibilidade com instâncias antigas.
3. **Formato Textual Legado do Boralá:** `"[1042] [Maria Santos] [15] [42] [43]"`. Detectado pelo início com colchetes `[`.

---

## 6. Arquivos de Implementação

- **Boralá (C# / .NET 8):**
  - `Core/DTO/CrachaQrPayloadDto.cs` em `Core/DTO/`
  - `EventoWeb/Helpers/CrachaQrCodeHelper.cs` em `EventoWeb/Helpers/`
  - `EventoWebTests/Helpers/CrachaQrCodeHelperTests.cs` em `EventoWebTests/Helpers/`

- **Bipou (Dart / Flutter):**
  - `frontend/lib/models/participante_qr_payload.dart` em `frontend/lib/models/`
  - `frontend/test/models/participante_qr_payload_test.dart` em `frontend/test/models/`
