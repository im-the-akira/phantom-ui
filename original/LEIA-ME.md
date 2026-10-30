# ORIGINAL — ResetExplode v0.16.0

Cópia integral do projeto original que deu origem ao PHANTOM UI.

- Origem: https://github.com/GustavoReset/ResetExplode
- Autor: Gustavo (GustavoReset)
- Licença: MIT (texto em `LICENSE`, nesta pasta)
- Versão copiada: 0.16.0
- Data da cópia: 2026-10-30

## Por que esta pasta existe

Guarda o ponto de partida em estado puro, para consulta e comparação. O código do PHANTOM UI
está em `../codigo/` e é o que evolui. Aqui não se mexe.

## Relação entre as duas pastas

| | `original/` | `codigo/` |
|---|---|---|
| Nome | RESET EXPLODE | PHANTOM UI |
| Versão | 0.16.0 | 1.0.0 |
| Projeto | `ResetExplode.csproj` | `PhantomUI.csproj` |
| Escopo | diagnóstico, sensores, benchmarks, stress/OC, overlay, RGB, USB, launcher | tudo do original + otimização com rollback, jogos, BIOS, drivers oficiais, reparos, Windows pré-otimizado, 212 tweaks, 34 planos, 43 apps |
| Telas | 18 | 28 |
| Estado | congelado | desenvolvimento |

O PHANTOM UI preserva todas as funções do ResetExplode: monitoramento, benchmarks de CPU, GPU,
memória e disco, benchmarks de jogo e ray tracing, stress test, overclock com NVML, overlay de
FPS, RGB (iCUE, OpenRGB, Chroma), limpeza de USB e portas COM, gerenciador de inicialização e
launcher de jogos.

## Como compilar (se precisar comparar)

```
cd original
dotnet build ResetExplode.csproj -c Release -p:EnableWindowsTargeting=true
```

Verificado em 2026-10-30: compila com 0 erro e 16 avisos, com .NET SDK 8.0.427 e
`EnableWindowsTargeting`. Os avisos são do código original e não foram corrigidos aqui,
de propósito, para a cópia ficar fiel.

## Aviso de licença

A licença MIT do autor original exige manter o aviso de copyright e a permissão em todas as
cópias ou partes substanciais do software. Ela está em `original/LICENSE` e também é citada em
`../codigo/LICENSE` e no README principal. Não remova.
