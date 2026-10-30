# =====================================================================
#  Kairo Tweaker :: 75.Absorvido.ps1  (v2 GRANULAR)
#  Conteudo dos scripts antigos (EXM V6.1/V10/fix, autounattend, RemoveAI,
#  Memphis/Ajustes na memoria, AIMDARK, Pack Fivem, MinProcessos,
#  FinetuneInputLag, OtimizacaoSegura, OtimizarHardware, ReverterStutter,
#  MasterOptimizer) convertido em OPCOES INDIVIDUAIS: 1 valor = 1 tweak.
#  Cada item traz: Explicacao (de onde vem + o que faz) e Problema
#  (efeito colateral real que pode causar). Tudo reversivel pelo app.
# =====================================================================

Add-KTModule -Modulo 'Absorvido2' -Tweaks @(

    # ==================== REDE E INTERNET (EXM completo, 1 a 1) ====================
    @{ Id='net.exm.userport'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='MaxUserPort = 65534 (EXM)'
       Explicacao='Origem: EXM (Tcpip\Parameters). Aumenta de ~5000 para 65534 o numero de portas TCP efemeras disponiveis. Util para quem abre/fecha muitas conexoes rapidas (jogos com matchmaking, P2P, rastreadores).'
       Problema='Praticamente nenhum em desktop. Em servidor pode confundir firewalls antigos.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='MaxUserPort'; Tipo='DWord'; Valor='65534' } )
    }
    @{ Id='net.exm.timewait'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='TcpTimedWaitDelay = 30 (EXM)'
       Explicacao='Origem: EXM. Portas TCP em TIME_WAIT liberam em 30s em vez de 120s. Reconexao rapida a servidores de jogo apos sair da partida.'
       Problema='Nenhum relevante em casa. Teoricamente permite reuso de porta com pacote antigo na rede.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='TcpTimedWaitDelay'; Tipo='DWord'; Valor='30' } )
    }
    @{ Id='net.exm.ttl'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='DefaultTTL = 64 (EXM)'
       Explicacao='Origem: EXM/AIMDARK. TTL padrao de pacotes sai como 64 (estilo Linux). Alguns antimalware/ISP usam TTL para detectar compartilhamento de conexao: muda o valor pode contornar isso.'
       Problema='Pode confundir verificacoes de hotspot do provedor de celular.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'; Nome='DefaultTTL'; Tipo='DWord'; Valor='64' } )
    }
    @{ Id='net.exm.dnsprio'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='Prioridades de resolucao DNS/local/hosts (EXM)'
       Explicacao='Origem: EXM (Tcpip\ServiceProvider: Dns 6, Local 4, Hosts 5, Netbt 7). Prioriza hosts file e DNS sobre NetBIOS na resolucao de nomes.'
       Problema='Nenhum em casa. Em rede corporativa com NetBIOS antigo pode mudar comportamento de compartilhamentos.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider'; Nome='DnsPriority';   Tipo='DWord'; Valor='6' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider'; Nome='LocalPriority'; Tipo='DWord'; Valor='4' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider'; Nome='HostsPriority'; Tipo='DWord'; Valor='5' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider'; Nome='NetbtPriority'; Tipo='DWord'; Valor='7' }
       )
    }
    @{ Id='net.exm.mtu'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$false
       Nome='MTU fixo 1500 em Ethernet e Wi-Fi (EXM)'
       Explicacao='Origem: EXM (netsh interface ipv4/ipv6 set subinterface mtu=1500). Padroniza o MTU para o maximo ethernet, evitando fragmentacao com PPPOE mal configurado.'
       Problema='Se seu ISP usa PPPOE real (1492), pacotes grandes podem fragmentar: ping instavel. Reverta se suspeitar.'
       Comandos=@(
           @{ Cmd='netsh interface ipv4 set subinterface "Ethernet" mtu=1500 store=persistent'; Rev='netsh interface ipv4 set subinterface "Ethernet" mtu=1500 store=persistent' }
           @{ Cmd='netsh interface ipv6 set subinterface "Ethernet" mtu=1500 store=persistent'; Rev='netsh interface ipv6 set subinterface "Ethernet" mtu=1500 store=persistent' }
       )
    }
    @{ Id='net.exm.teredo'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$false
       Nome='Desligar Teredo (EXM)'
       Explicacao='Origem: EXM/AIMDARK. Teredo e tunel IPv6-sobre-IPv4 usado por Xbox Live/UPnP antigos. Desligar reduz superficie de rede e elimina o processo de tunel.'
       Problema='Partidas de jogos Xbox/Microsoft antigos podem nao achar partida (NAT restrito).'
       Comandos=@(
           @{ Cmd='netsh int teredo set state disabled'; Rev='netsh int teredo set state default' }
       )
    }
    @{ Id='net.exm.mld'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='MLD level = all (EXM)'
       Explicacao='Origem: EXM (mldlevel=all). Aceita todo trafego multicast MLD na pilha.'
       Problema='Nenhum conhecido em casa.'
       Comandos=@(
           @{ Cmd='netsh int ip set global mldlevel=all'; Rev='echo padrao restaurado pelo -Reverter do app' }
       )
    }

    # ==================== MEMORIA (Memphis/AIMDARK, filtrados) ====================
    @{ Id='mem.exec'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='DisablePagingExecutive = 1 (Memphis/Ryzen reg)'
       Explicacao='Origem: "Habilitar cache L1 e L2 (Ryzen).reg" + Ajustes na memoria.reg + EXM. Mantem kernel e drivers sempre na RAM fisica (nao paginam para disco).'
       Problema='Consome alguns MB fixos de RAM. Com 16 GB, custo zero. Com 4-8 GB, evite.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='DisablePagingExecutive'; Tipo='DWord'; Valor='1' } )
    }
    @{ Id='mem.sessionview'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='SessionViewSize/SessionPoolSize = 0xC0 (Memphis)'
       Explicacao='Origem: Ajustes na memoria.reg (Memphis). Aumenta memoria de sessao/kernel para servicos por usuario (92 MB -> 192 MB). Ajuda quando muitos apps de usuario graficos abrem juntos.'
       Problema='Reserva kernel fixa maior: em PCs com pouca RAM reduz memoria disponivel. Reverta se o boot ficar lento.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='SessionViewSize'; Tipo='DWord'; Valor='0xc0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='SessionPoolSize'; Tipo='DWord'; Valor='0xc0' }
       )
    }
    @{ Id='mem.iopagelock'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='IoPageLockLimit = 1 MB (Memphis)'
       Explicacao='Origem: Ajustes na memoria.reg. Aumenta bytes travados em RAM para operacoes de I/O (era util em XP; no Win10/11 o Windows gerencia sozinho e ignora parcialmente).'
       Problema='Valor praticamente ignorado pelo Windows moderno. Inofensivo porem sem efeito comprovado.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='IoPageLockLimit'; Tipo='DWord'; Valor='0x100000' } )
    }
    @{ Id='mem.spectre'; Categoria='Otimização do Windows'; Risco='Alto'; Reinicio=$true
       Nome='Desligar mitigacoes Spectre/Meltdown (FeatureSettingsOverride 3) - EXM/Memphis'
       Explicacao='Origem: EXM + Ajustes na memoria.reg (FeatureSettingsOverride=3, Mask=3). Desliga as mitigacoes de hardware contra Spectre/Meltdown: recupera 5-15% de CPU em cargas especificas.'
       Problema='GRAVE DE SEGURANCA: seu PC fica vulneravel a ataques de canal lateral se rodar codigo nao confiavel. Nao use em PC de trabalho/banco. Ganho real em jogos: raramente perceptivel.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='FeatureSettingsOverride';    Tipo='DWord'; Valor='3' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='FeatureSettingsOverrideMask'; Tipo='DWord'; Valor='3' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='FeatureSettings';            Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='mem.cfg'; Categoria='Otimização do Windows'; Risco='Alto'; Reinicio=$true
       Nome='Desligar CFG/ASLR do kernel (EnableCfg=0, MoveImages=0) - EXM'
       Explicacao='Origem: EXM. CFG (Control Flow Guard) e ASLR sao protecoes de memoria contra exploit. Desliga-las reduz alguns ns por chamada de sistema.'
       Problema='GRAVE DE SEGURANCA: facilita exploracao de vulnerabilidades por malware. Ganho em jogo: imperceptivel. So use se aceitar o risco consciente.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='EnableCfg';  Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='MoveImages'; Tipo='DWord'; Valor='0' }
       )
    }

    # ==================== NDU / SIZREQBUF / SERVIDOR (AIMDARK) ====================
    @{ Id='net.ndu'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='Ndu Start = 2 (estado padrao) - AIMDARK reverso'
       Explicacao='Origem: AIMDARK alterava Ndu (Network Data Usage) para reduzir vazamento de pool nao paginado em Win8/8.1. No Win11 o servico e leve. Este item GARANTE o padrao 2 (automatico): util se algum tweak antigo deixou em 4 e apps de monitoramento de rede pararam de funcionar.'
       Problema='Nenhum. E o padrao da Microsoft.'
       Spec=@( @{ Path='HKLM:\SYSTEM\ControlSet001\Services\Ndu'; Nome='Start'; Tipo='DWord'; Valor='2' } )
    }
    @{ Id='net.sizreqbuf'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='SizReqBuf = 17656 (AIMDARK/LanmanServer)'
       Explicacao='Origem: AIMDARK (dword:000044f8). Aumenta buffer de requisicoes do compartilhador de arquivos SMB. Melhorava throughput em copia por rede em Win XP/7.'
       Problema='Mais RAM por sessao SMB. Em gigabit moderno: efeito quase nulo.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters'; Nome='SizReqBuf'; Tipo='DWord'; Valor='17656' } )
    }
    @{ Id='net.irp'; Categoria='Rede e Internet'; Risco='Baixo'; Reinicio=$true
       Nome='IRPStackSize = 20 (EXM/AIMDARK)'
       Explicacao='Origem: EXM/AIMDARK/Diminuir processos reg. Aumenta pacotes de requisicao de I/O do servidor SMB (fila de disco por sessao de rede).'
       Problema='Consumo minimo maior de pool kernel. Nenhum efeito em desktop sem compartilhamento.'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'; Nome='IRPStackSize'; Tipo='DWord'; Valor='0x14' } )
    }

    # ==================== SERVICOS 1 A 1 (Pack Fivem + desabilitar-servicos) ====================
    @{ Id='svc.sysmain'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='SysMain (Superfetch) desligado - bat Fivem'
       Explicacao='Origem: "Desativar o Superfetch_Prefetch.bat". O SysMain pre-carrega apps usados com frequencia na RAM. Em SSD, o pre-carregamento nao compensa e o servico so consome CPU em background.'
       Problema='Primeira abertura de cada programa fica um pouco mais lenta (sem cache). Em HDD NAO desligue (fica pior).'
       Servicos=@( @{ Nome='SysMain'; Inicio='Disabled' } )
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters'; Nome='EnablePrefetcher'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters'; Nome='EnableSuperfetch'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='svc.pcasvc'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='PcaSvc (Assistente de Compatibilidade) desligado - bat Fivem'
       Explicacao='Origem: "Desativar o Servico de Assistente de Compatibilidade". Coleta telemetria de execucao de programas e alimenta o solucionador de compatibilidade.'
       Problema='Solver de compatibilidade do Windows para de sugerir correcoes de jogos antigos.'
       Servicos=@( @{ Nome='PcaSvc'; Inicio='Disabled' } )
    }
    @{ Id='svc.mapsbroker'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='MapsBroker (Download Maps Manager) desligado - reg/Memphis'
       Explicacao='Origem: "Desabilitar Download Maps Manager.reg" + desabilitar-servicos.ps1. Baixa mapas offline do Windows em background.'
       Problema='App Mapas do Windows perde atualizacao offline. Jogos/apps nao dependem dele.'
       Servicos=@( @{ Nome='MapsBroker'; Inicio='Disabled' } )
    }
    @{ Id='svc.wdisystem'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='WdiSystemHost (Diagnostico de Memoria) desligado - bat Fivem'
       Explicacao='Origem: bat Fivem. Roda diagnostico de memoria fisica sob demanda/manutencao.'
       Problema='Relatorios de "Diagnostico de memoria do Windows" nao rodam (use o mdsched manual se precisar).'
       Servicos=@( @{ Nome='WdiSystemHost'; Inicio='Disabled' } )
    }
    @{ Id='svc.wdihost'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='WdiServiceHost (Diagnostico) desligado - EXM'
       Explicacao='Origem: EXM. Host de cenarios de diagnostico do Windows (troubleshooters).'
       Problema='Solucionadores de problemas do Windows (troubleshooters) deixam de funcionar.'
       Servicos=@( @{ Nome='WdiServiceHost'; Inicio='Disabled' } )
    }
    @{ Id='svc.dps'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='DPS (Politica de Diagnostico) desligado - EXM'
       Explicacao='Origem: EXM. Orquestra o WDI e decide cenarios de diagnostico; alimenta o Health Center.'
       Problema='Relatorios de saude/energia automaticos param. Nada quebra para jogos.'
       Servicos=@( @{ Nome='DPS'; Inicio='Disabled' } )
    }
    @{ Id='svc.wersvc'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='WerSvc (Relatorio de Erros) desligado - bat Fivem'
       Explicacao='Origem: bat + EXM. Envia dumps de crash para Microsoft.'
       Problema='Se um jogo crashar, o relatorio nao e enviado (sua analise fica local).'
       Servicos=@( @{ Nome='WerSvc'; Inicio='Disabled' } )
    }
    @{ Id='svc.wsearch'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='WSearch (indexacao de arquivos) desligado - bat Fivem'
       Explicacao='Origem: bat Fivem + desabilitar-servicos.ps1. Indexa nome/conteudo de arquivos continuamente para a busca do menu Iniciar.'
       Problema='Busca por ARQUIVOS fica lenta (busca de apps continua OK). Em HDD recomenda-se ligar.'
       Servicos=@( @{ Nome='WSearch'; Inicio='Disabled' } )
    }
    @{ Id='svc.trkwks'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='TrkWks (Distributed Link Tracking) desligado - desabilitar-servicos'
       Explicacao='Origem: desabilitar-servicos.ps1. Rastreia atalhos quando arquivos se movem entre volumes NTFS.'
       Problema='Atalhos OLE/antigos nao se atualizam sozinhos ao mover arquivo. Uso domestico: irrelevante.'
       Servicos=@( @{ Nome='TrkWks'; Inicio='Disabled' } )
    }
    @{ Id='svc.sharedaccess'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$false
       Nome='SharedAccess (Mobile Hotspot/Ics) desligado - desabilitar-servicos'
       Explicacao='Origem: desabilitar-servicos.ps1. Provem Internet Connection Sharing e Mobile Hotspot.'
       Problema='Voce nao podera compartilhar internet do PC via hotspot. Nao afeta rede normal.'
       Servicos=@( @{ Nome='SharedAccess'; Inicio='Disabled' } )
    }
    @{ Id='svc.wpn'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='WpnService/WpnUserService (Notificacoes push) desligado - MinProcessos'
       Explicacao='Origem: MinProcessos.ps1 (aplicado no PC real). Mata as notificacoes push do Windows (toast) e o servico por usuario que as mantem.'
       Problema='Notificacoes de apps (Discord web, calendario, email) param; apps de jogo podem perder notificacoes.'
       Servicos=@( @{ Nome='WpnService'; Inicio='Disabled' } )
       Spec=@( @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications'; Nome='ToastEnabled'; Tipo='DWord'; Valor='0' } )
    }
    @{ Id='svc.cdp'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='CDPUserSvc/CDPSvc (sincronizacao entre dispositivos) desligado - MinProcessos'
       Explicacao='Origem: MinProcessos.ps1. Continuity/Handshake entre dispositivos Microsoft (Link to Windows, copiar no PC vizinho).'
       Problema='Handoff Android/Link-to-Windows e clipboard na nuvem param.'
       Servicos=@( @{ Nome='CDPUserSvc'; Inicio='Disabled' } )
    }
    @{ Id='svc.onesync'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='OneSyncSvc/PimIndex/UserData/Unistore (sync de contas) desligado - MinProcessos'
       Explicacao='Origem: MinProcessos.ps1 (4 servicos por usuario). Sincroniza Mail/Contatos/Calendario de contas Microsoft locais.'
       Problema='Mail/Calendario/Contatos nativos param de sincronizar (nao afeta Gmail no navegador).'
       Servicos=@(
           @{ Nome='OneSyncSvc'; Inicio='Disabled' }
           @{ Nome='PimIndexMaintenanceSvc'; Inicio='Disabled' }
           @{ Nome='UnistoreSvc'; Inicio='Disabled' }
           @{ Nome='UserDataSvc'; Inicio='Disabled' }
       )
    }
    @{ Id='svc.uso'; Categoria='Windows Update'; Risco='Medio'; Reinicio=$false
       Nome='UsoSvc (Update Session Orchestrator) desligado - MinProcessos'
       Explicacao='Origem: MinProcessos.ps1. Orquestra sessoes do Windows Update (scan/download/instalacao).'
       Problema='O Windows Update para de rodar por conta propria (combina com o bloqueio). Para atualizar manualmente: reative este item.'
       Servicos=@( @{ Nome='UsoSvc'; Inicio='Disabled' } )
    }
    @{ Id='svc.dosvc'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='DoSvc (Delivery Optimization) desligado - MinProcessos'
       Explicacao='Origem: MinProcessos.ps1 + bat "limpar atualizacoes". P2P de updates (baixa da MS e distribui para PCs da rede).'
       Problema='Updates da Microsoft Store tambem usam DoSvc: loja pode ficar lenta para baixar.'
       Servicos=@( @{ Nome='DoSvc'; Inicio='Disabled' } )
    }
    @{ Id='svc.xbox'; Categoria='Jogos'; Risco='Baixo'; Reinicio=$false
       Nome='Xbox (Auth/GameSave/NetApi/Gip) desligado - EXM/desabilitar-servicos'
       Explicacao='Origem: EXM + desabilitar-servicos.ps1 (4 servicos Xbox). Para quem NAO joga via Xbox/Game Pass.'
       Problema='Game Pass/Xbox app/GameBar gravacao param. Alguns jogos UPMC Xbox exigem o Auth: reative se o jogo reclamar.'
       Servicos=@(
           @{ Nome='XblAuthManager'; Inicio='Disabled' }
           @{ Nome='XblGameSave'; Inicio='Disabled' }
           @{ Nome='XboxNetApiSvc'; Inicio='Disabled' }
           @{ Nome='XboxGipSvc'; Inicio='Disabled' }
       )
    }
    @{ Id='svc.hyper'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Hyper-V convidados (HvHost/vmic*) desligado - EXM'
       Explicacao='Origem: EXM (8 servicos vmic). Intermacao com host Hyper-V (quando o PC e maquina virtual). Em PC fisico: 100% inutil.'
       Problema='Se este PC for VM, sincronizacao de tempo/shutdown gracia param. Em PC fisico: nenhum.'
       Servicos=@(
           @{ Nome='HvHost'; Inicio='Disabled' }
           @{ Nome='vmickvpexchange'; Inicio='Disabled' }
           @{ Nome='vmicguestinterface'; Inicio='Disabled' }
           @{ Nome='vmicshutdown'; Inicio='Disabled' }
           @{ Nome='vmicheartbeat'; Inicio='Disabled' }
           @{ Nome='vmicvmsession'; Inicio='Disabled' }
           @{ Nome='vmicrdv'; Inicio='Disabled' }
           @{ Nome='vmictimesync'; Inicio='Disabled' }
       )
    }
    @{ Id='svc.misc'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Servicos variados sem uso (Fax, RetailDemo, RemoteRegistry, Wallet...) - desabilitar-servicos'
       Explicacao='Origem: desabilitar-servicos.ps1 consolidado: Fax, RetailDemo, RemoteRegistry, WMPNetworkSvc, PhoneSvc, SEMgrSvc (NFC), WalletService, lfsvc (geo), SCardSvr/ScDeviceEnum/SCPolicySvc (smartcard), TapiSrv, WbioSrvc (biometria), TabletInputService, WpcMonSvc (controle dos pais), wisvc (Insider), dmwappushservice.'
       Problema='Individualizados: sem fax NFC carteira geolocalizacao biometria touch-insights Insider e controle dos pais. Se usar algum desses (ex: leitor de digitos Windows Hello), desmarque o correspondente antes de aplicar.'
       Servicos=@(
           @{ Nome='Fax'; Inicio='Disabled' }
           @{ Nome='RetailDemo'; Inicio='Disabled' }
           @{ Nome='RemoteRegistry'; Inicio='Disabled' }
           @{ Nome='WMPNetworkSvc'; Inicio='Disabled' }
           @{ Nome='PhoneSvc'; Inicio='Disabled' }
           @{ Nome='SEMgrSvc'; Inicio='Disabled' }
           @{ Nome='WalletService'; Inicio='Disabled' }
           @{ Nome='lfsvc'; Inicio='Disabled' }
           @{ Nome='SCardSvr'; Inicio='Disabled' }
           @{ Nome='ScDeviceEnum'; Inicio='Disabled' }
           @{ Nome='SCPolicySvc'; Inicio='Disabled' }
           @{ Nome='TapiSrv'; Inicio='Disabled' }
           @{ Nome='WbioSrvc'; Inicio='Disabled' }
           @{ Nome='TabletInputService'; Inicio='Disabled' }
           @{ Nome='WpcMonSvc'; Inicio='Disabled' }
           @{ Nome='wisvc'; Inicio='Disabled' }
           @{ Nome='dmwappushservice'; Inicio='Disabled' }
       )
    }

    # ==================== TAREFAS AGENDADAS 1 A 1 (EXM/Fivem) ====================
    @{ Id='task.compat'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Tarefas: Compatibility Appraiser + ProgramDataUpdater + AitAgent (EXM/Fivem)'
       Explicacao='Origem: bat "desativar telemetria" + EXM. CompatTelRunner varre programas/instaladores para "melhorar o Windows" (telemetria pesada de app).'
       Problema='A Windows deixa de sugerir drivers/compatibilidade baseada em uso. Zero impacto em jogos.'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\ProgramDataUpdater" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\ProgramDataUpdater" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\AitAgent" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\AitAgent" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Application Experience\StartupAppTask" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Application Experience\StartupAppTask" /Enable' }
       )
    }
    @{ Id='task.ceip'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Tarefas CEIP completas (Consolidator/BthSQM/Kernel/Usb/Uploader) - EXM'
       Explicacao='Origem: EXM. Customer Experience Improvement Program: coleta e envia dados de uso.'
       Problema='Nenhum. E telemetria opt-out da Microsoft.'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Consolidator" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\BthSQM" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\BthSQM" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Uploader" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Customer Experience Improvement Program\Uploader" /Enable' }
       )
    }
    @{ Id='task.diskdiag'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Tarefas DiskDiagnostic + WinSAT + Autochk Proxy (EXM)'
       Explicacao='Origem: EXM. Diagnostico de disco periodicamente, WinSAT (benchmark de manutencao) e proxy de autochk.'
       Problema='Pontuacao WinSAT desatualizada (afeta recomendacoes da Store, nao jogos).'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Maintenance\WinSAT" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Maintenance\WinSAT" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Autochk\Proxy" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Autochk\Proxy" /Enable' }
       )
    }
    @{ Id='task.misc'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Tarefas: PowerDiagnostics, FamilySafety, CloudHost, DiskFootprint, Sqm, NetTrace, SmartScreen, DeviceInfo (EXM)'
       Explicacao='Origem: EXM. Restante das tarefas diagnosticas/telemetria do V10: AnalyzeSystem, FamilySafety (3), CloudExperienceHost CreateObject, DiskFootprint, PI Sqm-Tasks, GatherNetworkInfo, SmartScreenSpecific, Device Information.'
       Problema='Family Safety (controle parental) e analise de energia automaticas param. SmartScreen continua ativo (so a tarefa especifica para).'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyMonitor" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyMonitor" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyRefresh" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyRefresh" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyUpload" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Shell\FamilySafetyUpload" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\CloudExperienceHost\CreateObjectTask" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\CloudExperienceHost\CreateObjectTask" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\DiskFootprint\Diagnostics" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\DiskFootprint\Diagnostics" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\PI\Sqm-Tasks" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\PI\Sqm-Tasks" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\NetTrace\GatherNetworkInfo" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\NetTrace\GatherNetworkInfo" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\AppID\SmartScreenSpecific" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\AppID\SmartScreenSpecific" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Device Information\Device" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Device Information\Device" /Enable' }
       )
    }
    @{ Id='task.office'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Tarefas Office Telemetry (5) - EXM'
       Explicacao='Origem: EXM. Telemetria do Office 2013/2016 instalado por MSI.'
       Problema='Nenhum se nao usa Office antigo com telemetria.'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Office\OfficeTelemetryAgentFallBack2016" /Disable'; Rev='schtasks /change /tn "\Microsoft\Office\OfficeTelemetryAgentFallBack2016" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Office\OfficeTelemetryAgentLogOn2016" /Disable'; Rev='schtasks /change /tn "\Microsoft\Office\OfficeTelemetryAgentLogOn2016" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Office\OfficeTelemetryAgentLogOn" /Disable'; Rev='schtasks /change /tn "\Microsoft\Office\OfficeTelemetryAgentLogOn" /Enable' }
       )
    }
    @{ Id='task.time'; Categoria='Otimização do Windows'; Risco='Alto'; Reinicio=$false
       Nome='Tarefas de Sincronizacao de horario OFF (EXM)'
       Explicacao='Origem: EXM V10 desligava SynchronizeTime/ForceSynchronizeTime. Mantido aqui com RISCO ALTO porque relogio errado quebra HTTPS/logins/anticheat.'
       Problema='RELOGIO DERRAPA: HTTPS falha, logins falham, anticheat bloqueia. Nao marque a menos que saiba o que esta fazendo.'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Time Synchronization\SynchronizeTime" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Time Synchronization\SynchronizeTime" /Enable' }
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\Time Synchronization\ForceSynchronizeTime" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\Time Synchronization\ForceSynchronizeTime" /Enable' }
       )
    }
    @{ Id='task.wu.autoapp'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='Tarefa: Automatic App Update (updates da LOJA) OFF - EXM'
       Explicacao='Origem: EXM (WindowsUpdate\Automatic App Update). Loja para de atualizar apps automaticamente.'
       Problema='Apps da Store (Netflix, WhatsApp desktop) ficam desatualizados ate voce atualizar na mao.'
       Comandos=@(
           @{ Cmd='schtasks /change /tn "\Microsoft\Windows\WindowsUpdate\Automatic App Update" /Disable'; Rev='schtasks /change /tn "\Microsoft\Windows\WindowsUpdate\Automatic App Update" /Enable' }
       )
    }

    # ==================== PRIVACIDADE 1 A 1 (EXM consolidado) ====================
    @{ Id='priv.telemetry.reg'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='AllowTelemetry = 0 (bat Fivem + EXM)'
       Explicacao='Origem: bat "desativar telemetria" + EXM. Politica oficial: envio de dados diagnostico no nivel minimo possivel (0 = Security em Enterprise, Basic no Home/Pro).'
       Problema='Feedback Hub e alguns relatorios ficam sem dados. Nada funcional.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection'; Nome='AllowTelemetry'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection'; Nome='AllowTelemetry'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.siuf'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Feedback periodic = 0 (bat Fivem/EXM)'
       Explicacao='Origem: bat Fivem. Para os popups "como esta a experiencia do Windows".'
       Problema='Nenhum.'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Siuf\Rules'; Nome='NumberOfSIUFInPeriod'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Siuf\Rules'; Nome='PeriodInNanoSeconds'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.appcompat'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='AIT/AppCompat/Inventory/UART off (EXM)'
       Explicacao='Origem: EXM. Application Impact Telemetry, inventario de programas e telemetria de compatibilidade.'
       Problema='Nenhum funcional.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppCompat'; Nome='AITEnable'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppCompat'; Nome='DisableInventory'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppCompat'; Nome='DisableUAR'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='priv.ceip'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='CEIP off (SQMClient) - EXM'
       Explicacao='Origem: EXM. Customer Experience Improvement (legado, pre-Win10).'
       Problema='Nenhum.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\SQMClient\Windows'; Nome='CEIPEnable'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\SQMClient\Windows'; Nome='CEIPEnable'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.adid.exm'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Advertising ID off (EXM)'
       Explicacao='Origem: EXM. Desliga o ID de publicidade e anuncios personalizados.'
       Problema='Anuncios deixam de ser "relevante" (continuam aparecendo, genericos).'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo'; Nome='DisabledByGroupPolicy'; Tipo='DWord'; Valor='1' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Privacy'; Nome='TailoredExperiencesWithDiagnosticDataEnabled'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.input'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Personalizacao de digito/escrita off (EXM)'
       Explicacao='Origem: EXM. Inking/typing personalization: Windows aprende do seu teclado/escrita e envia para nuvem.'
       Problema='Dicionario pessoal nao sincroniza entre dispositivos.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\InputPersonalization'; Nome='RestrictImplicitTextCollection'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\InputPersonalization'; Nome='RestrictImplicitInkCollection'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\InputPersonalization'; Nome='AllowInputPersonalization'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Personalization\Settings'; Nome='AcceptedPrivacyPolicy'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.location'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Localizacao OFF (bat Fivem + EXM)'
       Explicacao='Origem: bat "desat. localizacao" + EXM. ConsentStore location=Deny + servico lfsvc off + politicas de mapa offline.'
       Problema='Apps de clima/Find My Device/localizacao no Edge param. Nao afeta jogos.'
       Spec=@(
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location'; Nome='Value'; Tipo='String'; Valor='Deny' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors'; Nome='DisableLocation'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Maps'; Nome='AutoDownloadAndUpdateMapData'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Maps'; Nome='AllowUntriggeredNetworkTrafficOnSettingsPage'; Tipo='DWord'; Valor='0' }
       )
       Servicos=@( @{ Nome='lfsvc'; Inicio='Disabled' } )
    }
    @{ Id='priv.sync'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$true
       Nome='SettingSync OFF (todos os grupos) - EXM'
       Explicacao='Origem: EXM (SettingSync completo: 34 chaves consolidadas). Desativa sincronizacao de temas/senhas/idioma/favoritos entre PCs com a conta Microsoft.'
       Problema='Trocar de PC nao traz tema/senhas do Edge automaticamente.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\SettingSync'; Nome='DisableSettingSync'; Tipo='DWord'; Valor='2' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\SettingSync'; Nome='DisableSettingSyncUserOverride'; Tipo='DWord'; Valor='1' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync'; Nome='SyncPolicy'; Tipo='DWord'; Valor='5' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Personalization'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\BrowserSettings'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Credentials'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Language'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\DesktopTheme'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Windows'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.cortana.busca'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$true
       Nome='Cortana/busca web no Iniciar OFF (bat Fivem + EXM)'
       Explicacao='Origem: bat "desativar cortana" + EXM. AllowCortana=0, BingSearchEnabled=0, DisableWebSearch=1, taskkill SearchUI.'
       Problema='Busca do Iniciar so acha apps/arquivos locais (nao busca na internet).'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search'; Nome='AllowCortana'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search'; Nome='DisableWebSearch'; Tipo='DWord'; Valor='1' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Search'; Nome='BingSearchEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Search'; Nome='CortanaConsent'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.wer'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Windows Error Reporting OFF (bat + reg Fivem)'
       Explicacao='Origem: "desat. windows error report.reg" + bat. Chave Disabled=1 + servicos WerSvc/wercplsupport off.'
       Problema='Crashes nao enviam relatorio a Microsoft (debug fica local).'
       Spec=@( @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting'; Nome='Disabled'; Tipo='DWord'; Valor='1' } )
       Servicos=@(
           @{ Nome='WerSvc'; Inicio='Disabled' }
           @{ Nome='wercplsupport'; Inicio='Disabled' }
       )
    }
    @{ Id='priv.devmeta'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Device Metadata/DriverSearching da nuvem OFF (EXM)'
       Explicacao='Origem: EXM. Windows nao busca metadados de dispositivos nem drivers automaticos na nuvem.'
       Problema='Novo periferico pode precisar de driver manual do site do fabricante (igual voce ja prefere).'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata'; Nome='PreventDeviceMetadataFromNetwork'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching'; Nome='SearchOrderConfig'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching'; Nome='DontSearchWindowsUpdate'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='priv.biometry'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Biometria (Windows Hello) off - EXM'
       Explicacao='Origem: EXM (Biometrics Enabled=0 + WbioSrvc off).'
       Problema='Login por digital/rosto desativa (use PIN/senha).'
       Spec=@( @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Biometrics'; Nome='Enabled'; Tipo='DWord'; Valor='0' } )
       Servicos=@( @{ Nome='WbioSrvc'; Inicio='Disabled' } )
    }
    @{ Id='priv.maintenance'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Manutencao automatica OFF (EXM/AIMDARK)'
       Explicacao='Origem: EXM/AIMDARK (MaintenanceDisabled=1). Desativa a janela de Manutencao Automatica (desfragmentacao, varreduras) que roda quando o PC fica ocioso.'
       Problema='HD mecanico NAO desfragmenta mais sozinho: SSD ok, HDD desfragmentar manual 1x/mes.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance'; Nome='MaintenanceDisabled'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'; Nome='NoLazyMode'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='priv.wifisense'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='WiFi Sense/hotspot reporting off (EXM)'
       Explicacao='Origem: EXM. Nao compartilha credenciais de Wi-Fi com contatos e nao reporta redes.'
       Problema='Nenhum em casa.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\PolicyManager\default\WiFi'; Nome='AllowWiFiHotSpotReporting'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\PolicyManager\default\WiFi'; Nome='AllowAutoConnectToWiFiSenseHotspots'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config'; Nome='AutoConnectAllowedOEM'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.content'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$false
       Nome='Conteudo patrocinado/sugestoes/lockscreen dinamico OFF (EXM + unattend)'
       Explicacao='Origem: EXM + autounattend (ContentDeliveryManager completo: 17 chaves do DefaultUser + SoftLanding + SilentInstalled + SubscribedContent 310093/338387/338388/338389/338393/353694/353696/353698/88000326).'
       Problema='Nenhum. Remove sugestoes de apps e fundo de tela dinamico.'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-310093Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-338387Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-338388Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-338389Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-338393Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-353694Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-353696Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-353698Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SubscribedContent-88000326Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SilentInstalledAppsEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SystemPaneSuggestionsEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='PreInstalledAppsEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='OemPreInstalledAppsEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='SoftLandingEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='RotatingLockScreenEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'; Nome='RotatingLockScreenOverlayEnabled'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='priv.copilot'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$true
       Nome='Copilot/Recall/IA off (unattend + RemoveAI)'
       Explicacao='Origem: autounattend (TurnOffWindowsCopilot, RemoveCopilot, RemoveRecall) + RemoveAI-UpdateCleanup (familia WindowsWorkload.* e pacotes AIX/CoreAI/Provider). Politicas + remocao dos pacotes da conta atual.'
       Problema='Copilot/Recall indisponiveis (podem voltar num feature update: rode de novo).'
       Spec=@(
           @{ Path='HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot'; Nome='TurnOffWindowsCopilot'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot'; Nome='TurnOffWindowsCopilot'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\AIRecall'; Nome='DisableAIDataAnalysis'; Tipo='DWord'; Valor='1' }
       )
       Comandos=@(
           @{ Cmd='powershell -NoProfile -Command "Get-AppxPackage -Name ''Microsoft.Windows.Ai.Copilot.Provider'' -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue; Get-AppxPackage -Name ''Microsoft.Copilot'' -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue; Get-AppxPackage -Name ''MicrosoftWindows.Client.CoreAI'' -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue"'; Rev='echo Reinstale pela Store se quiser o Copilot de volta' }
       )
    }

    # ==================== UX/DESKTOP 1 A 1 (EXM + AIMDARK) ====================
    @{ Id='ux.menudelay'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='MenuShowDelay = 0 (AIMDARK/EXM)'
       Explicacao='Origem: AIMDARK (MenuShowDelay=0/dword) + EXM. Menus abrem instantaneo (padrao 400ms).'
       Problema='Nenhum. Apenas estetica "seca".'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Desktop'; Nome='MenuShowDelay'; Tipo='String'; Valor='0' }
       )
    }
    @{ Id='ux.animate'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Animacoes OFF (MinAnimate/TaskbarAnimations/VisualFX) - EXM'
       Explicacao='Origem: EXM. Minimize/maximize sem animacao, taskbar sem highlight animado, conjunto "melhor desempenho".'
       Problema='Interface menos "bonita"; nenhuma perda funcional.'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Desktop\WindowMetrics'; Nome='MinAnimate'; Tipo='String'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects'; Nome='VisualFXSetting'; Tipo='DWord'; Valor='2' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Nome='TaskbarAnimations'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='ux.serialize'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='StartupDelayInMSec = 0 (EXM/AIMDARK)'
       Explicacao='Origem: EXM/AIMDARK. Remove o atraso artificial de 120s para apps de startup rodarem apos logon.'
       Problema='Varios apps abrem ao mesmo tempo no logon (com auto-start limpo, beneficio maximo).'
       Spec=@( @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Serialize'; Nome='StartupDelayInMSec'; Tipo='DWord'; Valor='0' } )
    }
    @{ Id='ux.hover'; Categoria='Mouse e Teclado'; Risco='Baixo'; Reinicio=$false
       Nome='MouseHoverTime = 10 (EXM/AIMDARK)'
       Explicacao='Origem: EXM/AIMDARK. Tooltip aparece em 10ms (padrao 400ms).'
       Problema='Nenhum.'
       Spec=@( @{ Path='HKCU:\Control Panel\Mouse'; Nome='MouseHoverTime'; Tipo='String'; Valor='10' } )
    }
    @{ Id='ux.icons'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Desktop:Este PC/Painel/Recycle visiveis, OneDrive/Rede/Controle ocultos (unattend)'
       Explicacao='Origem: autounattend (UserOnce HideDesktopIcons). Mostra Este PC, Painel de Controle e Lixeira no desktop; esconde Redes, OneDrive, Usuarios.'
       Problema='Preferencia estetica. Reverta se quiser o layout padrao.'
       Comandos=@(
           @{ Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{20d04fe0-3aea-1069-a2d8-08002b30309d}" /t REG_DWORD /d 0 /f'; Rev='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{20d04fe0-3aea-1069-a2d8-08002b30309d}" /t REG_DWORD /d 1 /f' }
           @{ Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{645ff040-5081-101b-9f08-00aa002f954e}" /t REG_DWORD /d 0 /f'; Rev='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{645ff040-5081-101b-9f08-00aa002f954e}" /t REG_DWORD /d 1 /f' }
           @{ Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{5399e694-6ce5-4d6c-8fce-1d8870fdcba0}" /t REG_DWORD /d 0 /f'; Rev='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{5399e694-6ce5-4d6c-8fce-1d8870fdcba0}" /t REG_DWORD /d 1 /f' }
           @{ Cmd='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{59031a47-3f72-44a7-89c5-5595fe6b30ee}" /t REG_DWORD /d 1 /f'; Rev='reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{59031a47-3f72-44a7-89c5-5595fe6b30ee}" /t REG_DWORD /d 0 /f' }
       )
    }
    @{ Id='ux.sounds'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Sons do sistema OFF (unattend: Schemes .None)'
       Explicacao='Origem: autounattend (UserOnce). Schemes=.None silencia eventos do Windows.'
       Problema='Sem sons de login/notificacao (apps/jogos continuam com som).'
       Spec=@( @{ Path='HKCU:\AppEvents\Schemes'; Nome='(Default)'; Tipo='String'; Valor='.None' } )
    }
    @{ Id='ux.dark'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Tema escuro + transparencia OFF (unattend: SetColorTheme)'
       Explicacao='Origem: autounattend (SystemUsesLightTheme=0, AppsUseLightTheme=0, EnableTransparency=0). Dark mode e sem blur de acrilico (o blur custa GPU na composicao).'
       Problema='Transparencia sumindo deixa barras "chapadas".'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'; Nome='SystemUsesLightTheme'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'; Nome='AppsUseLightTheme'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'; Nome='EnableTransparency'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='ux.searchbox'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Caixa de busca da taskbar oculta (unattend)'
       Explicacao='Origem: autounattend (SearchboxTaskbarMode=0).'
       Problema='Nenhum (Win+S continua funcionando).'
       Spec=@( @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Search'; Nome='SearchboxTaskbarMode'; Tipo='DWord'; Valor='0' } )
    }
    @{ Id='ux.endtask'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='"Finalizar tarefa" no clique direito da taskbar (unattend)'
       Explicacao='Origem: autounattend (TaskbarEndTask=1). Habilita matar app direto da barra (Win11 23H2+).'
       Problema='Nenhum.'
       Spec=@( @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings'; Nome='TaskbarEndTask'; Tipo='DWord'; Valor='1' } )
    }
    @{ Id='ux.explorer'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Explorer: extensoes visiveis, arquivos ocultos, super-ocultos (unattend)'
       Explicacao='Origem: autounattend (HideFileExt=0, Hidden=1, ShowSuperHidden=1).'
       Problema='Voce vera arquivos de sistema: cuidado ao apagar.'
       Spec=@(
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Nome='HideFileExt'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Nome='Hidden'; Tipo='DWord'; Valor='1' }
           @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Nome='ShowSuperHidden'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='ux.notepad'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Notepad sem banner da Store (unattend)'
       Explicacao='Origem: autounattend (ShowStoreBanner=0).'
       Problema='Nenhum.'
       Spec=@( @{ Path='HKCU:\Software\Microsoft\Notepad'; Nome='ShowStoreBanner'; Tipo='DWord'; Valor='0' } )
    }
    @{ Id='ux.taskview'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Botao Task View da taskbar oculto (unattend/EXM)'
       Explicacao='Origem: autounattend (ShowTaskViewButton=0).'
       Problema='Nenhum (Win+Tab continua).'
       Spec=@( @{ Path='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'; Nome='ShowTaskViewButton'; Tipo='DWord'; Valor='0' } )
    }
    @{ Id='ux.acess'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Teclas de acessibilidade (Sticky/Toggle/MouseKeys) OFF (AIMDARK/EXM)'
       Explicacao='Origem: AIMDARK (Flags 506/58/58) + EXM. Evita o popup de StickyKeys quando aperta Shift 5x.'
       Problema='Quem USA teclas de adesao por acessibilidade perde o atalho.'
       Spec=@(
           @{ Path='HKCU:\Control Panel\Accessibility\StickyKeys'; Nome='Flags'; Tipo='DWord'; Valor='506' }
           @{ Path='HKCU:\Control Panel\Accessibility\ToggleKeys'; Nome='Flags'; Tipo='DWord'; Valor='58' }
           @{ Path='HKCU:\Control Panel\Accessibility\MouseKeys'; Nome='Flags'; Tipo='DWord'; Valor='58' }
       )
    }
    @{ Id='ux.shutdown'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='WaitToKillServiceTimeout 2000ms + HungAppTimeout 1000 (EXM/AIMDARK)'
       Explicacao='Origem: EXM/AIMDARK + "Diminuir processos". Windows espera menos antes de matar servicos/apps pendurados ao desligar.'
       Problema='Apps com salvar-em-disco pendente podem perder dados se o disco estiver lento.'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control'; Nome='WaitToKillServiceTimeout'; Tipo='String'; Valor='2000' }
           @{ Path='HKCU:\Control Panel\Desktop'; Nome='WaitToKillAppTimeout'; Tipo='String'; Valor='2000' }
           @{ Path='HKCU:\Control Panel\Desktop'; Nome='HungAppTimeout'; Tipo='String'; Valor='1000' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'; Nome='AutoEndTasks'; Tipo='String'; Valor='1' }
       )
    }

    # ==================== SECURITY (unattend, 1 a 1) ====================
    @{ Id='sec.uac'; Categoria='Privacidade e Telemetria'; Risco='Medio'; Reinicio=$true
       Nome='UAC OFF (unattend: EnableLUA=0)'
       Explicacao='Origem: autounattend (Specialize) + "Desabilitar UAC.reg" do Fivem. Controle de Conta de Usuario desativado.'
       Problema='Todos os programas rodam como admin silenciosamente: malware ganha acesso total sem pedir. Loja e apps UWP podem quebrar. E o estado atual do seu PC.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'; Nome='EnableLUA'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'; Nome='ConsentPromptBehaviorAdmin'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='sec.smartscreen'; Categoria='Privacidade e Telemetria'; Risco='Medio'; Reinicio=$false
       Nome='SmartScreen OFF (unattend + EXM + Fivem)'
       Explicacao='Origem: unattend (SmartScreenEnabled=Off + WTDS) + EXM + "Desabilitar SmartSceen e Downloads Blocks.reg" (SaveZoneInformation).'
       Problema='Downloads suspeitos nao sao checados contra reputacao: mais risco de baixar malware por descuido.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer'; Nome='SmartScreenEnabled'; Tipo='String'; Valor='Off' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\System'; Nome='EnableSmartScreen'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components'; Nome='ServiceEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments'; Nome='SaveZoneInformation'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='sec.longpaths'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='LongPathsEnabled = 1 (unattend)'
       Explicacao='Origem: unattend. Caminhos de arquivo > 260 chars funcionam (util para pastas de mods).'
       Problema='Nenhum. Alguns programas antigos ficam confusos com caminhos longos (raro).'
       Spec=@( @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem'; Nome='LongPathsEnabled'; Tipo='DWord'; Valor='1' } )
    }
    @{ Id='sec.audit'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Auditoria de processos ON (unattend: auditpol)'
       Explicacao='Origem: unattend (auditpol /subcategory Process Creation enable). Log de auditoria quando processo e criado (seguranca/forense).'
       Problema='Log de Segurança cresce mais rapido. Custo minimo.'
       Comandos=@(
           @{ Cmd='auditpol /set /subcategory:"{0CCE922B-69AE-11D9-BED3-505054503030}" /success:enable /failure:enable'; Rev='auditpol /set /subcategory:"{0CCE922B-69AE-11D9-BED3-505054503030}" /success:disable /failure:disable' }
       )
    }
    @{ Id='sec.lockout'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Sem bloqueio de conta e senha sem expirar (unattend: net accounts)'
       Explicacao='Origem: unattend (lockoutthreshold:0, maxpwage:UNLIMITED). Conta local nunca bloqueia por tentativas e senha nunca expira.'
       Problema='Menos protecao contra forca bruta local. Em PC domestico unico usuario: ok.'
       Comandos=@(
           @{ Cmd='net accounts /lockoutthreshold:0'; Rev='net accounts /lockoutthreshold:5' }
           @{ Cmd='net accounts /maxpwage:UNLIMITED'; Rev='net accounts /maxpwage:90' }
       )
    }
    @{ Id='sec.edgepol'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Edge policies: sem first-run, sem boost, sem background (unattend)'
       Explicacao='Origem: autounattend (HideFirstRunExperience, BackgroundModeEnabled=0, StartupBoostEnabled=0).'
       Problema='Nenhum. Edge abre mais "limpo" e morre quando fecha.'
       Spec=@(
           @{ Path='HKLM:\Software\Policies\Microsoft\Edge'; Nome='HideFirstRunExperience'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\Software\Policies\Microsoft\Edge\Recommended'; Nome='BackgroundModeEnabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\Software\Policies\Microsoft\Edge\Recommended'; Nome='StartupBoostEnabled'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='sec.edgeuninst'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$false
       Nome='Tornar o Edge DESINSTAVEL (unattend: MakeEdgeUninstallable)'
       Explicacao='Origem: autounattend (IntegratedServicesRegionPolicySet.json: guid 1bca278a -> enabled). Habilita a politica regional que permite desinstalar o Edge em Configuracoes > Aplicativos.'
       Problema='Alguns apps que usam WebView2 podem exigir o Runtime (instale o WebView2 Runtime separado se algo quebrar).'
       Comandos=@(
           @{ Cmd='powershell -NoProfile -Command "$p=''C:\Windows\System32\IntegratedServicesRegionPolicySet.json''; $o=Get-Content -LiteralPath $p -Encoding Utf8 | ConvertFrom-Json; $o.policies | ForEach-Object { if ($_.guid -eq ''{1bca278a-5d11-4acf-ad2f-f9ab6d7f93a6}'') { $_.defaultState=''enabled'' } }; $o | ConvertTo-Json -Depth 9 | Out-File -LiteralPath $p -Encoding Utf8"'; Rev='echo Reinstale o Edge para voltar ao padrao' }
       )
    }
    @{ Id='sec.oobe'; Categoria='Windows Update'; Risco='Baixo'; Reinicio=$false
       Nome='BypassNRO + matar DevHome/Outlook do OOBE (unattend: Specialize)'
       Explicacao='Origem: autounattend. BypassNRO permite conta local sem internet no primeiro boot; remove agendadores de primeira-execucao do DevHome/Outlook.'
       Problema='Nenhum apos instalado (afeta OOBE).'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\OOBE'; Nome='BypassNRO'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='sec.chat'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='ConfigureChatAutoInstall = 0 (unattend: Teams chat auto)'
       Explicacao='Origem: unattend. Impede o auto-instalar do Teams chat na taskbar.'
       Problema='Nenhum.'
       Spec=@( @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Communications'; Nome='ConfigureChatAutoInstall'; Tipo='DWord'; Valor='0' } )
    }
    @{ Id='sec.consumer'; Categoria='Privacidade e Telemetria'; Risco='Baixo'; Reinicio=$true
       Nome='DisableWindowsConsumerFeatures + ConsumerAccountState (unattend)'
       Explicacao='Origem: unattend (Specialize + CloudContent). Windows nao instala apps sugeridos de fabrica nem restaura bloat apos feature update.'
       Problema='Nenhum para uso avancado. Menos "presentes" da Microsoft.'
       Spec=@(
           @{ Path='HKLM:\Software\Policies\Microsoft\Windows\CloudContent'; Nome='DisableWindowsConsumerFeatures'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\Software\Policies\Microsoft\Windows\CloudContent'; Nome='DisableConsumerAccountStateContent'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='sec.news'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='News and Interests / Widgets OFF (unattend: Dsh AllowNewsAndInterests)'
       Explicacao='Origem: unattend + EXM (Feeds). Remove feed de noticias/widgets da taskbar (processo WebView2 permanente).'
       Problema='Nenhum. Widgets pode ser religado nas Configuracoes.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Dsh'; Nome='AllowNewsAndInterests'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds'; Nome='EnableFeeds'; Tipo='DWord'; Valor='0' }
           @{ Path='HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Feeds'; Nome='ShellFeedsTaskbarViewMode'; Tipo='DWord'; Valor='2' }
       )
    }
    @{ Id='sec.bootsound'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$false
       Nome='Som de inicializacao OFF (unattend: BootAnimation)'
       Explicacao='Origem: unattend (DisableStartupSound + EditionOverrides).'
       Problema='Nenhum.'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation'; Nome='DisableStartupSound'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\EditionOverrides'; Nome='UserSetting_DisableStartupSound'; Tipo='DWord'; Valor='1' }
       )
    }
    @{ Id='sec.vbs'; Categoria='Otimização do Windows'; Risco='Medio'; Reinicio=$true
       Nome='VBS/HVCI OFF (unattend: DeviceGuard 0) - Isolamento de nucleo'
       Explicacao='Origem: autounattend (4 chaves DeviceGuard) + sua escolha de +FPS. Desativa Isolamento de nucleo/Virtualizacao Based Security. No 5700X recupera 3-8% de desempenho.'
       Problema='Perde protecao contra exploits de kernel. Hyper-V/WSL2/DVC param (hypervisorlaunchtype off e necessario tambem: item de energia).'
       Spec=@(
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'; Nome='EnableVirtualizationBasedSecurity'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity'; Nome='Enabled'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity'; Nome='EnabledBootId'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity'; Nome='WasEnabledBy'; Tipo='DWord'; Valor='0' }
       )
    }
    @{ Id='sec.visualeffects'; Categoria='Otimização do Windows'; Risco='Baixo'; Reinicio=$true
       Nome='Visual Effects defaults de desempenho (unattend: 16 chaves)'
       Explicacao='Origem: autounattend (Specialize: ControlAnimations/AnimateMinMax/TaskbarAnimations/AeroPeek/MenuAnimation/Tooltip/SelectionFade/SaveThumbnail/CursorShadow/ListviewAlpha/DragFull/ComboBox/DropShadow=0; ListviewShadow/ThumbnailsOrIcon/FontSmoothing/ListBoxSmooth=1).'
       Problema='Visual "utilitario". Fontes continuam ClearType (FontSmoothing=1 mantido).'
       Spec=@(
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\ControlAnimations'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\AnimateMinMax'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\TaskbarAnimations'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\DWMAeroPeekEnabled'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\MenuAnimation'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\TooltipAnimation'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\SelectionFade'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\DWMSaveThumbnailEnabled'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\CursorShadow'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\ListviewAlphaSelect'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\DragFullWindows'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\ComboBoxAnimation'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\DropShadow'; Nome='DefaultValue'; Tipo='DWord'; Valor='0' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\ListviewShadow'; Nome='DefaultValue'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\ThumbnailsOrIcon'; Nome='DefaultValue'; Tipo='DWord'; Valor='1' }
           @{ Path='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\FontSmoothing'; Nome='DefaultValue'; Tipo='DWord'; Valor='1' }
       )
    }
)
