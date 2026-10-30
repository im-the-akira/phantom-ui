# UI E UX

## Direcao

Painel tecnico de jogo, denso, escuro e rapido de ler. Nao e app de banco, nao e landing page. A pessoa abre, ve o problema em uma linha e tem o botao ao lado.

## Tokens

| Funcao | Valor |
| background | #07090F |
| surface | #0E1220 |
| surface alta | #151B2D |
| text-primary | #E8EDF7 |
| text-secondary | #9AA6BF |
| border | #1E2637 |
| action-primary | #00E5FF |
| action-secondary | #7C4DFF |
| success | #22C55E |
| warning | #F59E0B |
| danger | #EF4444 |
| focus-ring | #00E5FF com 2px de fora |

Estado nunca aparece so por cor. Todo badge de risco tem texto.

## Tipografia

Uma familia (Inter ou Segoe UI Variable no Windows). Escala de quatro papeis: titulo de pagina, titulo de bloco, corpo, legenda. Numero de sensor sempre tabular.

## Densidade e grade

Margem de 24 na pagina, 16 entre blocos, 8 dentro do bloco. Alvo de clique minimo de 44 px na altura. Nada de informacao so no hover, porque o app e usado no teclado e em tela de 1366x768 tambem.

## Componentes e estados

Card de tweak: titulo, descricao, comando, badge de risco, badge de estado, botao aplicar e reverter. Estados: nao aplicado, aplicado, aplicado por outro perfil, indisponivel neste hardware, falhou.

Lista: vazia, carregando, sem resultado, erro, selecionado, desabilitado.

## Acessibilidade

Alvo WCAG 2.2 AA no que se aplica a desktop: contraste, foco visivel, navegacao por teclado em toda acao, rotulo associado a campo, mensagem de erro ligada ao campo, sem animacao essencial, escala de 100 a 150 por cento sem quebrar layout.

## Movimento

Só o util: barra de progresso, pulso de leitura de sensor, transicao de pagina de 120 ms. Sem blur decorativo, sem gradiente roxo automatico, sem card para tudo.

## Texto

Segue `A:\Desenvolvimento\IA\Nexor\equipes\CRIACAO.md`: rotulo curto, erro diz o que houve e como corrigir, botao diz a acao, sem travessao, sem emoji, sem ponto e virgula, sem negrito decorativo.
