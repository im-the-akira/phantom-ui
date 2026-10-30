# PHANTOM UI

**Ajuste fino para jogar. Nada que quebra o Windows.**

> **Ajusta o Windows para jogo sem quebrar o sistema. Em português e inglês, com risco medido, estado ao vivo e rollback de verdade.**
> Cada ajuste mostra o que faz, o comando exato, o risco e como desfazer. Os mitos que estragam o Windows ficam bloqueados e explicados.
> Construído em **C# / .NET 8 / WPF**, com base no ResetExplode (MIT) e nos acervos de tweak que estudamos para saber o que NÃO fazer.

<p align="center">
  <b>212 tweaks catalogados · 25 jogos com otimização direta · 34 planos de energia embutidos · 43 apps oficiais · 28 telas</b><br/>
  <sub>PT-BR / EN · tema Dark Gamer · MIT</sub>
</p>

---

## O que é

O **PHANTOM UI** é um único `.exe` (não precisa instalar o .NET) que reúne:

| Área | O que faz |
|---|---|
| **Auto-Otimizador** | Lê CPU (Intel/AMD, núcleos híbridos), GPU (NVIDIA/AMD/Intel, VRAM, ReBAR), RAM (tamanho, MHz, canais), discos (NVMe/SATA/HDD), se é **notebook ou desktop** e **quais anti-cheats** estão instalados. A partir disso aplica 4 perfis (`Balanceado`, `Competitivo FPS`, `Estabilidade Pura`, `Privacidade Máxima`) usando **só** o que é compatível e de risco baixo/médio. |
| **Otimização Manual** | 212 tweaks em 15 seções (OS, Menu, CPU, GPU, RAM, Disco, Periféricos/Input Lag, Rede, Energia, Jogos, Privacidade, IA/Copilot/Recall, Windows Update, Correções/Anti-Stutter, **Mitos Bloqueados**). Cada card mostra **o que faz**, **o comando exato**, **o risco** (Baixo/Médio/Alto/Crítico/Bloqueado) e **o estado ao vivo** na sua máquina — com reversão individual ou de sessão inteira. |
| **Jogos diretos** | Detecta 25 jogos instalados e escreve a config **do próprio jogo** (com backup `.phantomui.bak`), GPU dedicada por executável, QoS DSCP 46 e prioridade High. Nunca injeta DLL e nunca toca em anti-cheat. |
| **BIOS & Placa-mãe** | Diagnóstico **100% somente leitura** (UEFI/Legacy, Secure Boot, TPM 2.0, XMP/EXPO/DOCP, canais de RAM, ReBAR, VT-x/SVM, idade da BIOS) + checklist com o **nome exato da opção** no seu fabricante (ASUS/MSI/Gigabyte/ASRock/OEM) e o link oficial de suporte. |
| **Drivers oficiais** | Auditoria de todos os drivers publicados (genéricos/antigos), atualização de GPU pela fonte do fabricante — NVIDIA (API oficial), AMD (página do modelo), **Intel Driver & Support Assistant** — e **Microsoft WHQL** via Windows Update COM, com a política de WU liberada **temporariamente** e restaurada no fim. `pnputil` backup/restore. Nada de "driver pack". |
| **Diagnóstico & Reparos** | S.M.A.R.T. dos discos, WHEA, TDR (Event 4101), BSOD (1001), bit sujo, wuauserv/BITS, `hosts` adulterado por "debloat", DNS, espaço, pagefile desativado, tweaks perigosos já aplicados. Reparos oficiais: `SFC`, `DISM` inteligente, `chkdsk /spotfix`, ReTrim, Winsock/TCP-IP, cache de fonte/ícone/shader, Bluetooth, Store. |
| **Windows pré-otimizado** | Partindo da **ISO oficial**, gera `autounattend.xml` + `$OEM$` + injeção dos drivers do seu PC (`pnputil`), personaliza o `install.wim` **offline com DISM** e produz **pendrive bootável** e/ou **ISO** (`oscdimg`). Também instala limpo **sem pendrive** (`Mount-DiskImage` + `setup.exe /auto clean`, ou `systemreset -cleanpc`), sempre com confirmação explícita. |
| **RAM & Programas** | Purga nativa da standby list e do working set (`NtSetSystemInformation`), com limpeza automática por limite de RAM livre. Loop de segundo plano que ativa o **timer de 0,5 ms só enquanto um jogo está aberto**. |
| **Energia / Rede** | 34 planos `.pow` embutidos (importar/ativar/reverter), benchmark paralelo de DNS (UDP+ICMP) com troca em um clique, **PCIe MSI Mode + prioridade de interrupção** nativo (substitui o `MSI_util_v3.exe`), FilterKeys bloqueado. |
| **Verificação & relatório** | Confere o próprio catálogo em execução (id duplicado, perfil apontando para tweak inexistente, comando perigoso fora da seção de bloqueio) e gera um relatório da máquina para suporte, sem nome de usuário, sem nome do computador e sem chave. |
| **Apps** | 43 pacotes **oficiais** via `winget` (runtimes VC++/.NET/DirectX/Java, launchers, comunicação, streaming, ferramentas), com filtro por categoria e detecção do que já está instalado. |

E, preservado integralmente do ResetExplode: **monitoramento** (sensores, temperaturas, clocks), **benchmarks** (CPU, GPU, memória, disco, RT, jogos), **stress/overclock** (CPU/GPU/RAM + NVML), **overlay**, **RGB** (iCUE/OpenRGB/Chroma), **USB/portas COM** e **launcher de jogos**.

---

## Regras de segurança embutidas no código

O diferencial não é ter muitos tweaks: é **saber quais não aplicar**. Estes são bloqueados permanentemente (aparecem na seção "Mitos Bloqueados" com o comando original e o motivo):

| Mito do acervo original | Por que está bloqueado |
|---|---|
| `pagingfiles="c:\pagefile.sys 0 0"` | Pico de memória em Warzone/CS2 → `Out of memory` e travamento total. |
| `TdrLevel=0` | Impede o Windows de recuperar o driver de vídeo: tela preta até reiniciar. O correto é `TdrDelay=10`. |
| `lsass` / `svchost` / `csrss` em Idle ou RealTime | Quebra login, autenticação, áudio e mouse. |
| `bcdedit /set disableelamdrivers yes` | Permite rootkit antes do antivírus e derruba Vanguard/FACEIT. |
| `hypervisorlaunchtype off` | Vanguard **exige** virtualização; também quebra WSL2/Hyper-V/Sandbox. |
| `AGPConcur` (Windows 98) | Ruído de pacote copiado: zero efeito em PCIe moderno. |
| Apagar `C:\Windows\Prefetch` | Deixa boot e abertura de jogos lentos por semanas. |
| `MMCSS Affinity = 0x0f` | Prende áudio/agendador em 4 núcleos; stutter em CPUs híbridas. O correto é **remover** a afinidade. |
| Desativar Defender por registro / apagar `WinSxS` | Revertido em minutos pelo Windows / quebra a instalação de forma irreparável. |

E os valores **validados** do acervo Kairo são aplicados como padrão seguro: `Win32PrioritySeparation=38`, `MMCSS GPU Priority=2` + `Priority=2` + `Scheduling Category=Medium`, `GlobalTimerResolutionRequests=0`, `HwSchMode=2`, `SwapEffectUpgradeEnable=1`, `SvcHostSplitThresholdInKB=0x400000`, `LargeSystemCache=0`, `netsh int tcp set global taskoffload=enabled`, Memory Compression **ON** em 8–12 GB, SysMain **nunca** desativado em HDD e Prefetch **preservado** na limpeza.

Todo tweak passa pelo `TweakJournal`: backup do estado exato anterior (registro, serviço, tarefa agendada, `bcdedit`) em JSONL, por tweak e por sessão, em `%AppData%\PhantomUI\Backups`. Rollback é cirúrgico.

---

## Requisitos

- Windows 10 20H2 / Windows 11 (64 bits, x64).
- **Administrador** (o app pede no manifesto; sem ele, as páginas de tweak ficam somente leitura).
- .NET **não** precisa estar instalado — o build publica self-contained.
- Para temperatura/consumo de CPU: driver **PawnIO** (`winget install namazso.PawnIO`) — o app detecta e oferece o botão de correção.
- Para gerar ISO: **Windows ADK – Deployment Tools** (fornece o `oscdimg.exe` oficial). Sem o ADK, o modo pendrive continua funcionando.

## Como compilar

```bash
git clone https://github.com/im-the-akira/PhantomUI.git
cd PhantomUI/codigo              # o codigo vive em codigo/
dotnet build -c Release           # precisa do .NET SDK 8.0
publicar.bat                      # gera o .exe único em ..\temp\dist
```

Atalhos: `codigo/scripts/build-verificacao.ps1` no Windows e `codigo/scripts/build-verificacao.sh` em Linux/CI
(compila cruzado, só prova que fecha: não executa WPF, WMI, powercfg, bcdedit, pnputil, DISM nem oscdimg).

> O projeto segue o padrão do acervo **Nexor**: `AGENTS.md` na raiz, `codigo/` com o repositório,
> `docs/`, `memoria/`, `projeto/`, `handoffs/` e `temp/`.

## Como usar (5 minutos)

1. **Visão geral** → confira sensores e hardware detectado.
2. **Auto-Otimizador** → `ANALISAR HARDWARE` → leia os avisos (RAM em canal único, BIOS antiga, ReBAR off, anti-cheat) → escolha um perfil → `APLICAR PERFIL` (cria ponto de restauração antes).
3. **Otimização manual** → abra a seção que interessa, leia o comando e o risco, aplique um por um. Não gostou? `REVERTER`. Aplicou um lote? `DESFAZER SESSÃO`.
4. **Jogos diretos** → `DETECTAR JOGOS` → `OTIMIZAR` no jogo que você joga hoje.
5. **Diagnóstico & reparos** → `ESCANEAR AGORA` antes de culpar o jogo pelo stutter.
6. **Windows pré-otimizado** → aponte a ISO oficial, escolha o pendrive (ou marque só ISO) e gere a mídia.

## Estrutura

```
PhantomUI/                       raiz do projeto (padrão Nexor)
├─ AGENTS.md                      porta de entrada e regras do projeto
├─ docs/                          PRD, TRD, fluxo, UI/UX, plano, checklist de teste
├─ memoria/                       IDEIAS, REFINADO, FAZENDO, HISTORICO, COORDENACAO
├─ projeto/                       CONTEXTO, AGENTES, SKILLS, MCPS
├─ handoffs/                      passagem de sessão
└─ codigo/                        repositório Git
├─ TweakCatalog*.cs        # 212 tweaks (modelo, risco, seções, detecção ao vivo, comandos)
├─ SupportReport.cs        # verificação do catálogo + relatório de suporte sem dado pessoal
├─ TweakJournal.cs         # backup transacional JSONL + rollback por tweak/sessão + ponto de restauração
├─ HardwareAutoOptimizer.cs# perfil de hardware, filtro por compatibilidade, 4 perfis, checklist de BIOS
├─ DirectGameOptimizer.cs  # 25 jogos: detecção, config nativa com backup, QoS, GPU preference
├─ BiosAdvisor.cs          # diagnóstico UEFI/Secure Boot/TPM/XMP/ReBAR/VT (somente leitura)
├─ SystemDoctor.cs         # detecção de problemas + centro de reparos oficiais
├─ WindowsIsoBuilder.cs    # autounattend + $OEM$ + pnputil + DISM offline + USB/ISO + clean install
├─ DriverManager.cs / DriverAudit.cs  # drivers 100% oficiais (NVIDIA/AMD/Intel + WHQL)
├─ AppInstaller.cs         # 43 pacotes oficiais via winget
├─ PowerPlanVault.cs       # 34 planos .pow embutidos
├─ DnsBenchmark.cs / InputLagTools.cs / MemoryPurge.cs
├─ Theme.xaml / ThemeManager.cs / Localization.cs   # UI Dark Gamer + PT-BR/EN ao vivo
├─ MainWindow*.cs / MainWindow.xaml                 # 15 páginas WPF
└─ PowerPlans/**/*.pow · Drivers/**                # recursos embutidos no executável
```

## Por que "Phantom"

O nome vem do comportamento: o programa ajusta por baixo, sem aparecer na sua frente durante o jogo. Sem overlay obrigatório, sem processo brigando por CPU, sem serviço rodando o tempo todo. Ele trabalha quando você pede e sai da frente.

## Créditos e licença

- **MIT** — veja [LICENSE](LICENSE).
- Base original: **GustavoReset / ResetExplode** (monitoramento, benchmarks, stress/OC, overlay, RGB, USB, launcher) — obrigado por abrir o código.
- Acervo de tweaks: **Kairo / iGust / Atlas Playbook** — usados como *referência de engenharia reversa*; os comandos perigosos foram catalogados, documentados e **bloqueados**.
- O dossiê completo da análise (o que cada script fazia, arquivo por arquivo) está em [DOSSIE_ENGENHARIA_REVERSA_PHANTOMUI.md](../DOSSIE_ENGENHARIA_REVERSA_PHANTOMUI.md).

## Aviso

Otimização mexe no sistema operacional. O PHANTOM UI foi desenhado para ser reversível (backup transacional, ponto de restauração, avisos de risco e bloqueio de mitos destrutivos), mas **você** decide o que aplicar. Faça backup dos seus dados e crie um ponto de restauração antes de qualquer lote grande. Nenhuma ferramenta substitui um SSD decente, dual channel e uma BIOS atualizada.
