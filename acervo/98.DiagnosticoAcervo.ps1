# =====================================================================
#  Kairo Tweaker :: 98.DiagnosticoAcervo.ps1
#  Mostra o que o app encontrou na pasta do acervo (planos e ferramentas)
# =====================================================================

function Show-KTDiagnosticoAcervo {
    Show-KTTitulo
    Write-Host '   INTEGRACAO COM O ACERVO' -ForegroundColor Cyan
    Write-Host ('   ' + ('=' * 66)) -ForegroundColor DarkGray
    Write-Host ("   Aplicativo : $global:KT_Root") -ForegroundColor DarkGray
    Write-Host ("   Acervo     : $global:KT_PastaAcervo") -ForegroundColor DarkGray
    Write-Host ''

    # --- Planos de energia ---
    $lib = @(Get-KTPlanosBiblioteca)
    Write-Host '   PLANOS DE ENERGIA (.pow)' -ForegroundColor Magenta
    if ($lib.Count -eq 0) {
        Write-Host "     Biblioteca nao encontrada em: $global:KT_BibliotecaPow" -ForegroundColor DarkGray
    } else {
        Write-Host ("     {0} planos disponiveis para importar" -f $lib.Count) -ForegroundColor Green
        foreach ($g in ($lib | Group-Object Grupo | Sort-Object Name)) {
            Write-Host ("       {0,-20} {1,2}" -f $g.Name, $g.Count) -ForegroundColor Gray
        }
    }
    $inst = @(Get-KTPlanosInstalados)
    Write-Host ("     {0} planos ja instalados no Windows" -f $inst.Count) -ForegroundColor Gray
    Write-Host ''

    # --- Ferramentas de memoria ---
    Write-Host '   FERRAMENTAS DE MEMORIA DO ACERVO' -ForegroundColor Magenta
    $mem = @(Get-KTFerramentasMemoria)
    if ($mem.Count -eq 0) {
        Write-Host '     Nenhuma encontrada.' -ForegroundColor DarkGray
    } else {
        foreach ($m in $mem) {
            Write-Host ("     {0}  ({1} MB)" -f $m.Nome, $m.KB) -ForegroundColor Gray
            Write-Host ("       assinatura: {0}" -f $m.Assinatura) -ForegroundColor DarkGray
            Write-Host ("       site      : {0}" -f $m.Site) -ForegroundColor DarkGray
        }
        Write-Host '     Sao opcionais: nada e instalado ou executado sem voce pedir.' -ForegroundColor DarkGray
    }
    Write-Host ''
    $totalAbas = @(Get-KTAbasComAcervo).Count
    Write-Host ("   CATALOGO: {0} tweaks em {1} abas" -f $global:KT_Registry.Count, $totalAbas) -ForegroundColor Cyan
    Write-Host ('   ' + ('=' * 66)) -ForegroundColor DarkGray
    Write-Host ''
}