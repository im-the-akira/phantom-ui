# =====================================================================
#  Kairo Tweaker :: 52.EnergiaAbsorvida.ps1
#  Setor de ENERGIA completo: todos os planos .pow do acervo como opcoes
#  marcaveis (importados via powercfg - sem risco, o Windows so le o arquivo),
#  + os ajustes powercfg do EXM 1 a 1 (PROCTHROTTLEMIN, IDLESCALING,
#  SYSCOOLPOL, sleep off, e a exclusao dos planos default).
# =====================================================================

Add-KTModule -Modulo 'EnergiaAbsorvida' -Tweaks @(

    @{ Id='pwr.exm.throttlemin'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='PROCTHROTTLEMIN = 100 (EXM): clock minimo da CPU em 100%'
       Explicacao='Origem: EXM (sub_processor PROCTHROTTLEMIN 100). Impede o processador de reduzir a frequencia em idle: sempre no boost minimo. Ja aplicado no seu plano (energia.minimo do perfil antigo).'
       Problema='Consumo de energia e temperatura sobem em idle (30-60W vs 20-30W). Notebook na bateria: duracao cai muito.'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_processor 4d2b0152-7d5c-498b-88e2-34345392a2c5 100'; Rev='powercfg /setacvalueindex scheme_current sub_processor 4d2b0152-7d5c-498b-88e2-34345392a2c5 5' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }
    @{ Id='pwr.exm.idle'; Categoria='Energia e Desempenho'; Risco='Medio'; Reinicio=$false
       Nome='IDLESCALING = 1 + THROTTLING = 0 (EXM): idle states reduzidos'
       Explicacao='Origem: EXM. Reduz os estados de idle (C-states) da CPU e desliga o throttling termico por politica de software. CPU "acorda" mais rapido de estados baixos.'
       Problema='Mais calor e consumo; em alguns sistemas o clock reportado fica estranho em softwares de monitoramento. Throttling 0 NAO desliga protecao fisica termica da CPU.'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_processor 4b92d758-5a24-4851-a470-815d78d4890 1'; Rev='powercfg /setacvalueindex scheme_current sub_processor 4b92d758-5a24-4851-a470-815d78d4890 0' }
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_processor bc5038f7-23e0-4960-96da-33abaf5935ec 0'; Rev='powercfg /setacvalueindex scheme_current sub_processor bc5038f7-23e0-4960-96da-33abaf5935ec 1' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }
    @{ Id='pwr.exm.syscool'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='SYSCOOLPOL = Active (EXM): cooler responde antes'
       Explicacao='Origem: EXM (SYSCOOLPOL 1 = Active). Curva do cooler reage mais agressivamente a temperatura.'
       Problema='Cooler mais ruidoso em carga.'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_processor 94d3a615-a899-4ac5-ae2b-e4d8f634367f 1'; Rev='powercfg /setacvalueindex scheme_current sub_processor 94d3a615-a899-4ac5-ae2b-e4d8f634367f 0' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }
    @{ Id='pwr.exm.sleep'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='Suspensao/hybrid sleep/away mode OFF (EXM)'
       Explicacao='Origem: EXM (ALLOWSTANDBY 0, HYBRIDSLEEP 0, AWAYMODE 0 no plano atual). O PC nao entra em sleep pelas configuracoes do plano.'
       Problema='Voce perde o suspender. Desligue o PC ou deixe ligado. Monitores apagam normalmente.'
       Comandos=@(
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_sleep 29f6c1db-86da-48c5-9fdb-f2b67b1f44da 0'; Rev='powercfg /setacvalueindex scheme_current sub_sleep 29f6c1db-86da-48c5-9fdb-f2b67b1f44da 1800' }
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_sleep 94ac6d29-73ce-41a6-809f-63663ba21b47 0'; Rev='powercfg /setacvalueindex scheme_current sub_sleep 94ac6d29-73ce-41a6-809f-63663ba21b47 1800' }
           @{ Cmd='powercfg /setacvalueindex scheme_current sub_sleep 94d3a615-a899-4ac5-ae2b-e4d8f634367f 0'; Rev='powercfg /setacvalueindex scheme_current sub_sleep 94d3a615-a899-4ac5-ae2b-e4d8f634367f 0' }
           @{ Cmd='powercfg /setactive scheme_current'; Rev='powercfg /setactive scheme_current' }
       )
    }
    @{ Id='pwr.exm.deleteplans'; Categoria='Energia e Desempenho'; Risco='Medio'; Reinicio=$false
       Nome='Excluir planos padrao (Balanced/High/PowerSaver) - EXM'
       Explicacao='Origem: EXM (powercfg -delete dos 3 GUIDs padrao). Deixa so o seu plano de jogo na lista.'
       Problema='Se o plano ativo corromper, nao tera o Balanced para voltar (recrie com powercfg -restoredefaultschemes).'
       Comandos=@(
           @{ Cmd='powercfg -delete 381b4222-f694-41f0-9685-ff5bb260df2e'; Rev='powercfg -restoredefaultschemes' }
           @{ Cmd='powercfg -delete 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c'; Rev='powercfg -restoredefaultschemes' }
           @{ Cmd='powercfg -delete a1841308-3541-4fab-bc81-f71556f20b4a'; Rev='powercfg -restoredefaultschemes' }
       )
    }
)

# ---- Planos .pow da biblioteca do acervo, como opcoes importaveis ----
# (rodando DEPOIS do carregamento do Core: gera 1 tweak por .pow encontrado.
#  Cada plano vira uma ACAO NOMEADA pow.### que importa o arquivo e ativa o
#  GUID retornado: evita pwsh -Command com aspas aninhadas, que quebra o parse.)
function Add-KTPlanosPow {
    $lib = Join-Path $global:KT_PastaAcervo '5 - Energia Optimizer'
    if (-not (Test-Path $lib)) { return }
    $pow = @(Get-ChildItem $lib -Recurse -Filter '*.pow' -File -ErrorAction SilentlyContinue)
    $i = 0
    $tweaks = @()
    foreach ($f in $pow) {
        $i++
        $idAcao  = 'pow.' + $i.ToString('000')
        $caminho = $f.FullName            # capturado em variavel local: o scriptblock abaixo
        $grupo   = $f.Directory.Name      # le estas copias, nao a variavel do loop
        $nome    = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
        $global:KT_Acoes[$idAcao] = {
            param([bool]$Silencioso)
            $saida = (& powercfg.exe /import "$caminho" 2>&1 | Out-String)
            $m = [regex]::Match($saida, '([0-9a-fA-F-]{36})')
            if ($m.Success) {
                [void](& powercfg.exe /setactive $m.Groups[1].Value 2>&1)
                if (-not $Silencioso) { Write-Host ('   Plano importado e ativado (' + $m.Groups[1].Value + ')') -ForegroundColor Green }
            }
            elseif (-not $Silencioso) { Write-Host ('   Falha ao importar: ' + $saida.Trim()) -ForegroundColor Red }
        }.GetNewClosure()
        $tweaks += @{
            Id         = $idAcao
            Categoria  = 'Energia e Desempenho'
            Risco      = 'Baixo'
            Reinicio   = $false
            Nome       = ('Plano .pow: ' + $nome + '  [' + $grupo + ']')
            Explicacao = ('Origem: biblioteca de energia do acervo (5 - Energia Optimizer\\' + $grupo + '). Importa o plano "' + $nome + '" via powercfg -import e ativa. Planos .pow sao arquivos de configuracao lidos pelo Windows: nada e executado de dentro. Grupos: AMD (Ryzen optimizados), Gaming (Amit/EXM/Gaming), OS (Atlas/Revi/ggos/KernelOS), Windows (originais), Intel e Random.')
            Problema   = 'Planos "extreme" forcam clocks/latencias: mais calor. O plano entra duplicado se importar 2x (delete no powercfg.cpl). Ativa na hora: seu PC troca de plano imediatamente.'
            Executar   = $idAcao
        }
    }
    if ($tweaks.Count) { Add-KTModule -Modulo 'PlanosPow' -Tweaks $tweaks }
}
Add-KTPlanosPow
