# Novidades da v1.0.0 — PHANTOM UI

Primeira versão com o nome PHANTOM UI. Base: ResetExplode (v0.16) + acervo de tweaks estudado,
reescritos em C# nativo. **Todas** as funções antigas continuam aqui.

## Novas páginas

### AUTO-OTIMIZADOR DE HARDWARE
- Perfil de hardware real: CPU Intel/AMD (núcleos híbridos P+E), GPU NVIDIA/AMD/Intel (VRAM via
  registro do driver, não o WMI saturado em 4 GB), RAM (GB, MHz, canais, DDR4/DDR5), discos
  NVMe/SATA/HDD, notebook x desktop (WMI + bateria), anti-cheats instalados (Vanguard, FACEIT,
  EAC, BattlEye) e placa-mãe/BIOS.
- Diagnóstico com avisos acionáveis: canal único, XMP desligado, ReBAR suportado e off, BIOS
  com mais de 2 anos, Secure Boot/TPM ausentes, RAM <= 12 GB, notebook, anti-cheat presente.
- 4 perfis: **Balanceado**, **Competitivo FPS**, **Estabilidade Pura**, **Privacidade Máxima**.
  Só entram tweaks compatíveis com o hardware e de risco baixo/médio; ponto de restauração
  automático antes de aplicar; um clique para reverter o perfil inteiro.

### OTIMIZAÇÃO MANUAL (212 tweaks)
- 15 seções: Sistema, Menu, CPU, GPU, RAM, Armazenamento, Periféricos/Input Lag, Rede, Energia,
  Jogos/Anti-Cheat, Privacidade, IA/Copilot/Recall, Windows Update, Correções/Anti-Stutter e
  **Mitos Bloqueados por Segurança**.
- Cada card: descrição PT/EN, comando exato (`reg add`, `sc config`, `bcdedit`, `powercfg`...),
  badge de risco, estado ao vivo lido na máquina e botões APLICAR/REVERTER.
- Aplicação em lote com uma sessão transacional e botão DESFAZER SESSÃO (lista de sessões).

### JOGOS DIRETOS (25 títulos)
- Detecta instalação (Steam/Epic/Riot/Battle.net + pastas de config) e escreve a config nativa
  do jogo com backup `.phantomui.bak`: Warzone (`options.*.cod*.cst`: WorkerCount por núcleos
  físicos, VRAMScale, ReBAR), CS2 (`phantomui.cfg`), Fortnite/PUBG/Marvel Rivals/ARC Raiders/
  Delta Force/The Finals/R6 (`GameUserSettings.ini`), FiveM (**fix do MLD**, limpeza de cache),
  Apex/Rust/Dota 2 (`cfg`), Minecraft (flags JVM + G1GC), Tarkov, GTA V, LoL, Overwatch 2,
  Destiny 2, War Thunder, Roblox, Genshin e outros.
- Sempre: GPU dedicada por executável (`UserGpuPreferences`), QoS DSCP 46 e prioridade High.
  Nunca injeta DLL, nunca mexe em anti-cheat.

### BIOS & PLACA-MÃE (somente leitura)
- UEFI x Legacy, Secure Boot, TPM 2.0, XMP/EXPO/DOCP, canais, ReBAR, VT-x/SVM, idade da BIOS.
- Checklist com o nome **exato** da opção para ASUS, MSI, Gigabyte, ASRock e OEM.
- Nenhuma escrita em CMOS/NVRAM. A única ação é `shutdown /r /fw` (com confirmação) para abrir
  o setup. Relatório salvo em `%AppData%\PhantomUI\Reports`.

### DIAGNÓSTICO & CENTRO DE REPAROS
- Detecta: S.M.A.R.T. crítico, WHEA, TDR/Event 4101, BSOD/1001, bit sujo, wuauserv/BITS
  desativados, `hosts` adulterado, DNS quebrado, pouco espaço, RAM esgotada, pagefile 0,
  `TdrLevel=0`, MMCSS com afinidade fixa, hipervisor off com Vanguard/FACEIT, SysMain off em HDD.
- Repara com ferramentas oficiais: `SFC`, `DISM /RestoreHealth` (liberando a política de WU e
  restaurando no fim), `chkdsk /spotfix`, ReTrim, Winsock/TCP-IP, cache de shader/fonte/ícone,
  Bluetooth, Store, NTP, ponto de restauração.

### WINDOWS PRÉ-OTIMIZADO
- `autounattend.xml` completo (windowsPE/specialize/oobeSystem, UEFI + GPT, conta local, pula
  OOBE) + `$OEM$` (`SetupComplete.cmd`, `posinstall.cmd`, LEIA-ME).
- Exportação dos drivers do PC atual com `pnputil /export-driver` e injeção no `install.wim`.
- Personalização **offline** do WIM com `DISM` (mount, remove Appx provisionados, tweaks no hive
  SYSTEM, commit) e conversão de `install.esd` quando necessário.
- Pendrive bootável (limpa, MBR/NTFS, cópia) e ISO com `oscdimg` (ADK oficial) com boot duplo
  BIOS+UEFI.
- Instalação limpa **sem pendrive**: `Mount-DiskImage` + `setup.exe /auto clean` com o
  autounattend, ou `systemreset -cleanpc` — ambos com confirmação explícita.

### VERIFICAÇÃO & RELATÓRIO (novo)
- A própria tela **VERIFICAÇÃO** roda a checagem do catálogo em execução: id duplicado, tweak sem título, perfil apontando para id inexistente, perfil incluindo item de risco alto e comando da lista de mitos fora da seção de bloqueio.
- **GERAR RELATÓRIO** monta um arquivo com hardware, diagnóstico, integridade do catálogo, auditoria de drivers e preferências, salvo em `%AppData%\PhantomUI\Reports\support_<data>.txt`. Sem nome de usuário, sem nome do computador, sem chave.

## Páginas e recursos ampliados
- **Drivers**: auditoria completa (genéricos/antigos), atualização de GPU por fonte oficial
  (NVIDIA API, AMD página do modelo, **Intel DSA**) e WHQL via Windows Update COM com a política
  liberada temporariamente; backup `pnputil`.
- **Apps**: 43 pacotes oficiais via `winget` (runtimes VC++/.NET/DirectX/Java, launchers,
  comunicação, streaming, ferramentas) com filtro por categoria e link oficial.
- **Energia**: os 34 planos `.pow` agora aparecem no app (importar/ativar/listar/voltar ao
  Equilibrado).
- **Rede**: benchmark de DNS paralelo (UDP+ICMP) com troca em um clique, `PCIe MSI Mode` +
  prioridade de interrupção nativo (substitui `MSI_util_v3.exe`) e bloqueio de FilterKeys.
- **RAM & Programas**: purga nativa (`NtSetSystemInformation`) + limpeza automática por limite
  e timer de 0,5 ms apenas com jogo em primeiro plano.

## Interface
- Tema **Dark Gamer Modern** (`#00E5FF` / `#7C4DFF` sobre `#07090F`), badges de risco.
- **PT-BR / EN ao vivo** (botão PT/EN na barra de título, preferência salva).
- Navegação por categorias com novo grupo **OTIMIZAÇÃO INTELIGENTE**.

## Segurança
- `TweakJournal`: backup do estado exato anterior (registro, serviço, tarefa, `bcdedit`) em
  JSONL por tweak e por sessão, com rollback cirúrgico e ponto de restauração.
- Mitos destrutivos do acervo original catalogados e **bloqueados** (pagefile 0, TdrLevel 0,
  lsass/svchost em Idle, disableelamdrivers, AGPConcur, apagar Prefetch/WinSxS, MMCSS 0x0f).

## Mantido do ResetExplode
Monitoramento de sensores, benchmarks (CPU/GPU/memória/disco/RT/jogos), stress test e overclock
(NVML), overlay de FPS, RGB (iCUE/OpenRGB/Chroma), limpeza de USB/portas COM e launcher de jogos.
