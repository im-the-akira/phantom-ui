# CURADORIA - o que cada tweak faz e o risco de instabilidade

Gerado a partir de _tools\curadoria.json. Integrado ao MEU-TWEAKER.cmd como linhas
individuais e selecionaveis, cada uma com a sua propria reversao.

## Reduzir a quantidade de processos svchost.exe  (instabilidade: Medio)
### Reduce the number of svchost.exe processes  (instability: Medio)

**Para que serve:** Nao para nenhum processo. Svchost.exe e o hospedeiro que roda os servicos do Windows: por padrao, varios servicos ficam no mesmo processo. Esta opcao NAO encerra servico nenhum - ela aumenta a memoria usada como limite de agrupamento, forÃ§ando o Windows a juntar MAIS servicos em cada svchost.exe. Resultado: menos processos svchost.exe na Lista de Tarefas e menos threads.

**What it does:** It does not stop any process. svchost.exe hosts the Windows services: by default several services share one host. This does NOT stop anything - it raises the memory limit used to group them, forcing Windows to pack MORE services into each svchost.exe. Result: fewer svchost.exe processes in Task Manager.

**Nivel de instabilidade:** Medio

**Por que esse nivel:** O Windows 8+ separou os servicos de proposito: assim, um servico com defeito derruba apenas a si mesmo. Juntando tudo de volta, um unico servico defeituoso derruba todos os que dividem o mesmo processo. O acervo usa 9 valores diferentes para isso (4 MB a 128 MB), sem consenso - sinal de que e folklore, nao ajuste medido.

**Why that level:** Windows 8+ separated services on purpose so one faulty service only takes itself down. Packing them back means a single bad service takes down everything sharing that host. The collection uses 9 different values (4 MB to 128 MB) with no consensus - a sign of folklore, not measured tuning.

| # | Item | Instabilidade | Reverte para |
|---|---|---|---|
| 1 | Agrupar servicos em menos processos (64 MB) | Medio | `reg delete "HKLM\SYSTEM\CurrentControlSet\Control" /v "SvcHostSplitThresholdInKB" /f` |

Reinicio necessario: True

## Cache grande de CPU (forcar o nucleo na RAM)  (instabilidade: Alto)
### Large CPU cache (keep the kernel in RAM)  (instability: Alto)

**Para que serve:** O Windows assume, por padrao, que a CPU tem um cache L2 pequeno e pode paginar drivers para o arquivo de paginacao. Estas 4 chaves mandam o Windows assumir um L2 de 256 KB e manter o nucleo sempre na RAM fisica, sem paginar. E o tweak classico de 'big cache'.

**What it does:** By default Windows assumes a small L2 cache and may page drivers to disk. These 4 keys tell it to assume a 256 KB L2 and keep the kernel permanently in physical RAM, never paged out. This is the classic 'big cache' tweak.

**Nivel de instabilidade:** Alto

**Por que esse nivel:** O PROPRIO acervo do usuario classifica como Alto risco, e o ReverterStutter.ps1 lista exatamente este conjunto como causa de travadas. DisablePagingExecutable e o mais perigoso dos quatro: sem RAM suficiente, o sistema trava ou da BSOD. Em maquinas com 4-8 GB o ganho e nenhum e o risco e real.

**Why that level:** The user's own collection rates this High risk, and ReverterStutter.ps1 lists exactly this set as a cause of freezes. DisablePagingExecutive is the most dangerous of the four: with insufficient RAM the system can hang or BSOD. On 4-8 GB machines the gain is nil and the risk is real.

| # | Item | Instabilidade | Reverte para |
|---|---|---|---|
| 1 | Assumir cache L2 de 256 KB | Alto | `reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "SecondLevelDataCache" /f` |
| 2 | Usar o cache do sistema em vez do espaco livre | Alto | `reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "LargeSystemCache" /f` |
| 3 | Nao paginar o nucleo para o disco | Alto | `reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "DisablePagingExecutive" /f` |
| 4 | Limpar o arquivo de paginacao ao desligar | Alto | `reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "ClearPageFileAtShutdown" /f` |

Reinicio necessario: True
