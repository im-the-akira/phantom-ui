# FLUXO DO APP

## Abertura

1. Manifesto pede elevacao. Sem administrador, o app abre em modo leitura: catalogo aparece, botao de aplicar fica desabilitado com o motivo.
2. `AppSettings` carrega idioma, tema e preferencias (limpeza automatica de RAM, limite, timer de jogo).
3. Visao geral roda coleta de hardware em segundo plano e mostra sensor, quando disponivel.
4. Se o driver de sensor faltar, a tela mostra o aviso e o botao que instala o PawnIO.

## Caminho principal (usuario novo)

1. Visao geral: hardware e sensores.
2. Auto-otimizador: analisar hardware, ler avisos, escolher perfil, aplicar. Antes de aplicar, cria ponto de restauracao.
3. Otimizacao manual: por secao, ler risco, aplicar item ou lote, reverter.
4. Jogos diretos: detectar, escolher jogo, otimizar, restaurar arquivo original.
5. Diagnostico e reparos: escanear, aplicar reparo oficial.
6. Windows pre-otimizado: apontar ISO oficial, escolher pendrive ou so ISO, gerar.

## Caminho de reversao

Todo item aplicado entra no journal da sessao. Reverter item, reverter selecionados e desfazer sessao usam o mesmo journal. Sem journal, o botao de reverter nao aparece, evitando promessa falsa.

## Falha e estado vazio

Cada pagina tem quatro estados desenhados: carregando, sem dado, sem resultado de busca, falha com motivo e caminho de correcao. Lista vazia nunca aparece igual para situacoes diferentes.

## Confirmacao

Acao destrutiva (limpar disco, gerar midia, reiniciar para BIOS, instalar driver, remover app) exige segunda confirmacao com o texto do que sera feito. Acao de risco alto no catalogo exige o mesmo.

## Navegacao

Sete grupos: Otimizacao Inteligente, Monitoramento, Jogos e Launcher, Benchmark e Stress, Sistema e Drivers, Portas e Maker, mais a barra fixa (visao geral e configuracoes). Grupo recolhe e lembra o estado.
