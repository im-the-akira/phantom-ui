# ACERVO — scripts originais de tweak

Esta pasta guarda os scripts PowerShell, `.reg`, `.bat` e o acervo de tweaks que serviram
de **fonte de engenharia reversa** para o PHANTOM UI. O código do aplicativo em `codigo/`
é C# nativo e não executa nada daqui.

## Estado: arquivo de estudo, não de uso

**Nada nesta pasta deve ser executado em um PC em uso.**

Vários scripts daqui fazem coisas que o PHANTOM UI bloqueia de propósito, e estão catalogados
na tela "Mitos Bloqueados por Segurança" com o motivo de cada um. Exemplos presentes aqui:

| Item | Por que é perigoso |
|---|---|
| `Desabilitar UAC.reg` | Desliga o Controle de Conta de Usuário (`EnableLUA=0`), remove a barreira de elevação do Windows e deixa qualquer programa rodar como administrador sem aviso |
| `76.AbsorvidoAltoRisco.ps1` | Agrupa os ajustes de risco alto do acervo original |
| `pagingfiles` / pagefile 0 | Pico de memória em jogo grande gera travamento total |
| `TdrLevel=0` | Impede o Windows de recuperar o driver de vídeo |
| `lsass` / `svchost` em Idle ou RealTime | Quebra login, áudio e mouse |
| `disableelamdrivers`, `hypervisorlaunchtype off` | Abre espaço para rootkit e derruba Vanguard, FACEIT, WSL2 |
| Apagar `Prefetch` / `WinSxS` | Boot lento por semanas / instalação do Windows irrecuperável |

Os arquivos ficam aqui como **documentação histórica**: para entender de onde veio cada tweak
e por que alguns foram rejeitados. Não são instalador, nem ferramenta de suporte, nem atalho.

## O que foi deixado de fora, de propósito

O arquivo `ativador do windows.7z`, que existia no repositório de origem, **não foi importado**.
Ele não tem relação com otimização de desempenho e ficaria publicamente acessível em um
repositório aberto, sem necessidade. Continua no acervo privado do autor.

## Observação sobre licença

Os scripts desta pasta são de autoria do próprio autor do projeto. O que o PHANTOM UI usa
deles é apenas o **conhecimento**: quais chaves existem, o que cada uma faz e quais não
devem ser tocadas. Nenhum arquivo daqui é distribuído dentro do executável, exceto os
planos de energia `.pow`, que ficam em `codigo/PowerPlans/`.
