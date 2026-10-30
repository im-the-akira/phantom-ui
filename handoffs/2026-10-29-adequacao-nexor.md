# Handoff 2026-10-29. Adequacao ao padrao Nexor

## Objetivo

Deixar o PHANTOM UI no padrao de projeto do Nexor, com o codigo compilando a partir da raiz nova, pronto para o teste no Windows.

## Estado

Concluido. R001 fechado.

## Mudancas verificadas

- Codigo e repositorio movidos para `codigo/`. Repositorio Git foi recriado na raiz do projeto, com `.gitignore` e `.gitattributes` na raiz.
- Criados `AGENTS.md`, `CLAUDE.md` (uma linha, `@AGENTS.md`), `.gitignore`.
- Criadas `memoria/` (5 arquivos), `projeto/` (4 arquivos), `docs/` (7 arquivos), `handoffs/`, `temp/`.
- Criados `codigo/scripts/build-verificacao.ps1`, `build-verificacao.sh` e `coletar-ambiente.ps1`.
- `codigo/publicar.bat` passa a publicar em `..\temp\dist`.
- Workflow reescrito: verificacao em Linux a cada push, release em Windows apenas em tag `v*` ou execucao manual.
- README atualizado com o caminho novo e a arvore do padrao Nexor.

## Evidencia

`codigo/scripts/build-verificacao.sh Release` a partir de `codigo/`: Build succeeded, 0 Warning, 0 Error.

## Arquivos

Raiz: `AGENTS.md`, `CLAUDE.md`, `.gitignore`.
`docs/`: PRD, TRD, FLUXO-APP, UI-UX, PLANO, ESQUEMA-BACKEND, CHECKLIST-TESTE-WINDOWS.
`memoria/`: IDEIAS, REFINADO, FAZENDO, HISTORICO, COORDENACAO.
`projeto/`: CONTEXTO, AGENTES, SKILLS, MCPS.
`codigo/scripts/`: 3 scripts.

## Decisoes

- Repositorio Git na raiz do projeto, branch `main`, com o codigo em `codigo/`. Motivo: um unico repositorio cobre documento e codigo, como no padrao Nexor.
- Verificacao de build separada da publicacao no CI. Motivo: compilar cruzado prova o codigo, mas o executavel precisa de runner Windows.

## Pendencias

- R002: teste manual no Windows, roteiro em `docs/CHECKLIST-TESTE-WINDOWS.md`.
- R004: verificacao automatica do catalogo em CI, proposto e nao iniciado.
- Skills e MCPs do Nexor declarados em `projeto/SKILLS.md` e `projeto/MCPS.md`, ainda nao conectados nesta sessao.

## Processos ativos

Nenhum.
