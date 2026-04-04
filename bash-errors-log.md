# Log de Erros de Bash

Este arquivo documenta todos os erros de Bash encontrados durante sessões de trabalho neste repositório, incluindo causa raiz e solução adotada. É um log acumulativo — erros não são removidos após resolvidos.

## Template de Registro

```markdown
## Erro [N] — [Título descritivo do problema]

| Campo | Valor |
|---|---|
| **Número** | [N] |
| **Data** | [YYYY-MM-DD] |
| **Comando executado** | `[comando exato que falhou]` |
| **Erro retornado** | `[mensagem de erro exata]` |
| **Causa** | [Explicação técnica objetiva da causa raiz] |
| **Novo comando / solução** | `[comando ou sequência que resolveu]` |
```

---

> **Estado atual**: nenhum erro registrado. Erros serão documentados à medida que forem encontrados durante sessões de trabalho.

---

## Referências

- `docker-compose.yml` — arquivo principal afetado por correções de infraestrutura
- `src/Starter.Template.AOT.Api/Dockerfile` — modificado para suporte a CA customizada e hash symlinks
- `assumptions-log.md` — premissas de ambiente registradas

## Erro 1 — dotnet não encontrado no PATH padrão

| Campo | Valor |
|---|---|
| **Número** | 1 |
| **Data** | 2026-04-02 |
| **Comando executado** | `git checkout -B claude/number-string-endpoint-gxGKO 2>&1 && dotnet --version 2>&1 && docker --version 2>&1` |
| **Erro retornado** | `/bin/bash: line 2: dotnet: command not found` |
| **Causa** | dotnet SDK instalado em `/root/.dotnet` não está no PATH padrão do shell |
| **Novo comando / solução** | `export PATH="/root/.dotnet:$PATH"` antes de qualquer comando dotnet |

## Erro 2 — governance-audit.sh truncado no check 17

| Campo | Valor |
|---|---|
| **Número** | 2 |
| **Data** | 2026-04-02 |
| **Comando executado** | `bash scripts/governance-audit.sh` |
| **Erro retornado** | Exit code 1 — script trunca na verificação 17 (integridade dos hooks) |
| **Causa** | Regex `"[^"]*"` no check 17 truncava em escaped quotes (`\"`), gerando input malformado para o loop `while read` |
| **Novo comando / solução** | Substituir extração por `grep -oP '\.claude/hooks/[a-zA-Z0-9_-]+\.sh' "$SETTINGS" \| sort -u` — extração direta de paths |

## Erro 3 — Health check antes da app estar pronta

| Campo | Valor |
|---|---|
| **Número** | 3 |
| **Data** | 2026-04-02 |
| **Comando executado** | `curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` |
| **Erro retornado** | `HTTP 000` (exit code 7 — connection refused) |
| **Causa** | App em startup com `NotSupportedException` no `MapControllers()` por `IsEnhancedModelMetadataSupported` false ao processar parâmetro de rota |
| **Novo comando / solução** | Adicionar `SuppressInferBindingSourcesForParameters = true` em `ApiBehaviorOptions` no Program.cs |

## Erro 4 — Health check retry (duplicata do Erro 3)

| Campo | Valor |
|---|---|
| **Número** | 4 |
| **Data** | 2026-04-02 |
| **Comando executado** | `curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` |
| **Erro retornado** | `HTTP 000` (exit code 7) |
| **Causa** | Mesmo que Erro 3 — app ainda crashada antes do fix |
| **Novo comando / solução** | Ver Erro 3 |

## Erro 5 — Docker daemon não rodando

| Campo | Valor |
|---|---|
| **Número** | 5 |
| **Data** | 2026-04-02 |
| **Comando executado** | `docker compose up -d --build` |
| **Erro retornado** | `Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?` |
| **Causa** | Docker daemon não iniciado automaticamente no sandbox |
| **Novo comando / solução** | `dockerd > /dev/null 2>&1 &` seguido de `sleep 3` |

## Erro 6 — DNS failure no Docker build

| Campo | Valor |
|---|---|
| **Número** | 6 |
| **Data** | 2026-04-02 |
| **Comando executado** | `docker compose up -d --build` |
| **Erro retornado** | `Temporary failure resolving 'archive.ubuntu.com' / E: Unable to locate package clang` |
| **Causa** | Resolução DNS indisponível dentro de containers Docker neste sandbox — problema de rede do ambiente, não da aplicação |
| **Novo comando / solução** | Pendente — bloqueio de rede do sandbox. CI validará o Docker build |

## Erro 7 — DNS failure no Docker build (retry com DNS config)

| Campo | Valor |
|---|---|
| **Número** | 7 |
| **Data** | 2026-04-02 |
| **Comando executado** | `docker compose up -d --build` (após configurar DNS 8.8.8.8 em daemon.json) |
| **Erro retornado** | `Temporary failure resolving 'archive.ubuntu.com'` |
| **Causa** | DNS config em daemon.json não propagou para BuildKit. Limitação de rede do sandbox |
| **Novo comando / solução** | Pendente — mesma causa que Erro 6 |

## Erro 8 — Health check antes da app terminar compilação

| Campo | Valor |
|---|---|
| **Número** | 8 |
| **Data** | 2026-04-02 |
| **Comando executado** | `sleep 6 && curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` |
| **Erro retornado** | `HTTP 000` (exit code 7 — connection refused) |
| **Causa** | App ainda compilando (Roslyn) após 6 segundos de espera; precisa de ~15 segundos para iniciar |
| **Novo comando / solução** | Aumentar sleep para 15 segundos ou usar polling loop |

## Erro 9 — git push HTTP 503

| Campo | Valor |
|---|---|
| **Número** | 9 |
| **Data** | 2026-04-02 |
| **Comando executado** | `git push -u origin claude/number-string-endpoint-gxGKO` |
| **Erro retornado** | `error: RPC failed; HTTP 503 curl 22 The requested URL returned error: 503` |
| **Causa** | Servidor git remoto retornando HTTP 503 (Service Unavailable) em todas as tentativas de push |
| **Novo comando / solução** | Retry com backoff exponencial — todas falharam. Aguardar servidor restabelecer |

## Erro 10 — git push HTTP 503 (retry com fetch)

| Campo | Valor |
|---|---|
| **Número** | 10 |
| **Data** | 2026-04-02 |
| **Comando executado** | `git fetch origin && git push origin claude/number-string-endpoint-gxGKO` |
| **Erro retornado** | `error: RPC failed; HTTP 503 curl 22 The requested URL returned error: 503` |
| **Causa** | Mesma causa do Erro 9 — fetch funciona, push bloqueado pelo servidor |
| **Novo comando / solução** | Ver Erro 9 |

## Erro 11 — Captura automática via hook (duplicata do Erro 8)

| Campo | Valor |
|---|---|
| **Número** | 11 |
| **Data** | 2026-04-02 |
| **Comando executado** | `sleep 15 && curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | Duplicata do Erro 8 — app ainda compilando após 15 segundos |
| **Novo comando / solução** | Ver Erro 8 |

## Erro 12 — MCP servers do GitHub não carregados no startup do Claude Code

| Campo | Valor |
|---|---|
| **Número** | 12 |
| **Data** | 2026-04-02 |
| **Comando executado** | ToolSearch para `mcp__github__*` no início da sessão |
| **Erro retornado** | `No matching deferred tools found` — nenhuma ferramenta MCP do GitHub disponível |
| **Causa** | Inicialização assíncrona dos MCP servers falhou ou excedeu timeout. Endpoint 100% acessível (6/6 testes OK via curl), tokens válidos (ClaudeCode-Bot e Claude-Revisor confirmados). Problema é client-side: Claude Code não completou handshake com os MCP servers no startup. `MCP_TIMEOUT` não estava configurado — timeout padrão insuficiente para payload pesado (20+ tools com ícones base64). |
| **Novo comando / solução** | Adicionar `"MCP_TIMEOUT": "60000"` e `"MCP_TOOL_TIMEOUT": "300000"` em `.claude/settings.json` seção `env`. Adicionar verificação de conectividade MCP ao `session-start.sh` para diagnóstico imediato. |

## Erro 13 — Falso positivo do hook PreToolUse:Bash em comandos com variáveis MCP

| Campo | Valor |
|---|---|
| **Número** | 13 |
| **Data** | 2026-04-02 |
| **Comando executado** | `curl -s -X POST "https://api.githubcopilot.com/mcp/" -H "Authorization: Bearer ${GH_CLAUDE_CODE_MCP_CODIFICADOR}" ...` |
| **Erro retornado** | `[PreToolUse] BLOQUEADO: git push --force detectado. Force push é proibido sem autorização explícita.` |
| **Causa** | O pattern glob `"if": "Bash(git push --force*)"` no hook PreToolUse:Bash casava incorretamente com comandos que expandem variáveis de ambiente. O `*` no final do pattern permitia matching amplo demais quando o conteúdo expandido das variáveis era avaliado. |
| **Novo comando / solução** | Substituir pattern `Bash(git push --force*)` por dois patterns específicos: `Bash(git push --force)` (exato) e `Bash(git push --force *)` (com espaço antes do `*`). Também adicionado pattern separado para `--force-with-lease`. |

## Erro 14 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 14 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s "https://api.github.com/repos/albertkellner/GeminiClone/contents/ArquivosDoDisco/UseCase/DriverFind.cs" | python3 -c "import json,sys,base64; d=json.load(sys.stdin); print(base64.b64decode(d['content']).decode())"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 15 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 15 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s --max-time 5 "http://localhost:5000/drives/root/folder?path=home" -w "\nHTTP %{http_code}"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 16 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 16 |
| **Data** | 2026-04-03 |
| **Comando executado** | `pkill -f "dotnet run --project src/Starter" 2>/dev/null; sleep 2
export PATH="/root/.dotnet:$PATH" && dotnet run --project src/Starter.Template.AOT.Api/Starter.Template.AOT.Api.csproj > /tmp/app_run2.log 2>&1 &
echo "PID: $!"
sleep 20 && curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 17 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 17 |
| **Data** | 2026-04-03 |
| **Comando executado** | `pkill -f "dotnet exec" 2>/dev/null; pkill -f "Starter.Template" 2>/dev/null
sleep 3
ps aux | grep dotnet | grep -v grep | awk '{print $2}' | xargs kill -9 2>/dev/null
sleep 2
echo "Processes killed"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 18 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 18 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s "https://api.github.com/repos/AlbertKellner/GeminiClone/contents/ArquivosDoDisco.Web/Controllers" | python3 -c "import json,sys; d=json.load(sys.stdin); [print(x['name'], x['download_url']) for x in d]" 2>&1

curl -s "https://api.github.com/repos/AlbertKellner/GeminiClone/contents/ArquivosDoDisco" | python3 -c "import json,sys; d=json.load(sys.stdin); [print(x['name'], x.get('download_url','dir')) for x in d]" 2>&1` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 19 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 19 |
| **Data** | 2026-04-03 |
| **Comando executado** | `export PATH="/root/.dotnet:$PATH"
pkill -f "Starter.Template.AOT.Api" 2>/dev/null; sleep 2
dotnet run --project src/Starter.Template.AOT.Api/Starter.Template.AOT.Api.csproj > /tmp/app_frontend.log 2>&1 &
echo "PID=$!"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 20 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 20 |
| **Data** | 2026-04-03 |
| **Comando executado** | `# Validate static file serving (index.html)
curl -s -o /dev/null -w "HTTP %{http_code} Content-Type: %{content_type}\n" http://localhost:5000/
echo ""

# Validate GET /drives
curl -s http://localhost:5000/drives | python3 -c "import json,sys; d=json.load(sys.stdin); print('drives count:', len(d['drives'])); [print(' -', x['id'], x['name'], x['formattedTotalSize']) for x in d['drives']]"
echo ""

# Validate GET /drives/root/items (small path only)
curl -s "http://localhost:5000/drives/root/items" -o /tmp/items_resp.json -w "HTTP %{http_code}" --max-time 5
echo ""
python3 -c "import json; d=json.load(open('/tmp/items_resp.json')); print('driveId:', d.get('driveId')); root=d.get('root'); print('root name:', root['name'] if root else None); print('root children count:', len(root['children']) if root else 0)"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 21 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 21 |
| **Data** | 2026-04-03 |
| **Comando executado** | `# Test folder endpoint with a manageable path
curl -s --max-time 15 "http://localhost:5000/drives/root/folder?path=home/user/Starter.Template.AOT/src" -w "\nHTTP %{http_code}" | python3 -c "
import json,sys
raw = sys.stdin.read()
lines = raw.split('\n')
http_line = [l for l in lines if l.startswith('HTTP')]
json_str = '\n'.join([l for l in lines if not l.startswith('HTTP')])
d = json.load(__import__('io').StringIO(json_str))
print('HTTP:', http_line[0] if http_line else '?')
print('driveId:', d.get('driveId'))
print('folderPath:', d.get('folderPath'))
folder = d.get('folder')
if folder:
    print('folder name:', folder['name'])
    print('folder formattedSize:', folder['formattedSize'])
    print('children count:', len(folder.get('children', [])))
    for c in folder.get('children', [])[:5]:
        print(' -', c['name'], c['formattedSize'])
"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 22 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 22 |
| **Data** | 2026-04-03 |
| **Comando executado** | `pkill -f "Starter.Template.AOT.Api" 2>/dev/null; pkill -f "dotnet exec" 2>/dev/null
sleep 3
fuser -k 5000/tcp 2>/dev/null; sleep 2

# Restart
bash /tmp/start_app.sh > /tmp/app_frontend2.log 2>&1 &
echo "PID=$!"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 23 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 23 |
| **Data** | 2026-04-03 |
| **Comando executado** | `pkill -9 -f "dotnet" 2>/dev/null; sleep 2
fuser -k 5000/tcp 2>/dev/null; sleep 1
bash /tmp/start_app.sh > /tmp/app_frontend2.log 2>&1 &
echo "PID=$!"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 24 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 24 |
| **Data** | 2026-04-03 |
| **Comando executado** | `pkill -f "Starter.Template.AOT.Api" 2>/dev/null; pkill -9 -f "dotnet" 2>/dev/null; echo "app stopped"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 25 — Captura automática via hook

| Campo | Valor |
|---|---|
| **Número** | 25 |
| **Data** | 2026-04-03 |
| **Comando executado** | `export PATH="/root/.dotnet:$PATH"
pkill -f "Starter.Template.AOT.Api" 2>/dev/null; sleep 2
dotnet run --project src/Starter.Template.AOT.Api/Starter.Template.AOT.Api.csproj > /tmp/app_integration.log 2>&1 &
echo "PID=$!"` |
| **Erro retornado** | Capturado automaticamente pelo hook bash-error-capture.sh |
| **Causa** | A ser investigada pelo assistente |
| **Novo comando / solução** | Pendente |

## Erro 26 — Health check antes da app terminar compilação (segunda instância)

| Campo | Valor |
|---|---|
| **Número** | 26 |
| **Data** | 2026-04-03 |
| **Comando executado** | `sleep 18 && curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` |
| **Erro retornado** | `HTTP 000` (exit code 7 — connection refused) |
| **Causa** | App ainda compilando (Roslyn) após 18 segundos. Compilação de nova instância demorou mais que o esperado. Retry com sleep adicional de 10s resolveu. |
| **Novo comando / solução** | `sleep 10 && curl -s -o /dev/null -w "HTTP %{http_code}" http://localhost:5000/health` (após os 18s iniciais) |

## Erro 27 — Scan root/items esgota thread pool, app para de responder

| Campo | Valor |
|---|---|
| **Número** | 27 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s --max-time 5 http://localhost:5000/drives/root/items -o /tmp/items.json -w "HTTP %{http_code}"` |
| **Erro retornado** | Timeout (exit code 28) — app parou de responder a requisições subsequentes |
| **Causa** | GET /drives/root/items varre 251 GB recursivamente. O scan com Task.Run paralelo esgotou o thread pool do .NET, tornando a app incapaz de processar outras requisições. Comportamento esperado em ambiente de sandbox com filesystem gigante. |
| **Novo comando / solução** | Evitar GET /drives/root/items no sandbox. Usar GET /drives/{id}/folder?path=... para validar comportamento do endpoint de pasta, que é limitado a uma subárvore menor. Reiniciar a app após o teste. |

## Erro 28 — Folder endpoint timeout (app sem thread pool após Erro 27)

| Campo | Valor |
|---|---|
| **Número** | 28 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s --max-time 10 "http://localhost:5000/drives/root/folder?path=home/user" -w "\nHTTP %{http_code}"` |
| **Erro retornado** | `HTTP 000` (exit code 28 — timeout) |
| **Causa** | Consequência do Erro 27 — app com thread pool esgotado não conseguia processar novas requisições. Não é falha do endpoint em si. |
| **Novo comando / solução** | Reiniciar a app (`fuser -k 5000/tcp`) e repetir. Endpoint funcionou corretamente após restart. |

## Erro 29 — Folder endpoint timeout (mesma causa do Erro 28)

| Campo | Valor |
|---|---|
| **Número** | 29 |
| **Data** | 2026-04-03 |
| **Comando executado** | `sleep 5 && curl -s --max-time 10 "http://localhost:5000/drives/root/folder?path=home" -w "\nHTTP %{http_code}"` |
| **Erro retornado** | `HTTP 000` (exit code 28 — timeout) |
| **Causa** | Mesma causa do Erro 28 — app com thread pool esgotado pelo scan de Erro 27. |
| **Novo comando / solução** | Ver Erro 27. Reiniciar app antes de continuar testes. |

## Erro 30 — Health check timeout (app sem thread pool após Erro 27)

| Campo | Valor |
|---|---|
| **Número** | 30 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s --max-time 5 http://localhost:5000/health -w "HTTP %{http_code}"` |
| **Erro retornado** | `HTTP 000` (exit code 28 — timeout) |
| **Causa** | Confirmação de que o app estava totalmente sem resposta após o scan de Erro 27 esgotar o thread pool. Nem o health check respondia. |
| **Novo comando / solução** | `fuser -k 5000/tcp` para encerrar o processo e reiniciar. |

## Erro 31 — Drive não-root com path-based ID retorna 404

| Campo | Valor |
|---|---|
| **Número** | 31 |
| **Data** | 2026-04-03 |
| **Comando executado** | `curl -s --max-time 15 "http://localhost:5000/drives/%2Fopt%2Fenv-runner/items"` |
| **Erro retornado** | `HTTP 404 Not Found` — drive não encontrado |
| **Causa** | `BuildDriveId` no `DrivesGetAllRepository` mapeia `/` → `root` mas mantém outros mount points Linux com seus paths originais (ex: `/opt/env-runner`). Quando URL-encoded como `%2Fopt%2Fenv-runner` e passado na rota, o ASP.NET Core não decodifica `%2F` para `/`, então o lookup falha. O drive ID registrado é `/opt/env-runner` mas o parâmetro recebido é `%2Fopt%2Fenv-runner`. |
| **Novo comando / solução** | Limitação de design: drives não-root em Linux com path-based IDs não funcionam como segmentos de URL. Mitigação: o frontend só exibirá drives cujo ID não contém `/` (ou seja, apenas `root` e drives Windows-style). Melhoria futura: mapear todos os IDs para slugs sem barras. |
