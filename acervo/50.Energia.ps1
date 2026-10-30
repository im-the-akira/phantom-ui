# =====================================================================
#  Kairo Tweaker :: 50.Energia.ps1
#  ABA 5 - ENERGIA E DESEMPENHO DA CPU
# =====================================================================

Add-KTModule -Modulo 'Energia' -Tweaks @(

    @{ Id='energia.minimo'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='Impedir que a CPU reduza o clock (estado mínimo 100%)'
       Explicacao='Por padrao, o Windows deixa o processador baixar para poucos por cento quando a tela esta ligada e o sistema ocioso, para economizar energia. Isso faz demorar alguns segundos para a CPU voltar a 100% quando voce comeca a jogar. Forcar o estado minimo para 100% mantem o processador sempre na frequencia maxima. Em notebook, aumenta bastante o consumo e o calor; nesse caso prefira o plano Equilibrado.'
       Ganho='CPU ja em frequencia maxima ao comecar a jogar. Nao há ganho em tarefas leves.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c'; Nome='ACSettingIndex'; Tipo='DWord'; Valor='100' }
       )
    }

    @{ Id='energia.boost'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='Usar o boost do fabricante (agressivo da CPU)'
       Explicacao='O Windows tem tres esquemas de energia multinucleo: Conservative, Maximum Performance e o padrao "Automatico". O Automatico reduz o clock em transicoes de carga para poupar energia. O esquema "Maximum Performance" (PERFBOOSTMODE=2) e o modo boost explicito recomendado por AMD e Intel para desempenho maximo, mantendo a CPU no turbo por mais tempo.'
       Ganho='Resposta mais rapida da CPU em picos de carga curta. Mais temperatura e consumo.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\be337238-0d82-4146-a960-4f3749d470c7'; Nome='ACSettingIndex'; Tipo='DWord'; Valor='2' }
       )
    }

    @{ Id='energia.usb'; Categoria='Energia e Desempenho'; Risco='Baixo'; Reinicio=$false
       Nome='Impedir que a USB entre em suspensão'
       Explicacao='A suspensao selectiva da USB economiza pouca energia e faz o dispositivo levar de 1 a 2 segundos para acordar quando voce reconecta algo. Em desktop, desativar evita essa demora. Em notebook, a economia e pequena mas existente.'
       Ganho='Reconexao de dispositivos USB sem demora. Em notebook, aumenta levemente o consumo.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\2a737441-1930-4402-8d77-b2bebba308a3\48e6b7a6-50f5-4782-a5d4-53bb8f07e226'; Nome='ACSettingIndex'; Tipo='DWord'; Valor='0' }
       )
    }

    @{ Id='energia.plano.maximo'; Categoria='Energia e Desempenho'; Risco='Medio'; Reinicio=$false
       Nome='Ativar o plano de energia de máximo desempenho'
       Explicacao='Cria e ativa o plano "Desempenho Maximo", com todos os nucleos da CPU a 100% de forma permanente. E o plano mais rapido para desktop. Em notebook, NAO USE: ele consome a bateria em horas e esquenta muito o processor. O plano Equilibrado ja entrega quase todo o ganho sem esse custo.'
       Ganho='Maximo desempenho de CPU. Em notebook: bateria acaba muito antes e a CPU esquenta mais.'
       Executar='energia.ultimo'
    }
)

# Cria (uma vez) e ativa o plano "Desempenho Maximo" do Windows
# (o esquema oculto e9a42b02-... e duplicado pelo powercfg).
function Invoke-KTEnergiaMaxima {
    param([switch]$Silencioso)
    $guidUltimate = 'e9a42b02-d5df-448d-aa00-03f14749eb61'
    # 1) Se ja existe um plano de desempenho maximo instalado, apenas ativa
    $existente = Get-KTPlanosInstalados | Where-Object { $_.Nome -match 'Ultimate|Desempenho' } | Select-Object -First 1
    if ($existente) { Set-KTPlanoAtivo -Guid $existente.Guid; return $true }
    # 2) Duplica o esquema oculto e ativa a copia criada
    $saida = & powercfg.exe -duplicatescheme $guidUltimate 2>&1 | Out-String
    $m = [regex]::Match($saida, '([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})')
    if (-not $m.Success) {
        Write-KTLog "Nao foi possivel criar o plano de Desempenho Maximo neste Windows." 'ERRO'
        return $false
    }
    Set-KTPlanoAtivo -Guid $m.Groups[1].Value
    return $true
}
