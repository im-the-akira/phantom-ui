# ESQUEMA DOS MOTORES

Nao ha servidor nem banco. O esquema aqui e o contrato dos motores locais e dos arquivos que eles gravam.

## Catalogo

`TweakEntry`: id, secao, risco, tags, texto PT/EN, funcao de deteccao, lista de operacoes.
`TweakStateInfo`: aplicado ou nao, valor atual, motivo quando indisponivel.

## Journal (`%AppData%\PhantomUI\Backups`)

Um arquivo JSONL por dia. Cada linha: data, sessao, id do tweak, tipo de operacao, caminho do alvo, valor anterior, valor novo, resultado.

Reversao le a linha pelo id mais recente da sessao. Sessao inteira reverte em ordem inversa. Entrada sem valor anterior e marcada como nao reversivel e nunca entra na fila de aplicacao.

## Relatorios (`%AppData%\PhantomUI\Reports`)

`drivers_<carimbo>.txt` e `bios_<carimbo>.txt`. Texto puro, sem dado pessoal, com versao do app e do Windows.

## Preferencias (`%AppData%\PhantomUI\settings.json`)

Idioma, tema, limpeza automatica, limite de MB livre, timer de jogo, ultima pagina aberta.

## Midia de instalacao (`%AppData%\PhantomUI\IsoBuild`)

`autounattend.xml`, pasta `$OEM$`, `install.wim` montado e desmontado. Toda montagem com `/Discard` no caminho de falha.

## Contrato de erro

Todo motor devolve `Resultado`: tentado, alterado, falhas com motivo e codigo. A tela mostra o motivo e a acao de correcao. Nada de excecao engolida.
