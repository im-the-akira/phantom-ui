# PRD

## Objetivo

Entregar ao usuario de Windows um unico programa que responda tres perguntas e resolva duas: o que meu PC tem, o que esta segurando meu jogo, o que eu posso mexer com seguranca, e depois aplicar isso e desfazer se der errado.

## Publico

Jogador de PC no Brasil, de 15 a 40 anos, que ja viu video de "tweak milagroso" e nao quer estragar o Windows. Secundario: tecnico que monta e ajusta maquina de cliente.

## Escopo desta versao (v1.0.0)

1. Diagnostico de hardware com aviso acionavel.
2. Catalogo de 217 tweaks com risco medido, comando visivel, estado ao vivo e reversao.
3. Otimizacao automatica por perfil, so com o que e compativel com a maquina.
4. Configuracao direta de 25 jogos, com backup do arquivo original.
5. Orientacao de BIOS somente leitura.
6. Auditoria e atualizacao de driver por fonte oficial.
7. Diagnostico de problema e centro de reparos oficiais.
8. Geracao de midia de instalacao do Windows pre-otimizada.
9. Limpeza de memoria e timer de precisao sob demanda.
10. Planos de energia, benchmark de DNS, modo MSI de PCIe.
11. Instalacao de 43 programas oficiais.
12. Interface PT-BR e EN, tema escuro.

## Fora de escopo

- Conta, login, nuvem, telemetria, licenca paga.
- Overclock automatico e curva de tensao.
- Android, macOS, Linux.
- Prometer ganho de FPS. O app mede antes e depois quando o usuario pedir.

## Criterio de pronto da versao

- [ ] Build limpo, zero erro e zero aviso, em Debug e Release.
- [ ] Todas as 27 paginas abrem no Windows sem excecao.
- [ ] Todo tweak aplicado tem reversao provada em maquina de teste.
- [ ] Nenhum tweak da lista de bloqueio e executavel por nenhum caminho.
- [ ] Relatorio de auditoria de driver gerado em maquina real.
- [ ] Pendrive de instalacao gerado e boot testado em uma maquina.
- [ ] LEIA-ME, notas de versao e README conferidos.
- [ ] Gabriel autorizou a publicacao.

## Risco

Alto. O produto altera o sistema operacional do usuario. Mitigacao ja embutida: catalogo com risco, backup transacional por tweak e por sessao, ponto de restauracao antes de lote, bloqueio permanente dos comandos destrutivos herdados do acervo original, nenhuma acao destrutiva sem confirmacao escrita na tela.
