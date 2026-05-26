<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages

- **Estado:** Aceito / congelamento normativo Base 1.2
- **Base:** Base 1.2 — Actors Convergence / Convergência de Atores
- **Fundação normativa:** Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- **Relacionado:**
    - ADR-0014 — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline
    - ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
    - ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP
    - ADR-1.2-0004 — ActorAttributes como ActorCapability
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
