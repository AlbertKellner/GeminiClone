# DiskItemsGetAllByDrive — Varredura Completa de Drive

## Descrição

Varre recursivamente todos os arquivos e pastas de um drive, retornando a árvore completa com tamanhos acumulados e ordenação por tamanho descendente. Consultar esta página para entender o contrato do endpoint `GET /drives/{driveId}/items`, os campos retornados e o comportamento de varredura paralela assíncrona.

**Atenção**: a varredura de drives grandes (ex: disco raiz de 251 GB) pode levar vários minutos. Use [Feature-DiskItemGetByFolder](Feature-DiskItemGetByFolder) para varrer apenas uma pasta específica.

## Autenticação

Não requer autenticação.

## Contrato de Entrada

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Rota** | `/drives/{driveId}/items` |
| **Parâmetro de rota** | `driveId` — ID do drive conforme retornado por `GET /drives` (ex: `root`, `c`) |
| **Headers** | Nenhum obrigatório |
| **Body** | Nenhum |

## Contrato de Saída

### HTTP 200 — Sucesso

```json
{
  "driveId": "root",
  "root": {
    "name": "root",
    "sizeBytes": 274877906944,
    "formattedSize": "256 GB",
    "isFolder": true,
    "extension": "",
    "children": [
      {
        "name": "home",
        "sizeBytes": 1073741824,
        "formattedSize": "1 GB",
        "isFolder": true,
        "extension": "",
        "children": []
      }
    ]
  }
}
```

### HTTP 404 — Drive não encontrado

Retornado quando o `driveId` não corresponde a nenhum drive existente no sistema.

| Campo | Tipo | Descrição |
|---|---|---|
| `driveId` | string | ID do drive varrido |
| `root.name` | string | Nome do nó raiz |
| `root.sizeBytes` | long | Tamanho acumulado em bytes |
| `root.formattedSize` | string | Tamanho formatado (B, KB, MB, GB, TB) |
| `root.isFolder` | bool | Indica se o nó é pasta ou arquivo |
| `root.extension` | string | Extensão do arquivo (vazio para pastas) |
| `root.children` | array | Filhos ordenados por tamanho descendente |

## Comportamento

- **Varredura paralela**: cada subdiretório é varrido em `Task.Run` recursivo para performance.
- **Tamanho acumulado**: o tamanho de cada pasta é a soma recursiva de todos os seus filhos.
- **Ordenação**: filhos ordenados por tamanho descendente em todos os níveis da árvore.
- **Erros de acesso**: diretórios e arquivos inacessíveis (`UnauthorizedAccessException`, `IOException`) são ignorados silenciosamente.
- **Mapeamento de ID**: `root` → `/` no Linux; letra minúscula → `LETRA:\` no Windows.
- **Formatação**: tamanhos formatados pelo `DiskSizeFormatter` compartilhado.

## Testes Automatizados

Nenhum teste automatizado presente no repositório.

## BDD

Nenhum cenário BDD definido para esta funcionalidade.

## Referências

- [Governance-Architecture](Governance-Architecture) — Features implementadas e fluxo de request
- [Feature-DrivesGetAll](Feature-DrivesGetAll) — Listar drives disponíveis
- [Feature-DiskItemGetByFolder](Feature-DiskItemGetByFolder) — Varredura de pasta específica
