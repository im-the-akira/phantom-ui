# PLANO DE IMPLEMENTACAO

Cada etapa com dependencia e criterio de conclusao verificavel.

## Etapa 1. Estrutura Nexor (concluida)

Mover codigo para `codigo/`, criar memoria, docs, projeto, handoffs, temp, AGENTS.md e CLAUDE.md.
Pronto quando: build roda dentro de `codigo/` com zero erro.

## Etapa 2. Verificacao de build na raiz nova

Rodar build limpo depois da mudanca de pasta. Registrar comando e resultado em `memoria/HISTORICO.md`.
Pronto quando: 0 erro, 0 aviso, em Debug e em Release.

## Etapa 3. Teste manual no Windows (depende de 2)

Roteiro de 27 paginas, um item aplicado e revertido por secao critica, relatorio de driver, geracao de pendrive.
Pronto quando: `docs/CHECKLIST-TESTE-WINDOWS.md` preenchido com Passou, Falhou ou Nao verificado, sem item em branco.

## Etapa 4. Correcao do que o teste apontar (depende de 3)

Uma correcao por vez, com o item do checklist como criterio.
Pronto quando: nenhum item critico em Falhou.

## Etapa 5. Release (depende de 4 e de autorizacao)

Versao, notas, workflow, pacote, teste do executavel publicado em maquina limpa.
Pronto quando: pacote abre em maquina sem .NET instalado e Gabriel autorizou.

## Fila depois da v1.0.0

- Assinatura de codigo para reduzir aviso do SmartScreen.
- Instalador com desinstalacao limpa.
- Verificacao automatica do catalogo (id duplicado, perfil apontando para id inexistente) rodando em CI.
- Modo somente leitura exportavel em relatorio para suporte.
