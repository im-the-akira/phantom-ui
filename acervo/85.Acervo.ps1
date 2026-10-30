# =====================================================================
#  Kairo Tweaker :: 85.Acervo.ps1
#  IMPORTA TUDO QUE EXISTE NA PASTA DO ACERVO
#
#  Varre as pastas do acervo (3 - Regedits, 4 - Scripts, 6 - Otimizacao
#  de jogos, 8 - Ativadores etc.) e cria, para cada arquivo
#  .reg / .bat / .cmd / .ps1 / .vbs, um item MARCAVEL na aba
#  correspondente a pasta de primeiro nivel.
#
#  Assim o usuario marca varios itens em varias abas e aplica tudo de
#  uma vez (ou "marcar todas" dentro de cada aba), sem precisar abrir
#  cada arquivo na mao.
# =====================================================================

$global:KT_ExtensoesAcervo = @('.reg', '.bat', '.cmd', '.ps1', '.vbs')

# Titulos amigaveis para as pastas conhecidas do acervo
$global:KT_NomesPastas = @{
    '3 - Regedits'           = 'REGEDITS (arquivos .reg)'
    '4 - Scripts'            = 'SCRIPTS (bat / cmd / ps1)'
    '5 - Energia Optimizer'  = 'ENERGIA OPTIMIZER'
    '6 - Otimização de jogos' = 'OTIMIZAÇÃO DE JOGOS'
    '8 - Ativadores'         = 'ATIVADORES'
}

function Get-KTArquivosAcervo {
    <#  Devolve todos os scripts/reg do acervo, um objeto por arquivo.  #>
    $saida = @()
    if (-not (Test-Path $global:KT_PastaAcervo)) { return $saida }
    foreach ($f in (Get-ChildItem $global:KT_PastaAcervo -Recurse -File -ErrorAction SilentlyContinue)) {
        if ($global:KT_ExtensoesAcervo -notcontains $f.Extension.ToLower()) { continue }
        # Nao importa o proprio app
        if ($f.FullName -like "$global:KT_Root*") { continue }
        $raiz = $f.FullName.Substring($global:KT_PastaAcervo.Length).TrimStart('\').Split('\')[0]
        $titulo = if ($global:KT_NomesPastas.ContainsKey($raiz)) { $global:KT_NomesPastas[$raiz] } else { $raiz.ToUpper() }
        $saida += [pscustomobject]@{
            Arquivo = $f.FullName
            Nome    = $f.BaseName
            Ext     = $f.Extension.ToLower()
            Pasta   = $raiz
            Aba     = "Acervo: $titulo"
            KB      = [math]::Round($f.Length / 1KB, 1)
        }
    }
    return ($saida | Sort-Object Aba, Nome)
}

function Test-KTScriptSuspeito {
    <#  Heuristica simples de conteudo perigoso (apenas para avisar).  #>
    param([string]$Arquivo)
    try {
        $txt = (Get-Content -LiteralPath $Arquivo -TotalCount 120 -ErrorAction Stop) -join ' '
        $padroes = @(
            'DisableAntiSpyware|DisableAntiVirus',
            'Add-MpPreference',
            'certutil\s+-urlcache',
            'Invoke-WebRequest.+OutFile|curl.+-o|--output',
            'schtasks\s+/create', 'net\s+user\s+\S+\s+/add',
            'vssadmin\s+delete\s+shadows', 'wbadmin\s+delete',
            'format\s+[a-zA-Z]:',
            '(del|rd|Remove-Item).{0,40}(C:\\Windows|%windir%|%SystemRoot%)'
        )
        foreach ($p in $padroes) { if ($txt -match $p) { return $true } }
    } catch { }
    return $false
}

function Invoke-KTItemAcervo {
    <#
      .SYNOPSIS  Executa um item do acervo, cobrindo o caminho de reversao.
      .NOTES     - .reg : importado silenciosamente (reg import). Reversao:
                   exporta as chaves antes (arquivo .reg de undo) e importa de volta.
                 - .bat/.cmd/.ps1/.vbs : rodados em janela propria; o que eles
                   fazem e responsabilidade do arquivo (por isso o aviso de risco).
    #>
    param([Parameter(Mandatory)][string]$Arquivo, [switch]$Silencioso)

    $ext = [System.IO.Path]::GetExtension($Arquivo).ToLower()
    try {
        switch ($ext) {
            '.reg' {
                # Reversao: exporta cada chave citada no .reg ANTES de importar,
                # e registra no journal o comando que restaura o estado anterior.
                $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
                $nomeSeguro = ([regex]::Replace([System.IO.Path]::GetFileNameWithoutExtension($Arquivo), '[^\w\-. ]+', '_'))
                if (-not $global:KT_BackupDir) { $global:KT_BackupDir = New-KTBackup -Motivo "reg-import-$nomeSeguro" }

                $chaves = @(Get-Content -LiteralPath $Arquivo -ErrorAction SilentlyContinue |
                    Where-Object { $_ -match '^\s*\[' } |
                    ForEach-Object { $_.Trim().Trim('[', ']') })
                $n = 1
                foreach ($chave in $chaves) {
                    if (-not $chave) { continue }
                    & reg.exe query "$chave" 2>&1 | Out-Null
                    if ($LASTEXITCODE -eq 0) {
                        $undoFile = Join-Path $global:KT_BackupDir ("{0}-{1}-undo-{2}.reg" -f $nomeSeguro, $stamp, $n)
                        $n++
                        $null = & reg.exe export "$chave" "$undoFile" /y 2>&1
                        if (Test-Path $undoFile) {
                            Add-KTJournal -Entry @{ Tipo='Comando'; Path="reg import `"$undoFile`""; Nome=$chave; Existia=$true; TipoValor=''; Valor='' }
                        }
                    } else {
                        # A chave nao existia: reverter = apagar o que o import criar
                        Add-KTJournal -Entry @{ Tipo='Comando'; Path="reg delete `"$chave`" /f"; Nome=$chave; Existia=$false; TipoValor=''; Valor='' }
                    }
                }

                $saida = & reg.exe import "$Arquivo" 2>&1 | Out-String
                if ($LASTEXITCODE -ne 0) {
                    Write-KTLog "Falha ao importar $($Arquivo): $saida" 'ERRO'
                    return 'falhou'
                }
                if (-not $Silencioso) { Write-Host "  [+] $(Split-Path $Arquivo -Leaf): registro importado" -ForegroundColor Green }
                return 'aplicada'
            }
            { $_ -in '.bat', '.cmd' } {
                if (-not $Silencioso) {
                    Write-Host "  [~] Executando: $(Split-Path $Arquivo -Leaf)" -ForegroundColor Yellow
                }
                & cmd.exe /c $Arquivo | Out-Null
                if (-not $Silencioso) { Write-Host "  [+] concluido" -ForegroundColor Green }
                return 'aplicada'
            }
            '.ps1' {
                if (-not $Silencioso) { Write-Host "  [~] Executando: $(Split-Path $Arquivo -Leaf)" -ForegroundColor Yellow }
                & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$Arquivo" | Out-Null
                if (-not $Silencioso) { Write-Host "  [+] concluido" -ForegroundColor Green }
                return 'aplicada'
            }
            '.vbs' {
                if (-not $Silencioso) { Write-Host "  [~] Executando: $(Split-Path $Arquivo -Leaf)" -ForegroundColor Yellow }
                & wscript.exe "$Arquivo" | Out-Null
                return 'aplicada'
            }
            default {
                Write-KTLog "Extensao nao suportada: $ext" 'AVISO'
                return 'falhou'
            }
        }
    } catch {
        Write-KTLog "Falha ao executar $($Arquivo): $($_.Exception.Message)" 'ERRO'
        return 'falhou'
    }
}

# ---------------------------------------------------------------------
#  Catalogo do acervo -> DESATIVADO.
#  O conteudo real de todos os scripts foi ABSORVIDO como tweaks nativos
#  em modules\75.Absorvido.ps1 (abas tematicas, reversivel, sem depender
#  de arquivos externos). Mantenha a varredura desligada para as abas
#  9..28 ("EXM ...", "MINPROCESSOS.PS1", ...) nao reaparecerem.
#  Se um dia quiser executar um arquivo cru de novo, descomente a linha
#  abaixo.
# ---------------------------------------------------------------------
function Add-KTAcervoRegistry {
    $contador = 0
    foreach ($a in (Get-KTArquivosAcervo)) {
        $contador++
        $suspeito = Test-KTScriptSuspeito -Arquivo $a.Arquivo
        $risco = if ($suspeito) { 'Alto' } elseif ($a.Ext -in '.bat', '.cmd', '.ps1', '.vbs') { 'Medio' } else { 'Baixo' }
        $explic = "Executa o arquivo do acervo '$($a.Nome)$($a.Ext)' ($($a.KB) KB)."
        if ($a.Ext -eq '.reg') { $explic += ' Importa as chaves do .reg para o registro do Windows, silenciosamente.' }
        elseif ($a.Ext -eq '.ps1') { $explic += ' Roda o script PowerShell com ExecutionPolicy Bypass.' }
        elseif ($a.Ext -eq '.vbs') { $explic += ' Roda o script Visual Basic.' }
        else { $explic += ' Roda o script em janela de comando.' }
        if ($suspeito) { $explic += ' ATENCAO: conteudo sensivel detectado (toque em registro, tarefas agendadas, downloads ou desligar defesa). Revise o conteudo antes de aplicar.' }

        $id = "acervo.$($a.Pasta.ToLower() -replace '[^a-z0-9]+', '-').$contador"

        Add-KTModule -Modulo 'Acervo' -Tweaks @(
            @{
                Id         = $id
                Categoria  = $a.Aba
                Risco      = $risco
                Reinicio   = $false
                Ativo      = $true
                Nome       = $a.Nome
                Explicacao = $explic
                Ganho      = 'Depende do conteudo do arquivo. Scripts do acervo sao de terceiros: leia antes de aplicar.'
                Comandos   = @()
                AcervoArquivo = $a.Arquivo
                AcervoExt     = $a.Ext
            }
        )
    }
    if ($contador) { Write-KTLog "Acervo importado: $contador itens encontrados." 'OK' -SemTela }
}

# DESATIVADO: conteudo absorvido em 75.Absorvido.ps1 (abas tematicas).
# Add-KTAcervoRegistry
