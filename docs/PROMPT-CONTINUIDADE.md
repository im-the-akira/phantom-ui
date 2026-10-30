# PROMPT DE CONTINUIDADE — PHANTOM UI

Copie o bloco abaixo inteiro e cole como primeira mensagem para a próxima IA.
Ele foi escrito para funcionar sozinho, sem depender desta conversa.

---

```
Você vai continuar o desenvolvimento do PHANTOM UI. Leia até o fim antes de agir.

## O QUE É O PROJETO

Aplicativo desktop para Windows 10 e 11 (x64) que diagnostica hardware, aplica ajustes de
otimização com risco medido e reversão, configura jogos direto no arquivo de cada um, orienta
a BIOS, audita e atualiza drivers por fonte oficial, repara o Windows e gera mídia de
instalação pré-otimizada.

- Stack: C# 12, .NET 8, WPF, alvo net8.0-windows, publicação self-contained em arquivo único.
- Interface: 28 telas, tema escuro, português do Brasil e inglês com troca ao vivo.
- Repositório: https://github.com/im-the-akira/phantom-ui
- Pasta do projeto: PhantomUI/ (o código fica em PhantomUI/codigo/)

## ESTRUTURA DO REPOSITÓRIO

    PhantomUI/
    ├─ AGENTS.md                  regras do projeto (LEIA PRIMEIRO)
    ├─ CLAUDE.md                  uma linha: @AGENTS.md
    ├─ COMO-TESTAR.txt            passo a passo para compilar e testar no Windows
    ├─ codigo/                    PHANTOM UI v1.0.0, o que evolui
    │   ├─ PhantomUI.csproj
    │   ├─ TweakCatalog*.cs       212 tweaks em 15 seções
    │   ├─ TweakJournal.cs        backup JSONL, aplicar, reverter, sessão
    │   ├─ motores (14)           HardwareAutoOptimizer, DirectGameOptimizer, BiosAdvisor,
    │   │                         SystemDoctor, DriverAudit, WindowsIsoBuilder, MemoryPurge,
    │   │                         InputLagTools, PowerPlanVault, DnsBenchmark, AppInstaller...
    │   ├─ MainWindow*.cs         14 arquivos parciais de interface
    │   ├─ scripts/               build-verificacao.ps1/.sh, verificar-codigo.py, coletar-ambiente.ps1
    │   └─ PowerPlans/            34 planos .pow embutidos
    ├─ original/                  ResetExplode v0.16.0 congelado (MIT, GustavoReset)
    ├─ acervo/                    71 scripts de tweak estudados (material de estudo, NÃO executar)
    ├─ docs/                      PRD, TRD, FLUXO-APP, UI-UX, PLANO, ESQUEMA-BACKEND,
    │                             CHECKLIST-TESTE-WINDOWS, DOCUMENTACAO-COMPLETA
    ├─ memoria/                   IDEIAS, REFINADO, FAZENDO, HISTORICO, COORDENACAO
    └─ projeto/                   CONTEXTO, AGENTES, SKILLS, MCPS

## LEIA NESTA ORDEM, ANTES DE QUALQUER COISA

1. AGENTS.md                                    regras e proibições
2. docs/DOCUMENTACAO-COMPLETA.md                a história inteira em um arquivo
3. docs/CHECKLIST-TESTE-WINDOWS.md              o que ainda não foi provado
4. memoria/FAZENDO.md e memoria/REFINADO.md     o que está aberto
5. docs/TRD.md                                  arquitetura e contratos

Não leia arquivo de código antes disso. Use busca por símbolo em vez de abrir arquivo inteiro.

## COMO O PROJETO FUNCIONA (resumo técnico que você precisa saber)

- TweakEntry: id estável, seção, risco, tags de hardware, texto PT/EN, função de detecção
  (Func<TweakStateInfo>) e lista de TweakOp (RegDword, RegString, RegDel, Svc, Bcd, Cmd).
- TweakJournal: lê o valor anterior, grava linha JSONL em %AppData%\PhantomUI\Backups antes
  de escrever, aplica, e reverte pela linha mais recente daquele id. Sessão reverte em ordem
  inversa. Entrada sem valor anterior é marcada não reversível e nunca entra na fila.
- Perfis automáticos (HardwareAutoOptimizer): filtram por tag de hardware antes de aplicar.
  Risco alto, crítico ou bloqueado NUNCA entra em perfil.
- Id de tweak publicado NÃO MUDA. É a chave do backup e do perfil.
- Toda ferramenta externa ausente (ADK, pnputil, WMI) devolve falha explicada na tela.
  Nunca falha silenciosa, nunca exceção engolida.

## REGRAS DE SEGURANÇA QUE VOCÊ NÃO PODE QUEBRAR

Bloqueados permanentemente, catalogados na seção "Mitos Bloqueados", nunca executáveis por
nenhum caminho:

    pagefile 0 (pagingfiles="c:\pagefile.sys 0 0")
    TdrLevel=0
    lsass / svchost / csrss em Idle ou RealTime
    bcdedit disableelamdrivers yes
    hypervisorlaunchtype off
    AGPConcur (Windows 98)
    apagar C:\Windows\Prefetch
    apagar WinSxS manualmente
    MMCSS Affinity = 0x0f
    Desativar Defender por registro

Regras adicionais:
- Risco alto e crítico nunca aparecem em perfil automático.
- Máquina com anti-cheat (Vanguard, FACEIT, EAC, BattlEye) bloqueia tweak de hipervisor,
  assinatura de driver e integridade de memória.
- Memory compression fica LIGADA em máquina com 12 GB ou menos.
- SysMain nunca desligado em disco mecânico.
- Prefetch preservado na limpeza.
- Toda ação destrutiva pede segunda confirmação com o texto do que será feito.

## COMANDOS DE VERIFICAÇÃO (obrigatórios antes de dizer que terminou)

Build cruzado em Linux ou CI:

    cd PhantomUI/codigo
    python3 scripts/verificar-codigo.py
    dotnet build PhantomUI.csproj -c Release -p:EnableWindowsTargeting=true \
      -p:BaseIntermediateOutputPath=/tmp/obj/ -p:BaseOutputPath=/tmp/bin/ -o /tmp/out/ -v q --nologo

Ou no Windows:

    cd PhantomUI\codigo
    powershell -ExecutionPolicy Bypass -File scripts\build-verificacao.ps1

Critério de pronto de qualquer tarefa de código:
- 0 erro e 0 aviso no build, em Debug e Release
- verificar-codigo.py retorna OK
- se mexeu em XAML: nenhum x:Name duplicado, todo recurso citado existe, todo handler existe
- se mexeu no catálogo: contagem por seção confere, nenhum id duplicado
- se criou motor novo: caminho de falha tratado e mensagem na tela

Cuidado: existe /home/user/.local e /home/user/.cache que podem sumir entre sessões. Se
`dotnet` não existir, reinstale:
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/di.sh && bash /tmp/di.sh --channel 8.0 --install-dir ~/.local/dotnet --no-path

## O QUE ESTÁ ABERTO AGORA (estado real)

R002 · Teste manual no Windows — ABERTO, bloqueia o lançamento
    A interface nunca foi renderizada de verdade. WMI, powercfg, pnputil, bcdedit, DISM,
    oscdimg e Windows Update COM nunca foram executados. Roteiro em
    docs/CHECKLIST-TESTE-WINDOWS.md (27 itens). Só o dono do projeto pode rodar.

R003 · Correção do que o teste apontar — depende do R002

R005 · Release v1.0.0 — depende do R003 e de autorização
    Tag `v*` dispara .github/workflows/publicar.yml, que compila no Windows e publica o
    PhantomUI.exe na aba Releases.

Recomendado antes de divulgar:
    assinatura de código (SmartScreen), instalador com desinstalação, screenshots no README,
    verificação de binding em tempo de execução.

## PRIMEIRA COISA QUE VOCÊ DEVE FAZER

Não escreva código ainda. Responda em no máximo 15 linhas:

1. Confirme que leu AGENTS.md e docs/DOCUMENTACAO-COMPLETA.md, citando uma regra de segurança
   que você não vai quebrar.
2. Diga o estado real do projeto em uma frase (pronto para testar, não validado em execução).
3. Pergunte ao dono o que ele quer nesta sessão, oferecendo no máximo 3 opções concretas,
   entre elas: (a) revisar motores linha a linha procurando premissa de API errada,
   (b) ampliar a verificação estática, (c) trabalhar num item da lista de melhorias.

## ESTILO DE RESPOSTA

- Português do Brasil, direto, frase curta, comece pela resposta.
- Sem travessão, sem emoji, sem ponto e vírgula, sem negrito decorativo em documento.
- Nunca invente resultado. Se não executou, diga "não verificado".
- Prove com build, script ou saída de comando, e cole a evidência.
- Mudança pequena e no escopo. Não refatore nem renomeie o que não foi pedido.
- Mesmo erro três vezes: pare e reporte.
- Formato de retorno: STATUS, MUDANÇA, EVIDÊNCIA, ARQUIVOS, RISCO, PENDENTE.

## ACESSO AO REPOSITÓRIO

O repositório é público para leitura: https://github.com/im-the-akira/phantom-ui
Push exige token do dono. Não peça token por padrão. Se precisar publicar, combine a forma
(o dono roda o push, ou fornece um token fine-grained temporário com Contents: read and write).
```

---

## Notas para quem for usar este prompt

- A última linha do bloco é a mais importante: a IA anterior não tinha credencial e o push
  era sempre uma etapa manual. Deixe isso explícito desde o começo.
- Se a próxima IA rodar em ambiente Linux, ela compila mas não executa a interface. Isso já
  está escrito no prompt para evitar que ela declare "testado e aprovado".
- O commit de referência no momento em que este prompt foi gerado é `2be236e`, com 402
  arquivos no repositório.
