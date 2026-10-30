# COORDENACAO

Ler antes de comecar e antes de cada commit.

## Donos de arquivo

| Area | Dono | Arquivos |
| Catalogo de tweaks | agente backend | `codigo/TweakCatalog*.cs` |
| Transacao e reversao | agente backend | `codigo/TweakJournal.cs` |
| Motores | agente backend | motores listados em `docs/TRD.md` |
| Interface WPF | agente frontend | `codigo/MainWindow*.cs`, `codigo/MainWindow.xaml`, `codigo/Theme.xaml` |
| Texto PT/EN | agente criacao | `codigo/Localization.cs`, textos dentro do XAML |
| Documento e memoria | agente produto | `docs/`, `memoria/` |

## Trava de deploy

Nenhuma. Release so com autorizacao do Gabriel. Quem pegar a tarefa escreve a trava aqui e apaga ao terminar.

## Estado dos agentes

| Agente | Sessao | Ultima acao |
| builder | 2026-10-29 | estrutura Nexor e documentacao inicial |

## Recados

- Antes de mexer em XAML, conferir nome de elemento duplicado: dois `x:Name` iguais quebram a compilacao do XAML inteiro.
- Catalogo e a parte mais sensivel. Id publicado nao muda. Risco e lista de bloqueio passam por seguranca.
