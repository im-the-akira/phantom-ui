#!/usr/bin/env python3
"""
Verificador estático do OPTIMIZER X.

Pega a classe de erro que o compilador deixa passar e que só aparece em tempo de execução
no Windows: recurso de XAML inexistente, x:Name usado no code-behind que não existe no XAML,
handler de evento órfão, Tag de navegação sem página e id duplicado no catálogo.

Uso:
    python3 codigo/scripts/verificar-codigo.py            # a partir da raiz do projeto
    python3 codigo/scripts/verificar-codigo.py --quiet     # só o resultado final

Saída: 0 quando não há erro, 1 quando há.
"""
import re
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]          # .../codigo
XAML = RAIZ / "MainWindow.xaml"
TEMA = RAIZ / "Theme.xaml"
CODIGO = RAIZ

erros: list[str] = []
avisos: list[str] = []


def ler(p: Path) -> str:
    return p.read_text(encoding="utf-8", errors="replace")


def strip_comments(texto: str) -> str:
    texto = re.sub(r"<!--.*?-->", "", texto, flags=re.S)
    texto = re.sub(r"/\*.*?\*/", "", texto, flags=re.S)
    texto = re.sub(r"(?m)^\s*//.*$", "", texto)
    return texto


def arquivos_cs() -> list[Path]:
    return [p for p in CODIGO.glob("*.cs") if p.is_file()]


def main() -> int:
    quiet = "--quiet" in sys.argv

    def log(msg: str) -> None:
        if not quiet:
            print(msg)

    if not XAML.exists():
        print(f"FALHA: {XAML} não encontrado")
        return 1

    xaml = strip_comments(ler(XAML))
    tema = strip_comments(ler(TEMA))
    app = strip_comments(ler(RAIZ / "App.xaml")) if (RAIZ / "App.xaml").exists() else ""
    cs = "\n".join(strip_comments(ler(p)) for p in arquivos_cs())

    # ---------------------------------------------------------------- 1. x:Name
    nomes_xaml = re.findall(r'x:Name="([A-Za-z_][A-Za-z0-9_]*)"', xaml)
    dup = {n for n in nomes_xaml if nomes_xaml.count(n) > 1}
    if dup:
        erros.append(f"x:Name duplicado no XAML: {', '.join(sorted(dup))}")
    log(f"x:Name no XAML: {len(nomes_xaml)} únicos: {len(set(nomes_xaml))}")

    # ------------------------------------------------------- 2. handlers de evento
    handlers = set()
    for attr in ("Click", "Checked", "Unchecked", "SelectionChanged", "TextChanged", "SizeChanged",
                 "PasswordChanged", "MouseDown", "MouseUp", "MouseLeftButtonDown", "MouseDoubleClick",
                 "Drop", "DragOver", "KeyDown", "Loaded", "ValueChanged", "Closing", "Closed"):
        handlers |= set(re.findall(rf'\s{attr}="([A-Za-z_][A-Za-z0-9_]*)"', xaml))
    faltando = [h for h in sorted(handlers) if not re.search(rf"\b{re.escape(h)}\s*\(", cs)]
    if faltando:
        erros.append(f"handler citado no XAML e inexistente no C#: {', '.join(faltando)}")
    log(f"handlers no XAML: {len(handlers)}")

    # ----------------------------------------- 3. handlers do C# não citados no XAML
    declarados = set(re.findall(r"private\s+(?:async\s+)?void\s+([A-Za-z0-9_]+)\s*\(\s*object\s+sender", cs))
    # handlers atribuídos por código (ex.: += Metodo) não contam como órfãos
    usados_no_codigo = set(re.findall(r"\+=\s*([A-Za-z0-9_]+)", cs))
    orfaos = sorted(d for d in declarados if d not in handlers and d not in usados_no_codigo)
    if orfaos:
        avisos.append(f"handler declarado e nunca usado no XAML: {', '.join(orfaos)}")

    # ------------------------------------------------------ 4. recursos de XAML
    chaves = set(re.findall(r'x:Key="([A-Za-z0-9_.]+)"', tema + xaml + app))
    usados = set(re.findall(r"\{(?:Static|Dynamic)Resource\s+([A-Za-z0-9_.]+)\}", xaml + tema))
    internos = {"BoolVis"}  # definidos em Theme.xaml antes das chaves
    fantasma = sorted(u for u in usados if u not in chaves | internos)
    if fantasma:
        erros.append(f"recurso citado e inexistente (quebra ao abrir a página): {', '.join(fantasma)}")
    log(f"recursos: {len(chaves)} definidos, {len(usados)} citados")

    # ----------------------------------------------- 5. conversores citados no XAML
    for conv in sorted(set(re.findall(r'Converter=\{StaticResource ([A-Za-z0-9_]+)\}', xaml))):
        if conv not in chaves:
            erros.append(f"conversor '{conv}' citado no XAML e não registrado em Theme.xaml")

    # ------------------------------------------------- 6. navegação Tag x página
    tags = set(re.findall(r'Tag="(Page[A-Za-z]+)"', xaml))
    paginas = set(re.findall(r'x:Name="(Page[A-Za-z]+)"', xaml))
    sem_pagina = sorted(t for t in tags if t not in paginas)
    if sem_pagina:
        erros.append(f"Tag de navegação sem página correspondente: {', '.join(sem_pagina)}")
    sem_tag = sorted(p for p in paginas if p not in tags)
    if sem_tag:
        avisos.append(f"página sem item de navegação: {', '.join(sem_tag)}")

    # ShowPage precisa listar a página, senão ela abre por cima das outras
    m = re.search(r"private void ShowPage\(string name\)\s*\{.*?\}", cs, flags=re.S)
    if m:
        listadas = set(re.findall(r"\bPage[A-Za-z]+\b", m.group(0)))
        fora = sorted(p for p in paginas if p not in listadas)
        if fora:
            erros.append(f"página fora do ShowPage (fica visível junto com as outras): {', '.join(fora)}")
    else:
        erros.append("método ShowPage não encontrado no code-behind")

    # cada página acionada na navegação precisa de um case no switch
    cases = set(re.findall(r'case\s+"(Page[A-Za-z]+)"', cs))
    sem_case = sorted(p for p in paginas if p not in cases)
    if sem_case:
        avisos.append(f"página sem 'case' no switch de abertura (abre vazia): {', '.join(sem_case)}")

    # ------------------------------------------------------- 7. catálogo de tweaks
    ids: list[str] = []
    fabricas = re.findall(r"internal static TweakEntry (\w+)\(", ler(RAIZ / "TweakCatalog.cs"))
    chamada = re.compile(
        r"yield\s+return\s+(?:" + "|".join(map(re.escape, fabricas)) + r")?\(?\s*\"([a-z][a-z0-9\-\.]*)\""
    ) if fabricas else None
    for f in ("TweakCatalog.Os.cs", "TweakCatalog.Perf.cs", "TweakCatalog.Games.cs"):
        p = RAIZ / f
        if not p.exists():
            erros.append(f"arquivo do catálogo ausente: {f}")
            continue
        if chamada is None:
            break
        ids += chamada.findall(ler(p))
    repetidos = {i for i in ids if ids.count(i) > 1}
    if repetidos:
        erros.append(f"id de tweak duplicado: {', '.join(sorted(repetidos))}")
    log(f"tweaks no catálogo: {len(ids)} ({len(set(ids))} únicos)")

    # perfis não podem apontar para id inexistente
    perf = ler(RAIZ / "HardwareAutoOptimizer.cs") if (RAIZ / "HardwareAutoOptimizer.cs").exists() else ""
    if "ProfileTweaks" in perf:
        trecho = perf[perf.index("ProfileTweaks"):]
        citados = set(re.findall(r'"([a-z]+\.[a-z0-9\-\.]+)"', trecho))
        quebrados = sorted(c for c in citados if c not in set(ids))
        if quebrados:
            erros.append(f"perfil aponta para tweak inexistente: {', '.join(quebrados)}")
        log(f"ids citados nos perfis: {len(citados)}")

    # ------------------------------------------------------- 8. comandos perigosos
    perigosos = ["pagingfiles", "TdrLevel", "hypervisorlaunchtype off", "disableelamdrivers",
                 "AGPConcur", "DisableAntiSpyware"]
    for arq in ("TweakCatalog.Os.cs", "TweakCatalog.Perf.cs", "TweakCatalog.Games.cs"):
        p = RAIZ / arq
        if not p.exists():
            continue
        texto = ler(p)
        # comandos perigosos só podem aparecer em descrição de mito bloqueado (seção Blocked)
        linhas_txt = texto.splitlines()
        for cmd in perigosos:
            for linha_no, linha in enumerate(linhas_txt, 1):
                alvo = cmd.lower()
                if cmd == "TdrLevel":
                    # TdrLevel só é mito quando vira ajuste; citação como aviso ("NÃO confunda com TdrLevel=0") é esperada
                    low = linha.lower()
                    if "tdrlevel" not in low:
                        continue
                    if "bloqueado" in low or "blocked" in low or "confunda" in low or "confuse" in low:
                        continue
                elif alvo not in linha.lower():
                    continue
                # janela de 8 linhas em volta: se o bloco é de mito bloqueado, é esperado
                janela = " ".join(linhas_txt[max(0, linha_no - 9):linha_no + 4])
                if "BlockedHazard" in janela or "TweakSection.Blocked" in janela or "⛔" in janela or "BLOQUEADO" in janela:
                    continue
                avisos.append(f"{arq}:{linha_no} cita '{cmd}' fora de contexto de bloqueio, confira")

    # --------------------------------------------------------------- resultado
    print()
    if avisos:
        print(f"AVISOS ({len(avisos)})")
        for a in avisos:
            print("  - " + a)
    if erros:
        print(f"\nERROS ({len(erros)})")
        for e in erros:
            print("  x " + e)
        print("\nRESULTADO: FALHOU")
        return 1
    print("RESULTADO: OK (nenhum erro estático)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
