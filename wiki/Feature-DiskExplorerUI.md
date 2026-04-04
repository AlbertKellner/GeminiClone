# Disk Explorer UI

## Descrição

Interface web estática de exploração de disco servida diretamente pela API. Permite visualizar graficamente o uso de espaço em disco através de um sunburst chart interativo e uma tabela de itens por pasta. Deve ser consultada ao entender o frontend, a integração com os endpoints da API, ou ao modificar os arquivos em `wwwroot/`. Relaciona-se com [DrivesGetAll](Feature-DrivesGetAll), [DiskItemsGetAllByDrive](Feature-DiskItemsGetAllByDrive) e [DiskItemGetByFolder](Feature-DiskItemGetByFolder).

## Autenticação

**Não requer autenticação.** Os arquivos estáticos (`index.html`, CSS, JS) são servidos publicamente pelo middleware `UseStaticFiles`. Os endpoints de API consumidos pelo frontend também não requerem autenticação.

## Contrato de Entrada

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Rota** | `/` (redireciona para `index.html` via `UseDefaultFiles`) |
| **Headers** | Nenhum obrigatório |
| **Body** | Nenhum |

## Contrato de Saída

| Status | Corpo | Descrição |
|--------|-------|-----------|
| `200 OK` | `text/html` | Página `index.html` com a interface completa |

Os arquivos estáticos adicionais seguem as rotas:
- `GET /css/site.css` → `wwwroot/css/site.css`
- `GET /js/app.js` → `wwwroot/js/app.js`
- `GET /js/colors.js` → `wwwroot/js/colors.js`

## Comportamento

- Ao carregar, o frontend chama `GET /drives` e popula o dropdown de drives.
- Ao selecionar um drive, chama `GET /drives/{driveId}/items` e renderiza o sunburst chart com a árvore completa.
- Ao clicar em um nó do sunburst chart, o gráfico navega para aquele nó e a tabela lateral exibe os filhos imediatos com nome e tamanho formatado.
- Cores são atribuídas automaticamente via algoritmo HSL — interpolação progressiva de cor base para cinza conforme profundidade da árvore.
- Nós pai do sunburst não têm `value` definido — o chart calcula o tamanho dos pais a partir da soma dos filhos.

## Arquitetura Frontend

| Arquivo | Responsabilidade |
|---------|-----------------|
| `wwwroot/index.html` | Estrutura HTML, estilos inline, carregamento de dependências CDN (D3.js, sunburst-chart) |
| `wwwroot/css/site.css` | Tema escuro (Catppuccin Mocha), scrollbars customizados, responsividade |
| `wwwroot/js/app.js` | Lógica principal: `fetchDrives`, `getStructure`, `transformNode`, `generateGraph`, `displayFolderItems`, `removeValueIfChildren`, `setStatus` |
| `wwwroot/js/colors.js` | Algoritmo de coloração: `PREDEFINED_COLORS` (45 cores Material Design), funções HSL, `generateBaseColor`, `addColorToChildren`, `countDescendants` |

### Dependências CDN

| Biblioteca | URL | Propósito |
|------------|-----|-----------|
| D3.js | `//unpkg.com/d3` | Visualização de dados (dependência do sunburst-chart) |
| sunburst-chart | `//unpkg.com/sunburst-chart` | Renderização do gráfico sunburst interativo |

### Servindo Arquivos Estáticos

O middleware de arquivos estáticos do ASP.NET Core é configurado em `Program.cs`:

```csharp
app.UseDefaultFiles();   // Serve index.html para GET /
app.UseStaticFiles();    // Serve wwwroot/ em geral
```

`UseDefaultFiles` deve ser registrado **antes** de `UseStaticFiles` para que o redirecionamento de `/` para `index.html` funcione.

### Integração com API

O frontend consome os seguintes endpoints da API:

| Endpoint | Uso |
|----------|-----|
| `GET /drives` | Listar drives para o dropdown de seleção |
| `GET /drives/{driveId}/items` | Obter árvore completa do drive para o sunburst chart |

A função `transformNode` adapta a resposta da API para o formato do sunburst chart:
- `node.sizeBytes` → `value` (tamanho para proporcionalidade do gráfico)
- `node.name` → `name` (label exibido)
- `node.formattedSize` → `formattedSize` (texto do tooltip)

## Testes Automatizados

Nenhum teste automatizado presente no repositório para o frontend estático.

## BDD

Nenhum cenário BDD definido para esta funcionalidade.

## Referências

- [DrivesGetAll](Feature-DrivesGetAll) — endpoint que lista os drives
- [DiskItemsGetAllByDrive](Feature-DiskItemsGetAllByDrive) — endpoint que retorna a árvore de um drive
- [DiskItemGetByFolder](Feature-DiskItemGetByFolder) — endpoint que retorna a árvore de uma pasta
- [Arquitetura](Governance-Architecture) — estrutura de pastas incluindo `wwwroot/`
- [Operação](Governance-Operation) — como executar a aplicação
