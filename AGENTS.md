# PHANTOM UI

Porta de entrada do projeto. Leia este arquivo inteiro antes de qualquer tarefa.

Aplicativo desktop Windows (C# / .NET 8 / WPF) que diagnostica hardware, aplica tweaks de sistema com risco medido e reversao, otimiza jogos direto na config de cada um, orienta a BIOS, audita e atualiza drivers oficiais, repara o Windows e gera midia de instalacao pre-otimizada. Interface em portugues do Brasil e ingles.

## O que ler

| Preciso de | Leia |
| Escopo, criterio de pronto, plano | `docs/PRD.md`, `docs/PLANO.md` |
| Arquitetura, modulos, decisoes tecnicas | `docs/TRD.md` |
| Telas, navegacao, fluxo | `docs/FLUXO-APP.md`, `docs/UI-UX.md` |
| Ambientes, caminhos, proibicoes | `projeto/CONTEXTO.md` |
| Equipes ativas neste projeto | `projeto/AGENTES.md` |
| Skills e MCPs autorizados aqui | `projeto/SKILLS.md`, `projeto/MCPS.md` |
| O que esta em andamento | `memoria/FAZENDO.md`, `memoria/COORDENACAO.md` |
| O que já foi feito | `memoria/HISTORICO.md` |
| Projeto de origem (ResetExplode) | `original/LEIA-ME.md` |
| Scripts de tweak estudados | `acervo/LEIA-ME.md` |

## Regras do projeto

- Codigo fica em `codigo/`. Documento, memoria e handoff ficam nas pastas do padrao Nexor.
- Nada de arquivo solto na raiz do projeto. Script, teste, print e log ficam dentro de `codigo/` ou `temp/`.
- Build de verificacao e sempre por linha de comando, sem depender do Visual Studio.
- Tweak destrutivo nao entra no executavel, nem em modo manual, nem em modo automatico. Fica catalogo na secao de mitos bloqueados, com o motivo escrito.
- Todo tweak aplicado precisa de backup do estado anterior e caminho de reversao antes de rodar.
- Nada de segredo no repositorio. Certificado de assinatura, token de release e chave de API ficam no COFRE do Nexor.
- Release publica so com autorizacao explicita do Gabriel.
- Dado real de maquina de cliente nao entra em documento, print ou issue.

## Regras herdadas

Leia e siga `A:\Desenvolvimento\IA\Nexor\AGENTS.md`. Este arquivo nao substitui as regras do Nexor, so acrescenta as do projeto.

## Padrao de retorno

STATUS, MUDANCA, EVIDENCIA, ARQUIVOS, RISCO, PENDENTE.

Sem travessao, emoji, ponto e virgula ou negrito decorativo no texto de documento e de interface.
