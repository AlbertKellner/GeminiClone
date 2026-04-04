# DiskItemGetByFolder — Varredura de Pasta Específica

## Descrição

Varre recursivamente os arquivos e pastas de uma pasta específica dentro de um drive, retornando a árvore com tamanhos acumulados e ordenação por tamanho descendente. Consultar esta página para entender o contrato do endpoint `GET /drives/{driveId}/folder`, os parâmetros aceitos e o comportamento de resolução de caminho.

## Autenticação

Não requer autenticação.

## Contrato de Entrada

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Rota** | `/drives/{driveId}/folder` |
| **Parâmetro de rota** | `driveId` — ID do drive conforme retornado por `GET /drives` (ex: `root`, `c`) |
| **Parâmetro de query** | `path` — Caminho relativo da pasta dentro do drive (ex: `home/user/Starter.Template.AOT`). Valor padrão: string vazia (raiz do drive) |
| **Headers** | Nenhum obrigatório |
| **Body** | Nenhum |

## Contrato de Saída

### HTTP 200 — Sucesso

```json
{
  "driveId": "root",
  "folderPath": "home/user/Starter.Template.AOT/src",
  "folder": {
    "name": "src",
    "sizeBytes": 3366912,
    "formattedSize": "3.21 MB",
    "isFolder": true,
    "extension": "",
    "children": [
      {
        "name": "Starter.Template.AOT.Api",
        "sizeBytes": 2097152,
        "formattedSize": "2 MB",
        "isFolder": true,
        "extension": "",
        "children": []
      }
    ]
  }
}
```

### HTTP 404 — Pasta não encontrada

Retornado quando a combinação `driveId` + `path` não corresponde a uma pasta existente no sistema.

| Campo | Tipo | Descrição |
|---|---|---|
| `driveId` | string | ID do drive informado na requisição |
| `folderPath` | string | Caminho relativo informado na requisição |
| `folder.name` | string | Nome da pasta raiz da varredura |
| `folder.sizeBytes` | long | Tamanho acumulado em bytes |
| `folder.formattedSize` | string | Tamanho formatado (B, KB, MB, GB, TB) |
| `folder.isFolder` | bool | Indica se o nó é pasta ou arquivo |
| `folder.extension` | string | Extensão do arquivo (vazio para pastas) |
| `folder.children` | array | Filhos ordenados por tamanho descendente |

## Comportamento

- **Resolução de caminho**: o `path` é normalizado (separadores `\` e `/` tratados igualmente) e combinado com o root do drive para formar o caminho absoluto.
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
- [Feature-DiskItemsGetAllByDrive](Feature-DiskItemsGetAllByDrive) — Varredura completa de drive
