# AGENTES

Equipes do Nexor ativas neste projeto, com o papel de cada uma aqui. Leia o arquivo da equipe antes da tarefa, em `A:\Desenvolvimento\IA\Nexor\equipes\`.

| Equipe | Papel neste projeto | Quando aciona |
| produto | escopo de versao, PRD, criterio de pronto, prioridade da fila | inicio de ciclo, mudanca de escopo |
| design | navegacao, consistencia visual das 27 paginas, acessibilidade de desktop, estados de erro e vazio | antes de mexer em XAML de tela |
| frontend | implementacao WPF, data binding, tema, localizacao PT/EN | tarefa de interface |
| backend | motores de sistema, acesso a registro, servico, WMI, agendador, COM, privilegio | tarefa de motor, driver, reparo, energia, rede |
| infra | empacotamento, release, ADK, assinatura, maquina de teste | publicar versao, preparar ambiente |
| seguranca | lista de bloqueio de tweak, privilegio, elevacao, origem de driver, LGPD quando houver dado | qualquer tweak novo, qualquer download |
| qualidade | build de verificacao, teste manual no Windows, checklist de aceite | antes de declarar pronto |
| criacao | textos da interface, LEIA-ME, notas de versao, tom PT-BR | texto novo ou revisao |

## Papeis de sessao

- Orchestrator: uma sessao por ciclo de versao, com delegacao.
- Builder: tarefa fechada em um modulo.
- Reviewer: valida entrega antes do commit de release.

## Regra de ouro

Tweak novo passa por seguranca antes de entrar no catalogo. Motor novo passa por backend e qualidade antes de entrar na interface.
