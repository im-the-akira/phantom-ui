# CHECKLIST DE TESTE NO WINDOWS

Preencher no Windows. Marcador: Passou, Falhou, N/A, Nao verificado. Nunca deixar em branco.

## Preparacao

- [ ] Maquina de teste, nao de trabalho. Ponto de restauracao criado.
- [ ] .NET SDK 8 instalado para compilar, ou usar o executavel ja publicado.
- [ ] Conta com administrador.
- [ ] Build limpo registrado (comando e saida).

## Abertura e interface

- [ ] App abre elevado, sem excecao na primeira tela.
- [ ] Troca PT/EN aplica em todas as paginas abertas.
- [ ] 27 paginas abrem uma a uma, sem excecao, sem tela em branco.
- [ ] Navegacao por teclado chega em toda acao, foco visivel.
- [ ] Janela em 1366x768 sem corte, sem barra horizontal.

## Catalogo de tweaks

- [ ] Contagem por secao confere com o catalogo.
- [ ] Estado ao vivo de um tweak ja aplicado aparece marcado.
- [ ] Aplicar 3 tweaks de secoes diferentes.
- [ ] Reverter um item aplicado. Conferir no registro que voltou.
- [ ] Aplicar lote e usar desfazer sessao. Conferir reversao completa.
- [ ] Tentar aplicar item da secao de mitos bloqueados. Nao pode executar.

## Auto-otimizador

- [ ] Detectar hardware em maquina Intel e em AMD, se possivel.
- [ ] Perfil aplica so item compativel.
- [ ] Aviso de canal unico, XMP, ReBAR, BIOS aparece quando for o caso.

## Jogos

- [ ] Detectar jogos instalados.
- [ ] Otimizar um jogo. Conferir arquivo `.phantomui.bak`.
- [ ] Restaurar o original e conferir conteudo.

## Memoria e timer

- [ ] Limpeza manual mostra MB liberados e processos pulados.
- [ ] Limpeza automatica respeita o limite configurado.
- [ ] Timer de 0,5 ms liga com jogo aberto e desliga ao fechar.

## Driver, BIOS e reparo

- [ ] Auditoria lista drivers e gera relatorio em `%AppData%\PhantomUI\Reports`.
- [ ] Consulta de driver oficial abre a pagina certa do fabricante.
- [ ] BIOS: diagnostico somente leitura, sem escrever nada. Reinicio para setup so com confirmacao.
- [ ] Escanear diagnostico e rodar um reparo oficial (SFC).

## Windows pre-otimizado

- [ ] Apontar ISO oficial e gerar pendrive em unidade de teste.
- [ ] Boot do pendrive em maquina de teste.
- [ ] Se o ADK estiver ausente, app avisa e nao quebra a pagina.

## Energia, rede e apps

- [ ] Listar planos embutidos, aplicar um e voltar ao Equilibrado.
- [ ] Benchmark de DNS roda e aplica o escolhido.
- [ ] Modo MSI de PCIe lista dispositivo e aplica com confirmacao.
- [ ] Instalar um app oficial pelo winget.
