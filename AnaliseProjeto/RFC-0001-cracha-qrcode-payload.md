# RFC 0001: Padronização do Payload Unificado de QR Code para Crachás e Credenciamento (Boralá ⟷ Bipou)

- **Status:** Aprovado
- **Versão do Protocolo:** 1.0 (v1)
- **Data de Aprovação:** 16 de Novembro de 2026
- **Sistemas Afetados:** Boralá (Web/Backend) e Bipou (Mobile/Frontend)

Consulte o documento completo em [docs/rfcs/RFC-0001-cracha-qrcode-payload.md](../docs/rfcs/RFC-0001-cracha-qrcode-payload.md).

---

## Resumo do Protocolo

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

- **String Canônica:** `{v}|{pid}|{cpf}|{nome}|{eid}|{sid_ordenados}` (ex: `"1|1042|12345678901|Maria Santos|15|42,43"`)
- **Hash:** HMAC-SHA256 truncado em 8 caracteres hexadecimais em caixa baixa.
- **Chave Secreta Padrão:** `borala:evento:{idEvento}:secret`
