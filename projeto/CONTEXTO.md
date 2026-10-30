# CONTEXTO

## O que e

PHANTOM UI, aplicativo desktop de otimizacao para Windows 10 e 11 (x64). Um executavel unico, sem exigir .NET instalado na maquina de destino. Interface WPF, tema escuro, portugues do Brasil e ingles.

## Raiz autorizada

Apenas a pasta deste projeto. Codigo em `codigo/`, documentos em `docs/`, memoria em `memoria/`, descartaveis em `temp/`.

Fora da raiz: nada. Nao escrever no Nexor, no COFRE, em BACKUPS ou em outro projeto sem pedido explicito para a tarefa.

## Ambientes

| Ambiente | Caminho | Papel | Proibicao |
| Development | pasta do projeto, qualquer PC com .NET SDK 8 | compilar, testar, empacotar | sem acesso a dado real de cliente |
| Verificacao | Windows 10 ou 11 x64 com conta de administrador | rodar o executavel, aplicar tweak, validar sensor | maquina de cliente, maquina de trabalho em uso |
| Build Linux (CI e agente) | sandbox Linux com .NET SDK 8 | compilar cruzado e validar estaticamente | nao executa WPF, WMI, powercfg, bcdedit, pnputil, DISM, oscdimg |

Compilar no Linux serve para provar que o codigo fecha. Rodar so no Windows.

## Hardware de referencia

Maquina de teste do Gabriel: verificar no Windows com `codigo/scripts/coletar-ambiente.ps1` antes de qualquer conclusao sobre desempenho.

## Enderecos e servicos

Nenhum servico em rede. O aplicativo nao abre porta, nao tem servidor e nao envia dado para fora. Acesso a rede so para consulta de driver oficial, benchmark de DNS e atualizacao de jogo.

## Dados

Nada de dado pessoal. Relatorios gerados ficam na maquina do usuario, em `%AppData%\PhantomUI\Reports`, e backups em `%AppData%\PhantomUI\Backups`. O projeto nao coleta telemetria.

## Proibicoes

- Nao publicar release, nao assinar executavel e nao gerar instalador sem autorizacao.
- Nao incluir tweak destrutivo, nem escondido atras de flag.
- Nao usar nome, marca ou imagem de terceiro no aplicativo e nos materiais.
- Nao gravar senha, token ou certificado no repositorio.
