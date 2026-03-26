# ADR-0038: Module Service Registration and Runtime Composition

## Status
- Aceito e validado em pilotos

## Evidências canônicas
- auditorias recentes de `module registration` vs `runtime composition`
- `Baseline 3.5`
- `ADR-0035` de ownership canônico
- `ADR-0030` e `ADR-0031` para fronteiras e pipeline macro
- pilotos validados em `GameLoop`, `SceneFlow`, `Navigation`, `LevelFlow` e `WorldReset`
- `Modules/GameLoop/Bootstrap/GameLoopBootstrap.cs` como referência positiva inicial

## Contexto
O modelo anterior misturava registro de serviços com composição runtime. Isso gerava pipelines frágeis, dependências invertidas e correções em cascata.

A base correta em `NewScripts` é duas fases globais, com registro primeiro e uso operacional depois. A decisão consolidada precisa também refletir a ordem por dependência explícita e o fail-fast do pipeline.

O objetivo não é esvaziar `Infrastructure/Composition`. O objetivo é remover wiring interno de módulo do root global e preservar apenas o núcleo legítimo de orquestração, helpers e entry points.

## Decisão
- `Module Installer` registra serviços, contratos, providers, factories, configs e pré-requisitos do módulo.
- `Module Runtime Bootstrap / Composer` compõe e ativa runtime depois que todos os installers relevantes terminaram.
- `GlobalCompositionRoot` orquestra fases, valida e resolve a ordem, e executa entry points explícitos.
- A ordem entre módulos em cada fase é determinada por dependências explícitas, com ordenação topológica ou equivalente.
- O pipeline falha cedo em dependência ausente, dependência inválida, ID duplicado e ciclo.
- Arquivos de installer e bootstrap/composer permanecem dentro do próprio módulo.
- Não há auto-registro mágico, reflection opaca ou bootstrap invisível.

## Canonical Two-Phase Model
Fase 1: todos os `Module Installers` executam primeiro e apenas registram o que o módulo disponibiliza.

Fase 2: todos os `Module Runtime Bootstraps / Composers` executam depois, usando somente o DI já preenchido.

As fases são globais. O módulo continua dono do seu wiring. O root apenas coordena a execução e não incorpora lógica interna de composição.

## Canonical Dependency Model
Cada módulo declara dependências explícitas para sua fase de installer e, quando aplicável, para sua fase de runtime composition.

O `GlobalCompositionRoot` monta o grafo de execução por fase e aplica ordenação determinística antes de invocar os entry points.

Dependências entre módulos não devem ser inferidas por reflexão, nomes de pasta ou descoberta implícita. A relação precisa ser declarada, validada e executada de forma previsível.

## What Belongs in Module Installers
Podem entrar:
- serviços
- interfaces
- providers
- factories
- configs
- registries
- contratos explícitos para bootstrap/composer do próprio módulo

Não entram:
- composição operacional
- ativação de runtime
- wiring entre serviços já consumíveis
- integração de pipeline
- execução de comportamento de domínio

## What Belongs in Module Runtime Composition
Podem entrar:
- composição entre serviços já registrados
- ativação de orchestrators e bridges
- wiring operacional do stack
- montagem de serviços finais de runtime
- ligação explícita entre contratos de módulos já instalados

A fase runtime sempre acontece depois do registro. Ela não substitui o installer e não pode depender de registrar contratos novos para completar o próprio boot.

## What the Global Root Should Still Do
- ordenar fases
- validar dependências
- chamar installers
- chamar bootstraps/composers
- passar contexto compartilhado
- falhar cedo quando a ordem não puder ser resolvida

O root não deve conhecer o wiring interno detalhado dos módulos nem carregar composição operacional de domínio.

## What May Legitimately Remain in Global Composition
- `Entry`
- `Pipeline`
- helpers/infra globais legítimos
- shims finos de orquestração ou delegação
- núcleos transversais que não pertencem a um módulo específico

O critério não é remover tudo do root. O critério é remover wiring interno de módulo e manter apenas o que é realmente global.

## Fail-Fast Rules
O pipeline deve falhar cedo quando houver:
- dependência ausente
- dependência inválida
- ID duplicado
- ciclo no grafo
- bootstrap solicitado antes do fim dos installers da sua fase

Fail-fast é obrigatório para preservar previsibilidade e evitar regressões silenciosas na composição modular.

## Positive References
- `GameLoop` validou a separação entre installer e runtime bootstrap
- `SceneFlow` validou a composição runtime após registro
- `Navigation` validou o split entre registro e dispatch/runtime
- `LevelFlow` validou o mesmo padrão para wiring dependente de `Navigation`

## Recommended Pilot
O modelo canônico já está validado pelos pilotos citados. A partir daqui, novos módulos devem adotar a mesma regra sem reabrir ownership de domínio.

## Non-Goals
- não criar novo ADR para este mesmo assunto
- não migrar todo o projeto de uma vez
- não remover imediatamente o `GlobalCompositionRoot`
- não criar sistema de plugin automático
- não reorganizar pastas neste ADR
- não reabrir ownership de domínio

## Consequences
- installers ficam menores e previsíveis
- bootstraps/composers ficam isolados da fase de registro
- o root global fica menor, mais claro e mais seguro
- a ordem de boot passa a ser determinística e auditável
- regressões por mistura de boot com pipeline tendem a cair
- módulos futuros passam a seguir um contrato reutilizável e homogêneo

## Validation Status
O modelo foi validado na prática em `GameLoop`, `SceneFlow`, `Navigation`, `LevelFlow` e `WorldReset`.

O pipeline em duas fases com dependências explícitas está operacional: installers primeiro, bootstraps/composers depois, com ordenação topológica e fail-fast.

O `GlobalCompositionRoot` permanece como agregador dos descriptors, executor do grafo e orquestrador das fases globais.
