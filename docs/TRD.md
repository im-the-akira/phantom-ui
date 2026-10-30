# TRD

## Stack

C# 12, .NET 8, WPF, alvo `net8.0-windows`, x64. Publicacao self-contained em arquivo unico. Sem NuGet de terceiro para o nucleo: apenas BCL, PInvoke e COM. Isso reduz cadeia de suprimentos e mantem o executavel pequeno.

## Camadas

| Camada | Arquivos | Responsabilidade |
| Modelo | `TweakCatalog*.cs`, `TweakEntry`, `TweakOp` | definicao do tweak, risco, deteccao de estado, geracao de comando |
| Transacao | `TweakJournal.cs` | leitura do estado anterior, backup JSONL, aplicacao, reversao, ponto de restauracao |
| Motores | `HardwareAutoOptimizer`, `DirectGameOptimizer`, `BiosAdvisor`, `SystemDoctor`, `DriverAudit`, `DriverManager`, `WindowsIsoBuilder`, `AppInstaller`, `MemoryPurge`, `InputLagTools`, `PowerPlanVault`, `DnsBenchmark` | regra de negocio por assunto |
| Apresentacao | `MainWindow*.cs`, `MainWindow.xaml`, `Theme.xaml` | tela, binding, navegacao, estado visual |
| Infra local | `AppSettings`, `Localization`, `ThemeManager`, `Converters`, `ReportBuilder` | preferencia, idioma, tema, formato |

## Contrato do tweak

`TweakOp` gera uma operacao de registro, servico, comando, remocao ou bcdedit. `TweakEntry` carrega id estavel, secao, risco, tags de hardware, texto PT/EN, funcao de deteccao `Func<TweakStateInfo>` e a lista de operacoes. Id e a chave de backup e de perfil. Id nunca muda depois de publicado.

## Contrato do motor

Todo motor devolve resultado com tres campos: o que foi tentado, o que mudou e o que falhou com motivo. Nenhum motor chama `Environment.Exit`, nenhum escreve direto no registro sem passar pelo `TweakJournal`, nenhum engole excecao sem registrar no log local.

## Regra de compatibilidade

Perfil automatico filtra por tag de hardware antes de aplicar. Tweak de risco alto ou critico nunca entra em perfil. Maquina com anti-cheat ativo bloqueia tweak que mexe em hipervisor, assinatura de driver e integridade de memoria.

## Dependencia de ambiente

WMI, registro, servico, Event Log, `powercfg`, `bcdedit`, `pnputil`, `sc`, `dism`, `oscdimg`, `winget`, `netsh`, COM do Windows Update. Ausente a ferramenta, o motor devolve falha explicada e a tela mostra o motivo com o caminho de correcao. Nunca falha silenciosa.

## Decisao registrada

D001: manter WPF em vez de migrar para WinUI 3 ou Avalonia. Motivo: o codigo herdado ja esta em WPF, migrar custa mais que o ganho e o alvo e Windows x64 apenas. Ver `memoria/HISTORICO.md`.

D002: publicacao self-contained em arquivo unico. Motivo: o publico nao instala runtime e a maquina de destino pode estar sem rede.

D003: catalogo em codigo, nao em JSON externo. Motivo: tipo forte, deteccao compilada e impossibilidade de o usuario injetar comando arbitrario por arquivo de configuracao.

## Divida conhecida

- `WindowsIsoBuilder` depende do ADK para gerar ISO. Sem ADK, so pendrive.
- Sensores de temperatura dependem do driver PawnIO em maquinas sem driver proprio.
- Nao existe teste automatizado de interface. Verificacao de tela e manual, com roteiro em `docs/PLANO.md`.
