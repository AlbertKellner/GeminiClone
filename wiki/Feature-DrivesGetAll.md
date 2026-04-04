# DrivesGetAll — Listar Drives do Sistema

## Descrição

Lista todos os drives disponíveis no sistema operacional, incluindo tamanho total e espaço disponível formatados. Consultar esta página para entender o contrato do endpoint `GET /drives`, os campos retornados e o comportamento de mapeamento de IDs de drive.

## Autenticação

Não requer autenticação.

## Contrato de Entrada

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Rota** | `/drives` |
| **Headers** | Nenhum obrigatório |
| **Body** | Nenhum |

## Contrato de Saída

### HTTP 200 — Sucesso

```json
{
  "drives": [
    {
      "id": "root",
      "name": "/",
      "driveType": "Fixed",
      "totalSizeBytes": 274877906944,
      "availableSizeBytes": 134217728000,
      "formattedTotalSize": "256 GB",
      "formattedAvailableSize": "125 GB"
    }
  ]
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `id` | string | Identificador URL-seguro do drive (`root` para `/` no Linux; letra minúscula para drives Windows) |
| `name` | string | Nome original do drive conforme sistema operacional |
| `driveType` | string | Tipo do drive (`Fixed`, `Network`, `CDRom`, etc.) |
| `totalSizeBytes` | long | Tamanho total em bytes (0 se drive não estiver pronto) |
| `availableSizeBytes` | long | Espaço disponível em bytes (0 se drive não estiver pronto) |
| `formattedTotalSize` | string | Tamanho total formatado (ex: `256 GB`) |
| `formattedAvailableSize` | string | Espaço disponível formatado (ex: `125 GB`) |

## Comportamento

- **Cross-platform**: utiliza `DriveInfo.GetDrives()` — compatível com Linux, macOS e Windows.
- **Mapeamento de ID**: `/` → `root`; letras Windows (`C:\`) → letra minúscula (`c`) para URLs sem encoding.
- **Drive não pronto**: se `drive.IsReady` for `false` ou `IOException` for lançada, `totalSizeBytes` e `availableSizeBytes` retornam `0`.
- **Formatação**: tamanhos são formatados automaticamente (B, KB, MB, GB, TB) pelo `DiskSizeFormatter`.

## Testes Automatizados

Nenhum teste automatizado presente no repositório.

## BDD

Nenhum cenário BDD definido para esta funcionalidade.

## Referências

- [Governance-Architecture](Governance-Architecture) — Features implementadas e fluxo de request
- [Feature-DiskItemsGetAllByDrive](Feature-DiskItemsGetAllByDrive) — Varredura completa de um drive
- [Feature-DiskItemGetByFolder](Feature-DiskItemGetByFolder) — Varredura de pasta específica
