# PHANTOM UI — documentação completa

Documento único com as seis respostas: o que o projeto antigo tinha, o que o acervo tinha,
o que o software novo tem, o que foi feito, como funciona e o que falta.

Tudo aqui foi contado a partir do código que está no repositório, não de memória.
Onde o número não pôde ser conferido, está escrito.

---

## 1 · O que o projeto antigo tinha (ResetExplode v0.16.0)

Origem: `original/` · 70 arquivos `.cs` (13.971 linhas), 21 telas, licença MIT.

| Área | O que fazia |
|---|---|
| Informações do sistema | Categorias e detalhes de SO, placa-mãe/BIOS, CPU, RAM por módulo, GPU e discos, via WMI. Cartões por unidade com anel de cheio/vazio, tipo e disco físico. |
| Avisos de dispositivo | Toast ao conectar ou remover pendrive, HD externo ou USB qualquer, por `WM_DEVICECHANGE`, sem consulta periódica. |
| Sensores em tempo real | Temperatura, clock, carga, voltagem, ventoinha e consumo via LibreHardwareMonitor. Atualiza a cada 1 s, com filtro por tipo. |
| Placa de vídeo (estilo GPU-Z) | Chip, arquitetura, processo, die, transistores, shaders, Tensor, RT, ROPs, TMUs, fillrates, TFLOPS, memória, Resizable BAR, clocks, PCIe, limites de energia, VBIOS, driver, WHQL, CUDA, DirectX, sensores ao vivo, P-state, PerfCap, NVENC/NVDEC. Fontes: NVML, DXGI, Direct3D 12, WMI, PnP, registro e banco de specs. |
| Stress test de CPU e RAM | Carga em todos os threads, padrões de memória em 40 a 80 por cento do livre, parada automática por temperatura ou erro, gráfico de 5 minutos. |
| Stress de GPU (estilo FurMark) | Torus com pelos por shells em Direct3D 11, overdraw massivo, sem limite de FPS, janela própria, opções de resolução, MSAA, intensidade e limite de temperatura. |
| Jogos | FPS esperado em 32 jogos para a GPU e CPU do usuário, por resolução, gráficos e upscaling. Chave da HowManyFPS opcional, cifrada com DPAPI. |
| Detector de gargalo | Equilíbrio teórico GPU × CPU e leitura ao vivo de carga, VRAM e RAM. |
| Overlay de FPS | Métricas sobre o jogo, incluindo jogo em janela sem borda. |
| Benchmark de CPU, memória, disco | `BenchRenderer`, `MemoryBench`, `DiskBench`, com histórico. |
| Overclock e diagnóstico de erro | NVML para NVIDIA, `OcCheck` lendo WHEA e reinício de driver. |
| RGB | iCUE (Corsair SDK), OpenRGB, Chroma. |
| Portas e Maker | Limpeza de portas COM fantasmas e números presos, drivers seriais CH340, CP2102, FTDI, Prolific e Arduino. |
| Inicialização | Ver, desativar e remover programas de inicialização, com desfazer. |
| Instalar apps | Steam, Chrome, Discord, Lightshot, 7-Zip, WinRAR e VLC pelo winget. |
| Launcher e biblioteca | Detecção de jogos instalados e lançamento com perfil por jogo (prioridade, plano de energia, GPU, FSO, modo de janela, resolução, Hz). |
| Limpeza e otimização básica | `Optimizer.cs` com fechamento de programas e limpeza de RAM; `WindowsTweaks.cs` com 4 ajustes simples. |
| Driver | `DriverManager.cs` com consulta de versão e links de fabricante. |
| Rede | `NetworkTest.cs` com teste de conexão. |

Limites do original: a otimização se resumia a poucos ajustes sem catálogo, sem risco medido, sem
reversão por item, sem BIOS, sem reparo, sem mídia de instalação, sem localização e com 16 avisos
de compilação.

---

## 2 · O que o acervo tinha (pasta `acervo/`)

Origem: 71 arquivos. 36 scripts PowerShell, 15 `.bat`, 7 `.reg`, e o catálogo
`ACERVO-KAIRO-TWEAKS.md` com 13.102 linhas.

| Bloco | Conteúdo |
|---|---|
| Núcleo e motor | `00.Core.ps1`, `90.Motor.ps1`, `95.Menu.ps1`, `Build-Catalog.ps1`, `_emit.ps1` |
| Otimização do Windows | `10.OtimizacaoWindows.ps1`, `65.Correcao.ps1`, `72.Extra.ps1` |
| Jogos | `20.Jogos.ps1`, `22.Games.ps1`, `23.JogosNovos.ps1` |
| Energia | `50.Energia.ps1`, `52.EnergiaAbsorvida.ps1`, `55.PlanosEnergia.ps1`, `5 - Energia Optimizer.7z` |
| Memória | `57.FerramentasMemoria.ps1` |
| Privacidade e limpeza | `60.Privacidade.ps1`, `60.Limpeza.ps1` |
| Windows Update | `45.WindowsUpdate.ps1` |
| Mouse e teclado | `30.MouseTeclado.ps1` |
| Rede | `40.Rede.ps1` |
| MSI e interrupções | `28.AtivadoresMsi.ps1` |
| Absorvidos | `75.Absorvido.ps1` (68 KB), `76.AbsorvidoAltoRisco.ps1`, `77.AbsorvidoExtras.ps1` |
| Diagnóstico | `97.Diagnostico.ps1`, `98.DiagnosticoAcervo.ps1`, `_diag_cats.txt` |
| Curadoria e controle | `CURADORIA.md`, `curadoria.json`, `_verify.ps1`, `_lint.ps1`, `_check.cmd.ps1`, `Dump-Comandos.ps1` |
| Pacote único | `MEU-TWEAKER.cmd`, 14,5 MB, 358 ajustes e 5.880 comandos autocontidos |
| Material de estudo | `ACERVO-KAIRO-TWEAKS.md`: catálogo por categoria, todos os comandos por arquivo, menus do EXM com risco, apêndices de conflitos e inventário |

O acervo ensinou quais chaves existem, o que cada uma faz e, principalmente, quais delas
estragam o Windows. Foi a fonte da lista de bloqueio.

---

## 3 · O que o software novo tem (PHANTOM UI v1.0.0)

Origem: `codigo/` · 86 arquivos `.cs`, 22.695 linhas de C#, 4.239 linhas de XAML, 28 telas.

### Tudo do original, preservado

Os 60 arquivos do projeto antigo continuam no lugar e compilando: sensores, GPU-Z, stress de CPU,
RAM e GPU, benchmarks, overlay, RGB, portas COM, drivers seriais, inicialização, launcher,
limpeza e diagnóstico de erro. Nada foi removido.

### O que é novo

| Recurso novo | O que entrega |
|---|---|
| Catálogo de 212 tweaks | 15 seções: Sistema, Menu, CPU, GPU, RAM, Disco, Input, Rede, Energia, Jogos, Privacidade, IA/Recall, Windows Update, Correções e Mitos Bloqueados. Cada item com descrição PT/EN, comando exato, badge de risco, estado ao vivo e aplicar/reverter. |
| Transação e reversão (`TweakJournal`) | Backup do estado anterior em JSONL, por item e por sessão. Reverter item, reverter selecionados e desfazer sessão. Ponto de restauração antes de lote. |
| Auto-otimizador de hardware | Perfil real da máquina: CPU Intel/AMD com núcleos híbridos, GPU por fornecedor com VRAM, RAM com canais e MHz, disco, notebook ou desktop, anti-cheats instalados, placa-mãe, BIOS. |
| 4 perfis seguros | Balanceado, Competitivo FPS, Estabilidade Pura e Privacidade Máxima. Só entra item compatível e de risco baixo ou médio. |
| Jogos diretos (25 títulos) | Escreve a config nativa de cada jogo com backup `.optimizerx.bak`, GPU dedicada por executável, QoS DSCP 46 e prioridade. Nunca injeta DLL nem toca em anti-cheat. |
| BIOS e placa-mãe | Diagnóstico somente leitura de UEFI, Secure Boot, TPM 2.0, XMP/EXPO, canais, ReBAR, VT-x/SVM e idade da BIOS, com o nome exato da opção por fabricante. |
| Drivers oficiais | Auditoria de genéricos e antigos, atualização pela fonte oficial (NVIDIA API, AMD página do modelo, Intel DSA) e Microsoft WHQL via Windows Update COM. Backup por `pnputil`. |
| Diagnóstico e reparos | 34 ações. Detecta S.M.A.R.T., WHEA, TDR, BSOD, bit sujo, Update, hosts, DNS, espaço, pagefile, MMCSS, hipervisor e SysMain. Executa SFC, DISM, chkdsk, ReTrim, Winsock, caches. |
| Windows pré-otimizado | `autounattend.xml` + `$OEM$` + injeção de drivers, personalização offline do WIM com DISM, pendrive bootável, ISO com `oscdimg` e instalação limpa sem pendrive. |
| RAM e programas | Purga nativa da standby list e do working set, com lista de proteção (anti-cheats, lsass, csrss e afins). Limpeza automática por limite e timer de 0,5 ms só com jogo em primeiro plano. |
| Energia | 34 planos `.pow` embutidos com importar, ativar, listar e voltar ao Equilibrado. |
| Rede | Benchmark de DNS paralelo com troca em um clique, PCIe MSI mode e prioridade de interrupção nativa, bloqueio de FilterKeys. |
| Apps | 43 pacotes oficiais pelo winget, com filtro por categoria e link oficial. |
| Verificação e relatório | Confere o próprio catálogo em execução e gera relatório da máquina sem dado pessoal. |
| Interface | 28 telas em 7 grupos, tema Dark Gamer, PT-BR e EN ao vivo. |

### Números conferidos

| Métrica | Original | PHANTOM UI |
|---|---|---|
| Telas | 21 | 28 |
| Arquivos `.cs` | 70 | 86 |
| Linhas de C# | 13.971 | 22.695 |
| Tweaks catalogados | 4 (em `WindowsTweaks`) | 212 |
| Perfis automáticos | 0 | 4 |
| Jogos com config própria | 0 | 25 |
| Planos de energia embutidos | 0 | 34 |
| Apps instaláveis | 7 | 43 |
| Ações de reparo | 0 | 34 |
| Idiomas | PT | PT e EN |
| Avisos de compilação | 16 | 0 |

---

## 4 · O que foi feito

Ordem real de execução, do começo ao fim do trabalho.

1. **Engenharia reversa do acervo.** Leitura dos 71 arquivos, do catálogo de 13.102 linhas e do pacote `MEU-TWEAKER.cmd`. Resultado: o dossiê em `../DOSSIE_ENGENHARIA_REVERSA_OPTIMIZERX.md` (fora do repositório).
2. **Curadoria de risco.** Separação do que funciona do que é mito. Nove itens destrutivos foram catalogados e bloqueados: pagefile 0, `TdrLevel=0`, `lsass`/`svchost` em Idle, `disableelamdrivers`, `hypervisorlaunchtype off`, `AGPConcur`, apagar Prefetch, apagar WinSxS, MMCSS com afinidade fixa e Defender desligado por registro.
3. **Rebrand e base.** Projeto renomeado, tema Dark Gamer, assets, planos `.pow` embutidos, correção de 34 planos extraídos do `.7z`.
4. **Catálogo em C#.** 212 tweaks em 15 seções, com tipo forte, detecção compilada e comando renderizado. Estado ao vivo lido na máquina.
5. **Transação.** `TweakJournal` com backup JSONL, rollback por item e por sessão, e ponto de restauração.
6. **Motores novos.** Auto-otimizador, jogos diretos, BIOS, drivers oficiais, reparos, mídia de instalação, purga de memória, planos de energia, DNS, PCIe MSI, apps.
7. **Interface.** 10 telas novas ligadas à navegação, com nome único por elemento e todo handler existente.
8. **Localização.** Motor PT/EN com troca ao vivo, sem reiniciar.
9. **Verificação.** Verificador estático próprio, que pega o que o compilador não pega, rodando antes de cada build e no CI.
10. **Limpeza de legado.** Removido o pipeline antigo `WindowsTweaks` e o campo `_tweaks`, que criavam estado duplicado. O dashboard passou a ler o catálogo novo.
11. **Rebrand final para PHANTOM UI.** Namespace, projeto, executável, título, pasta de dados, mutex, classes nativas de janela e User-Agent.
12. **Consolidação no repositório.** Scripts de estudo em `acervo/`, projeto original em `original/`, documentação em `docs/`, memória em `memoria/`.
13. **Publicação.** Repositório público com 401 arquivos, workflow de verificação e de release.

O que foi descartado de propósito: `ativador do windows.7z` (ativação ilegal, sem relação com
desempenho) e `Desabilitar UAC.reg` (desliga o UAC, contra o próprio posicionamento do produto).

---

## 5 · Como funciona

### Estrutura

```
PHANTOM UI
├─ Modelo        TweakCatalog*.cs      definição do tweak, risco, detecção, comando
├─ Transação     TweakJournal.cs       backup, aplicar, reverter, sessão
├─ Motores       HardwareAutoOptimizer, DirectGameOptimizer, BiosAdvisor,
│                SystemDoctor, DriverAudit, DriverManager, WindowsIsoBuilder,
│                AppInstaller, MemoryPurge, InputLagTools, PowerPlanVault, DnsBenchmark
├─ Interface     MainWindow*.cs (14 parciais), MainWindow.xaml, Theme.xaml
└─ Infra local   AppSettings, Localization, ThemeManager, Converters, SupportReport
```

### O caminho de um tweak, do clique ao rollback

1. O usuário abre uma seção. O catálogo renderiza o card com descrição, comando e risco.
2. A função de detecção lê o registro, serviço ou BCD e marca o estado real na máquina.
3. Ao aplicar, o `TweakJournal` lê o valor anterior e grava uma linha JSONL antes de escrever.
4. A operação roda. Se falhar, o registro diz o motivo e o estado volta ao anterior.
5. Reverter usa a linha mais recente daquele id. Desfazer sessão percorre em ordem inversa.
6. Entrada sem valor anterior é marcada como não reversível e nunca entra na fila.

### Regras que o código impõe

- Item de risco alto ou crítico nunca entra em perfil automático.
- Máquina com anti-cheat bloqueia ajuste de hipervisor, assinatura de driver e integridade de memória.
- Tweak de memória conservadora só entra quando a RAM é maior que 12 GB; abaixo disso, compressão de memória fica ligada.
- SysMain nunca é desligado em disco mecânico.
- Prefetch é preservado na limpeza.
- Ferramenta ausente no sistema (ADK, `pnputil`, WMI) devolve falha explicada na tela, nunca falha silenciosa.

### Verificação

Duas camadas, as duas antes do build:

- Compilador: `dotnet build`, com 0 erro e 0 aviso em Debug e Release.
- Verificador próprio: recurso de XAML inexistente, nome duplicado, handler fantasma, página fora do `ShowPage`, id de tweak duplicado, perfil apontando para id inexistente.

Em execução, a tela Verificação repete a checagem do catálogo e a tela Relatório junta o retrato da máquina.

---

## 6 · O que falta fazer

### Bloqueia o lançamento

| Item | Estado | Por quê |
|---|---|---|
| Teste real no Windows | aberto | a interface nunca foi renderizada fora do compilador; WMI, `powercfg`, `pnputil`, `bcdedit`, DISM, `oscdimg` e WU COM nunca rodaram |
| Checklist de 27 itens | a preencher | `docs/CHECKLIST-TESTE-WINDOWS.md` |
| Correção do que o teste apontar | depende do teste | itens críticos primeiro |
| Release com o `.exe` | depende do teste | tag `v1.0.0` publica pelo workflow |

### Recomendado antes de divulgar

| Item | Por quê |
|---|---|
| Assinatura de código | sem assinatura, o SmartScreen assusta no primeiro uso |
| Instalador com desinstalação | o público espera; hoje é executável solto |
| Screenshots e GIF no README | repositório sem imagem converte mal |
| Verificação de binding em tempo de execução | o verificador atual pega estrutura, não binding |

### Ideias registradas, não comprometidas

- Comparar FPS antes e depois do perfil, usando o benchmark que já existe.
- Exportar diagnóstico em PDF único para suporte.
- Tradução para espanhol.
- Perfil por tipo de jogo (competitivo, campanha, emulador).
- Teste automatizado do catálogo em CI com prova de quebra.

### Dívida técnica conhecida

- `WindowsIsoBuilder` depende do ADK para gerar ISO. Sem ADK, sobra o pendrive.
- Sensores de temperatura dependem do PawnIO em máquinas sem driver próprio.
- Não existe teste automatizado de interface. A verificação de tela é manual.
- O histórico do repositório tem dois commits vazios, da criação e remoção de um arquivo de teste de permissão.

### Riscos

| Risco | Mitigação |
|---|---|
| Tweak estraga a máquina do usuário | risco por item, backup transacional, ponto de restauração, lista de bloqueio |
| Anti-cheat derruba o jogo | tweaks sensíveis a anti-cheat marcados e bloqueados na presença deles |
| Driver de fonte errada | só fontes oficiais, sem driver pack |
| Falso positivo do antivírus | executável sem assinatura; orientação no LEIA-ME |
| Medo de aplicar | reverter por item, por seleção e por sessão |

---

## Resumo em uma tabela

| | Antes | Acervo | Agora |
|---|---|---|---|
| Forma | app WPF com diagnóstico e benchmark | scripts PowerShell soltos | app único, self-contained |
| Otimização | 4 ajustes, sem risco medido | 358 ajustes, muitos perigosos | 212 ajustes com risco, estado e reversão |
| Reversão | nenhuma por item | reescrever o original na mão | backup JSONL por item e por sessão |
| Segurança | não avaliada | pagefile 0, TdrLevel 0, UAC off | 9 mitos bloqueados e explicados |
| BIOS, reparo, mídia | não existiam | não existiam | completos |
| Idioma | PT | PT | PT e EN |
| Prova | 16 avisos | nenhuma | 0 aviso e verificador próprio |
