# =====================================================================
#  Kairo Tweaker :: 28.AtivadoresMsi.ps1
#  ABA ATIVACAO: ativadores (slmgr/KMS, IoT LTSC) e MSI Utility.
#  Conteudo ABSORVIDO da pasta "8 - Ativadores" (ATIVADOR.txt, IoT EnterpriseS,
#  WIN-11 IoT how-to) e do MSI_util_v3 (aberto de dentro do app).
#  ATIVACAO por KMS e tecnica para TESTES: licencie o Windows oficialmente
#  quando possivel. Nenhum binario e executado aqui: so comandos oficiais
#  do Windows (slmgr/cscript) e o MSI_util_v3 que ja existe no acervo.
# =====================================================================

Add-KTModule -Modulo 'AtivacaoMsi' -Tweaks @(

    @{ Id='ativ.kms.win10pro'; Categoria='Ativação'; Risco='Medio'; Reinicio=$false
       Nome='Ativar Windows Pro (chave generica + KMS msguides)'
       Explicacao='Origem: ATIVADOR.txt. Instala a chave generica de instalacao do Windows 10/11 Pro (W269N-WFGWX-YVC9B-4J6C9-T83GX), aponta o KMS para kms8.msguides.com e ativa (slmgr /ato). E o metodo KMS publico da internet.'
       Problema='Ativacao KMS expira (180 dias) e reativa precisa repetir. Nao e licenca original. Antivirus de terceiros as vezes bloqueiam o slmgr /skms. Use so para teste/poc.'
       Comandos=@(
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /ipk W269N-WFGWX-YVC9B-4J6C9-T83GX'; Rev='echo Sua licenca anterior pode ser restaurada com slmgr /ipk <chave>' }
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /skms kms8.msguides.com'; Rev='cscript.exe //nologo %windir%\system32\slmgr.vbs /ckms' }
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /ato'; Rev='echo /ato aplica a ativacao atual' }
       )
    }
    @{ Id='ativ.iot.ltsc'; Categoria='Ativação'; Risco='Medio'; Reinicio=$false
       Nome='Ativar Windows IoT Enterprise LTSC (chave KBN8V...)'
       Explicacao='Origem: "WIN-11 IOT Ent LTSC how to". Reinstala tokens de licenca (/rilc), desinstala chave atual, limpa KMS e aplica a chave de IoT EnterpriseS KBN8V-HFGQ4-MGXVD-347P6-PDQGT; reinicia LicenseManager.'
       Problema='Somente se a EDICAO instalada for IoT/LTSC: em Pro/Home a chave nao corresponde e da erro 0xC004F074. /upk remove a ativacao atual (precisa reativar depois).'
       Comandos=@(
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /rilc'; Rev='echo /rilc reinstala tokens (sem reverso)' }
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /upk'; Rev='echo ATENCAO: /upk REMOVE a chave atual do Windows' }
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /ckms'; Rev='echo /ckms limpa servidor KMS' }
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /cpky'; Rev='echo /cpky remove a chave do registro' }
           @{ Cmd='cscript.exe //nologo %windir%\system32\slmgr.vbs /ipk KBN8V-HFGQ4-MGXVD-347P6-PDQGT'; Rev='echo reative com sua chave' }
           @{ Cmd='sc config LicenseManager start= auto & net start LicenseManager'; Rev='echo servico de licenca religado' }
           @{ Cmd='sc config wuauserv start= auto & net start wuauserv'; Rev='echo wuauserv religado (reverta o bloqueio do app depois se quiser)' }
       )
    }
    @{ Id='ativ.marca'; Categoria='Ativação'; Risco='Baixo'; Reinicio=$false
       Nome='Remover marca d agua de ativacao (restart do explorer)'
       Explicacao='Origem: removemarca.bat. Reinicia o explorer para a marca "Ativar Windows" sumir da tela ate a proxima verificacao.'
       Problema='So some VISUALMENTE por um tempo; o Windows continua desativado.'
       Comandos=@(
           @{ Cmd='taskkill /F /IM explorer.exe & start explorer.exe'; Rev='echo sem reverso necessario' }
       )
    }
    @{ Id='ativ.licencas'; Categoria='Ativação'; Risco='Baixo'; Reinicio=$false
       Nome='Garantir LicenseManager e wuauserv ATIVOS para ativacao'
       Explicacao='Origem: how-to IoT. A ativacao precisa do servico de licenca e do wuauserv rodando. Este item religa os dois (reverta o bloqueio do WU depois, se quiser).'
       Problema='Religa o Windows Update servico (o bloqueio 2047 do app continua pelas politicas).'
       Servicos=@(
           @{ Nome='LicenseManager'; Inicio='Automatic' }
       )
    }
    @{ Id='ativ.msi'; Categoria='Ativação'; Risco='Baixo'; Reinicio=$false
       Nome='Abrir MSI Utility v3 (modo MSI de GPU/Soundcard de dentro do app)'
       Explicacao='Origem: MSI_util_v3.exe do acervo. Abre o utilitario para definir o modo de interrupcao (MSI vs Line-Based) de cada dispositivo PCIe. Colocar a GPU e a placa de som em MSI reduz latencia de interrupcao. O app abre a ferramenta grafica; voce escolhe o dispositivo e clica MSI.'
       Problema='Ativar MSI em dispositivo incompativel pode fazer o dispositivo sumir do Windows ate reverter (segure power off). GPU/sound de AMD/Realtek geralmente aceitam bem.'
       Comandos=@(
           @{ Cmd='cmd /c start "" "MSI_util_v3\MSI_util_v3.exe"'; Rev='echo ferramenta grafica: reversa feita dentro dela' }
       )
    }
)
