# ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages

- **Estado:** Aceito / congelamento normativo Base 1.2 / checkpoint funcional congelado
- **Base:** Base 1.2 — Actors Convergence / Convergência de Atores
- **Fundação normativa:** Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- **Relacionado:**
    - ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline
    - ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
    - ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP
    - ADR-1.2-0004 — ActorAttributes como ActorCapability
    - ADR-1.2-0006 — ActivityCapabilityPermission e Reação Local de Capabilities
    - ADR-1.2-0007 — Capability Discovery e Activity Capability Inventory
- **Escopo:** fronteira normativa para decomposição incremental do `SessionActivityPipeline` sem criar Base 2.0 completa
- **Fora do escopo:** alterações em `SessionOperationalPipeline`, framework genérico novo, compat paralelo, `partial` como solução principal

---

## 1. Contexto

A Base 1.1 está congelada. A Base 1.2 avança a Convergência de Atores sem redefinir o contrato operacional já validado.

A auditoria do trilho ativo mostrou que o `SessionActivityPipeline` acumulou responsabilidades que não pertencem ao mesmo owner:

```text
lifecycle macro
ActivityEntry
ActorPresentation
NonPlayerActor
ActorAttributes
PlayerActor
ActivityObject
QA
runtime command routing
```

Isso cria risco real de `God Pipeline`.

Este ADR congela a fronteira normativa para permitir decomposição incremental sem quebrar checkpoints já validados. O objetivo não é criar uma Base 2.0, nem reabrir a arquitetura operacional inteira. O objetivo é separar ownership de forma compatível com o que já existe.

---

## 2. Decisão central

O `SessionActivityPipeline` permanece como owner do lifecycle macro da Session Activity.

Ele decide:

```text
StartFromPreparedHandoff
activity entry sequence
ActivationWindow / ActivityRunning / DeactivationWindow
ActivityTransition
RestartCurrentActivity
CloseForRouteExit
foreign/stale guard do ciclo
```

A decomposição obrigatória fica assim:

```text
SessionActivityPipeline core
-> decide lifecycle macro e handoff

ActivityEntryPipeline stages
-> resolvem e comandam setup / release / readiness determinístico

Capability stages
-> preparam e liberam capabilities específicas

Endpoints / capability runtimes
-> validam e executam comportamento local quando a relação é local

Adapters
-> executam side-effects comandados

QA probes
-> ficam isoladas
```

Regras normativas:

1. `SessionActivityPipeline` não deve absorver nova ownership de capability.
2. Runtime mutation local não deve ser roteada pelo pipeline como se fosse lifecycle.
3. Sempre que a ação puder ser resolvida por relação local adequada, ela não pertence ao pipeline.
4. QA específico não deve virar stage de produção.
5. `SessionOperationalPipeline` permanece fora desta decomposição.
6. `partial` não é solução arquitetural para corrigir ownership.

---

## 3. Fronteira normativa

### 3.1 O que continua no `SessionActivityPipeline`

O core continua responsável por:

```text
macro lifecycle
cycle guard
route exit
restart
activity transition
handoff acceptance
entry sequence
stage ordering canônico
foreign/stale rejection
```

Isso inclui decidir quando um stage começa, quando um handoff é aceito e quando um ciclo é rejeitado por identidade foreign/stale.

### 3.2 O que deve sair do core

O core não deve ser owner de:

```text
capability setup/release
local runtime mutation
scene-authored actor discovery detalhado
actor presentation materialization/release detalhados
object snapshot/reset/restore detalhados
player readiness/input/movement/camera detalhados
QA probes
```

Essas responsabilidades passam para stages dedicadas, endpoints ou probes, conforme a natureza do comportamento.

### 3.3 Relações locais e ações locais

A Base 1.2 congela a seguinte regra:

```text
quando uma ação puder ser resolvida por relação local segura,
ela não deve ser roteada pelo pipeline.
```

Exemplos de relação local segura:

```text
objeto causador -> objeto alvo
collider/hit source -> ActorAttributeEndpoint do alvo
interação local -> endpoint local do objeto participante
capability local -> state runtime da própria instância
```

Essas ações podem produzir facts/logs/read-models para observabilidade, mas não se tornam lifecycle do pipeline.

O pipeline pode preparar, habilitar, resetar ou liberar a capability. Ele não deve arbitrar cada variação local moment-to-moment.

Exemplos que não pertencem ao pipeline como command runtime obrigatório:

```text
Subtract Health
Add Shield
Spend Stamina
Heal
Damage
Set local de atributo
Reset local de atributo já exposto como command local
```

Pertencem ao pipeline apenas quando a decisão for de lifecycle, ordenação determinística, policy de setup/release/reset global, snapshot/save timing ou handoff.

Proibido:

```text
usar o pipeline como service locator de gameplay local
usar o pipeline como router universal de commands locais
criar porta global para toda mutation local por conveniência
transformar QA/debug em caminho obrigatório de gameplay
```

---

## 4. Congelamento normativo

Ficam congeladas as seguintes regras:

1. `SessionActivityPipeline core` decide lifecycle macro.
2. `ActivityEntryPipeline` resolve e comanda setup/release/readiness determinístico.
3. `Capability stages` preparam e liberam capabilities específicas.
4. `Endpoints/capability runtimes` validam e executam comportamento local.
5. Runtime mutation local não deve ser roteada pelo pipeline.
6. Sempre que for possível e mais adequado resolver por relação local, a ação não pertence ao pipeline.
7. QA específico deve ficar em probes isolados.
8. Adapters executam side-effects comandados.
9. `SessionOperationalPipeline` não é alterado.

---

## 5. Responsabilidades por owner

### 5.1 SessionActivityPipeline core

Permanece no core:

```text
StartFromPreparedHandoff
activity entry sequence
ActivationWindow / ActivityRunning / DeactivationWindow
ActivityTransition
RestartCurrentActivity
CloseForRouteExit
foreign/stale guard do ciclo
```

### 5.2 ActivityEntryPipeline stages

Devem morar em stages do entry pipeline:

```text
ActivityContent load/release
Object setup/release/reset/restore
Participant setup/readiness
PlayerActor readiness/input/movement/camera binding
```

### 5.3 Capability stages

Devem morar em stages de capability:

```text
ActorPresentation setup/release
NonPlayerActor discovery/participation/readiness
ActorAttributes setup/release
```

### 5.4 Capability runtime / endpoints

Devem morar em endpoints e runtimes locais:

```text
ActorPresentation container/materialization behavior
NonPlayerActor runtime identity/state
ActorAttributes runtime mutation
Object snapshot/reset/restore execution local
PlayerActor local binding states
```

Esses runtimes/endpoints podem rejeitar comandos locais quando não estão prontos, quando o atributo/capability não existe ou quando a instância já foi liberada. Essa rejeição local não transfere ownership para o pipeline.

### 5.5 QA probes

Devem morar em probes isolados:

```text
ActorAttributes runtime commands QA
ActorPresentation smoke/manual probe
NonPlayerActor discovery/readiness QA
ActivityObject snapshot/reset/restore QA
```

### 5.6 Adapters

Devem continuar como executores de side-effects:

```text
scene load/unload
presentation materialization/release
input binding
movement binding
camera binding
pause overlay
transition loading/fade
```

---

## 5.7 Matriz de escopo — Activity vs Actor vs Object vs Local Gameplay

A decomposição do `SessionActivityPipeline` não deve trocar um `God Pipeline` por trilhos paralelos de `Player`, `NonPlayerActor` e `ActivityObject`.

A classificação normativa é:

| Pergunta | Owner correto | Observação |
|---|---|---|
| A Activity entra, roda, reinicia, transiciona ou sai? | `SessionActivityPipeline core` | lifecycle macro |
| Qual stage vem antes/depois? | `ActivityEntryPipeline` | ordem determinística |
| Um requisito é obrigatório/opcional? | stage/policy/inventory | ausência obrigatória é fail-fast |
| Um actor participa desta entry? | actor participation stage | Actor tem identidade própria |
| Um objeto participa como contributor? | object discovery/inventory | ActivityObject não vira Actor implicitamente |
| Quando resetar/restaurar/capturar/liberar? | pipeline/stage dono do ciclo | timing determinístico |
| Como resetar/restaurar/capturar/liberar? | endpoint/capability local | execução local |
| Uma capability pode aceitar gameplay agora? | permission/policy de Activity | o pipeline publica permissão semântica |
| Como a capability reage ao permitido/bloqueado? | receiver/runtime local | reação local da instância |
| Dano/heal/stamina/interação moment-to-moment | relação local entre endpoints/capabilities | não é lifecycle do pipeline |

Regra de corte:

```text
Pipeline Scope = quando, ordem, policy, readiness, handoff, reset/snapshot/release timing e foreign/stale guard.
Actor Scope = identidade, participation, lifecycle de ator e ActorCapabilities.
Object Scope = estado próprio, endpoints locais e capabilities de objeto/contributor.
Local Gameplay Scope = relações moment-to-moment entre instâncias.
```

`PlayerActor` e `NonPlayerActor` não devem virar lifecycles paralelos. Eles convergem como `Actor` com policies, endpoints e capabilities diferentes. O que varia é o inventário resolvido e as policies, não a existência de um pipeline separado para cada tipo de ator.

## 6. Matriz normativa

| Responsabilidade atual | Owner correto | Ação |
|---|---|---|
| Lifecycle macro da Session Activity | `SessionActivityPipeline core` | Manter |
| `StartFromPreparedHandoff` e guardas foreign/stale | `SessionActivityPipeline core` | Manter |
| Sequência de entrada da Activity | `ActivityEntryPipeline stage` | Extrair stage |
| `ActivityContent` load/release | `ActivityEntryPipeline stage` | Extrair stage |
| `ActivityObject` snapshot/reset/restore | `Capability Runtime/Endpoint` | Mover endpoint |
| `PlayerActor` readiness/input/movement/camera | `ActivityEntryPipeline stage` | Extrair stage |
| `ActorPresentation` setup/release | `Capability Stage` | Extrair stage |
| `ActorPresentation` materialization/retenção local | `Capability Runtime/Endpoint` | Mover endpoint |
| `NonPlayerActor` discovery/participation/readiness | `ActivityEntryPipeline stage` | Extrair stage |
| `NonPlayerActor` runtime registry/state | `Capability Runtime/Endpoint` | Mover endpoint |
| `ActorAttributes` setup/release | `Capability Stage` | Extrair stage |
| `ActorAttributes` runtime mutation local | `Capability Runtime/Endpoint` | Mover endpoint / relação local |
| `ActorAttributes` command routing via pipeline | `QA Probe` ou remover | Não usar como gameplay path |
| QA de runtime command para atributos | `QA Probe` | Mover QA |
| QA de presentation/discovery/snapshot | `QA Probe` | Mover QA |
| Adapters de side-effects | `Adapter` | Manter |
| Route exit e restart | `SessionActivityPipeline core` | Manter |
| SessionOperationalPipeline | `Future` / fora do escopo | Deixar futuro |

---

## 7. Sequência segura de decomposição

### 7.1 Ordem recomendada

1. Reverter/remover wiring novo que transforme `SessionActivityPipeline` em port/router obrigatório de mutation local.
2. Manter QA de `ActorAttributes` em probe isolado, sem `SessionActivityDebugPanel` e sem path obrigatório de gameplay.
3. Formalizar o caminho local de mutation em `ActorAttributeEndpoint`/receiver local, sem passar pelo pipeline para ações moment-to-moment.
4. Extrair `ActorAttributes` setup/release para capability stage próprio.
5. Extrair `ActorPresentation` para capability stage com runtime local próprio.
6. Separar `NonPlayerActor` discovery, participation e readiness em stage própria.
7. Quebrar `PlayerActor` readiness/input/movement/camera em sub-stages do entry pipeline.
8. Mover `ActivityObject` snapshot/reset/restore para runtime/endpoint quando a execução for local.
9. Separar `ActivityContent` load/release e o inventário de setup em stages do entry pipeline.
10. Só depois reduzir o acoplamento residual do core, sem tocar em `SessionOperationalPipeline`.

### 7.2 Critério de segurança

Cada passo deve preservar os checkpoints já congelados:

```text
PlayerActor readiness/input/movement/camera
ActivityContent load/release
ActivityObject snapshot/reset/restore
ActorPresentation
NonPlayerActor
ActorAttributes Setup/Release
ActorAttributes Runtime Commands local/QA
RestartCurrentActivity
RouteExit
```

Se um passo exigir compat paralelo ou reinterpretação do contrato, ele está cedo demais.

---

## 8. Riscos que este ADR evita

1. `SessionActivityPipeline` virar owner de capability local por inércia.
2. QA entrar no trilho canônico e virar pseudo-stage de produção.
3. `ActivityEntry` continuar misturado com lifecycle macro.
4. `ActivityObject`, `ActorPresentation` e `NonPlayerActor` ficarem presos ao mesmo bloco de decisão.
5. `partial` ser usado como remendo para um problema de ownership.
6. Runtime mutation local virar command obrigatório de pipeline por conveniência.
7. Relações locais simples serem transformadas em fluxo global sem necessidade.

---

## 9. Consequência normativa

A partir deste ADR:

```text
o SessionActivityPipeline pode orquestrar
mas não deve absorver capability ownership
```

E também:

```text
o que é local, capability ou probe não deve voltar a ser roteado como lifecycle core
```

Regra prática:

```text
setup/release/reset lifecycle = pipeline/stage
ação local moment-to-moment = endpoint/capability local
side-effect Unity comandado = adapter
QA específico = probe isolado
```

Esse é o limite entre Base 1.1 congelada e Base 1.2 em convergência de atores, sem abrir uma Base 2.0 completa.

---

## 10. Critério de aceite

Este ADR é aceito quando a decomposição preservar os checkpoints existentes e impedir novos acoplamentos indevidos.

Critérios:

```text
SessionActivityPipeline core não absorve novas capabilities.
Mutation runtime local não entra no pipeline por padrão.
Capability stages existem para setup/release/readiness.
Endpoints/capability runtimes executam comportamento local.
QA específico fica fora do core e fora do painel macro.
SessionOperationalPipeline permanece intocado.
```

A primeira aplicação prática deve ser `ActorAttributes`, porque foi a capability que revelou o risco de transformar mutation local em routing de pipeline.

---

## 11. Checkpoint funcional congelado — SessionActivityPipeline Decomposition

Após a aplicação incremental deste ADR na Base 1.2 — Actors Convergence, fica congelado o seguinte checkpoint funcional:

```text
SessionActivityPipeline Decomposition — Functional Freeze
Status: PASS funcional
Escopo: Base 1.2 / Actors Convergence
Fundação preservada: Base 1.1 / Pipeline Convergence
```

O checkpoint confirma que o `SessionActivityPipeline` permanece como owner do lifecycle macro, enquanto a execução detalhada de capabilities e participantes foi deslocada para boundaries/stages dedicados, ainda que vários desses boundaries permaneçam fisicamente internos ao arquivo `SessionActivityPipeline.cs` neste corte.

Este congelamento não transforma a organização física atual em shape final. Ele congela apenas o comportamento, a fronteira de ownership e a evidência funcional validada.

---

## 12. Fases fechadas como PASS funcional

As seguintes fases da decomposição foram concluídas e validadas por smoke de caminho feliz:

| Fase | Frente | Resultado | Observação normativa |
|---|---|---|---|
| Fase 2B | `ActorAttributes` | PASS funcional | Setup/release em boundary/stage; mutation local permanece no `ActorAttributeEndpoint`/relação local. |
| Fase 3B | `ActorPresentation` | PASS funcional | Setup/release/materialization details em boundary/stage; `UnityActorPresentationMaterializationAdapter` permanece executor de side-effect. |
| Fase 4B | `NonPlayerActor` | PASS funcional | Discovery, participation enter/exit e readiness em stages dedicados; registry/runtime local preservados. |
| Fase 5B | `PlayerActor` / input / movement / camera | PASS funcional | Readiness, input binding, movement binding/control e camera binding separados em boundaries/stages; checkpoints Base 1.1 preservados. |
| Fase 5C1 | `ActivityObject` Entry | PASS funcional | Discovery, snapshot contract validation, reset e snapshot restore separados em boundary/stage. |
| Fase 5C2 | `ActivityObject` Exit | PASS funcional | Snapshot capture, release e contributor unregister separados em boundary/stage. |

### 12.1 Checkpoints preservados

O freeze preserva os checkpoints funcionais já aceitos da Base 1.1 e os cortes da Base 1.2:

```text
SessionActivityEntryHandoffAccepted
ActorPresentationReady / Retained / Released
ActorAttributeReady / Changed / Released / SkippedNoContent
NonPlayerActorDiscoveryCompleted
NonPlayerActorParticipationEntered / Exited
NonPlayerActorReady
PlayerInputActionsReboundToCanonical
MovementBindingCompleted
MovementBindingRetained
MovementControlEnabled / Disabled
PlayerCameraEndpointResolved
ActivityCameraTargetBound
CameraBindingCompleted
CameraBindingSkippedNoRequiredCamera
ActivityObjectContributorDiscovery
ActivityObjectSnapshotContractValidation
ActivityObjectReset
ActivityObjectSnapshotRestore
ActivityObjectSnapshotCapture
ActivityObjectRelease
ActivityObjectContributorUnregister
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
```

### 12.2 Semântica de skip preservada

A decomposição preserva a regra normativa:

```text
subplano vazio ou requisito opcional ausente
=> Skipped / zero-count explícito
=> não vira failure
```

Exemplos congelados:

```text
activity_02 sem ActivityContent
=> ActivityObject discovery/reset/release com contagens zero ou skipped explícito

activity_02 sem camera requirement obrigatório
=> CameraBindingSkippedNoRequiredCamera
=> MovementControlEnabled
=> ActivityRunning
```

---

## 13. Dívida deliberada: separação física dos stages internos

Fica registrada como dívida explícita:

```text
Vários stages/boundaries já existem logicamente,
mas ainda permanecem fisicamente dentro de SessionActivityPipeline.cs.
```

Isso inclui, conforme o corte atual:

```text
ActorAttributeSetupStage / ActorAttributeReleaseStage
ActorPresentationSetupStage / ActorPresentationReleaseStage
NonPlayerActorDiscoveryStage / NonPlayerActorParticipationStage
PlayerActorReadinessStage
PlayerInputBindingStage
PlayerMovementBindingStage
PlayerMovementControlStage
PlayerCameraBindingStage
ActivityObjectEntryStage
ActivityObjectExitStage
```

Essa dívida foi aceita deliberadamente para estabilizar comportamento antes de reorganizar arquivos. Ela não autoriza novas responsabilidades no core do pipeline.

### 13.1 Regra de congelamento

Até nova fase explícita de organização física:

```text
não mover stages para arquivos próprios
não aplicar partial como solução arquitetural
não criar context objects genéricos por conveniência
não abrir campos privados do pipeline apenas para facilitar extração
não alterar logs/facts/checkpoints
não alterar comportamento runtime
```

A organização física futura deve ser feita em fase própria, com auditoria de dependências, plano de corte pequeno e smoke completo de não regressão.

### 13.2 O que este checkpoint não congela

Este checkpoint **não** congela o arquivo `SessionActivityPipeline.cs` como forma final.

Congela apenas:

```text
ownership lógico
ordem macro
semântica de lifecycle
semântica de skip/failure
pontos de integração entre stages, endpoints e adapters
evidência funcional validada
```

---

## 14. Restrições para próximos cortes

Enquanto este checkpoint estiver congelado, próximos trabalhos não devem reabrir as frentes abaixo sem necessidade concreta:

```text
ActorAttributes
ActorPresentation
NonPlayerActor
PlayerActor readiness/input/movement/camera
ActivityObject entry
ActivityObject exit
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
```

É permitido mexer nesses trilhos somente quando houver:

```text
bug real de caminho feliz
novo requisito concreto de gameplay
nova capability com ownership explícito
fase dedicada de organização física sem mudança semântica
```

É proibido:

```text
limpeza estética antes de freeze
refatoração física misturada com mudança funcional
compat paralelo
fallback silencioso
framework genérico de stages
service locator de gameplay local
QA/debug como caminho obrigatório de produção
```

---

## 15. Próximos passos recomendados

Próximo passo imediato:

```text
não implementar nova decomposição agora
não reorganizar fisicamente agora
manter checkpoint congelado
```

Próximas reaberturas possíveis, apenas quando houver necessidade concreta:

1. **Organização física dos stages internos**  
   Fase própria, sem alterar comportamento, com auditoria de dependências e smoke completo.

2. **Novas capabilities de atores**  
   Devem entrar por `ActorCapability`, `ActorEndpoint`, stage/boundary ou adapter adequado, sem reabrir o core do lifecycle macro.

3. **RuntimeSpawn / ObjectEntry real**  
   Só quando houver objeto, prop, actor ou prefab realmente materializado pelo pipeline.

4. **Pooling integrado a objetos concretos**  
   Só quando houver policy explícita e caso real de `Rent`, `Prewarm` ou `ReturnToPool`.

5. **Progression Save real**  
   Só quando houver progressão real de actor, objeto, run ou activity para persistir.

---

## 16. Conclusão do freeze

A Base 1.2 — Actors Convergence fecha este corte com a seguinte conclusão:

```text
O SessionActivityPipeline deixou de ser o owner lógico dos detalhes operacionais de capabilities e participantes.
Ele permanece owner do lifecycle macro.
Stages/boundaries especializados concentram a decomposição lógica.
Endpoints e adapters preservam side-effects e comportamento local.
A separação física ainda é dívida deliberada.
```

Este checkpoint deve ser tratado como contrato de estabilização antes de qualquer nova refatoração física.

