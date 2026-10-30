# MCPS

MCPs do Nexor autorizados neste projeto, e para que.

| MCP | Uso aqui | Limite |
| github | issues, release, workflow de publicacao, revisao de PR | sem force-push, sem auto-merge, sem publicar release sem autorizacao |
| codebase-memory | indice do codigo C#, achar classe e chamada antes de ler arquivo inteiro | so leitura do indice |
| context7 | documentacao atual de API .NET, WPF, WMI, PInvoke | so consulta |

MCPs nao usados aqui: playwright (nao existe interface web), proxmox-lan (nao existe servidor neste projeto), firecrawl (tarefa de marketing nao faz parte desta versao), keepass-local (nenhuma credencial e usada no build).

Regra de segredo: nenhum MCP le valor de cofre neste projeto. Se alguma tarefa exigir certificado de assinatura, a acao para e volta para o Gabriel com o motivo.

Registro de MCP segue `A:\Desenvolvimento\IA\Nexor\mcp\<nome>\config.json`, com segredo apenas no Credential Manager.
