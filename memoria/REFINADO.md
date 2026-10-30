# REFINADO

Só daqui se pega trabalho. Cada item com escopo e criterio de pronto.

## R001. Adequar o projeto ao padrao Nexor

Escopo: mover codigo para `codigo/`, criar AGENTS.md, CLAUDE.md, memoria, projeto, docs, handoffs e temp, apontar skills e MCPs autorizados, ajustar caminhos de build e de publicacao.

Criterio de pronto: build limpo dentro da raiz nova, comando de build registrado em HISTORICO.md, nenhum arquivo solto na raiz, README com o caminho novo.

Estado: concluido em 2026-10-29.

## R002. Verificacao manual no Windows

Escopo: rodar o roteiro de `docs/CHECKLIST-TESTE-WINDOWS.md` na maquina de teste.

Criterio de pronto: checklist inteiro preenchido, cada item com Passou, Falhou ou Nao verificado, e uma lista de correcao derivada dele.

Dependencia: R001.

## R003. Correcao do que o teste apontar

Escopo: uma correcao por vez, cada uma amarrada a um item do checklist.

Criterio de pronto: nenhum item critico em Falhou, build continua limpo, reversao continua provada.

Dependencia: R002.

## R004. Verificacao automatica do catalogo em CI

Escopo: script que falha quando existe id duplicado, perfil apontando para id inexistente, tweak de risco alto dentro de perfil, ou comando da lista de bloqueio presente no catalogo executavel.

Criterio de pronto: script roda no workflow, quebra o build em caso de violacao, e prova que quebra ao injetar um id duplicado de proposito.

Dependencia: R001.

## R005. Release v1.0.0

Escopo: versao, notas, pacote, workflow, teste do executavel publicado em maquina sem .NET.

Criterio de pronto: pacote abre em maquina limpa, notas publicadas, autorizacao do Gabriel registrada.

Dependencia: R003.
