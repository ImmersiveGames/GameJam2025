# ADR-0038 — Registro modular de DI e composição runtime por módulo

## Status
- Estado: Implementado
- Data: 2026-03-27
- Escopo: `Infrastructure/Composition`, descriptors de composição, `GlobalCompositionRoot`, boot modular de `NewScripts`

## Contexto

O modelo anterior misturava:

- registro de serviços;
- composição runtime;
- wiring interno de módulo no root global;
- entrypoints implícitos de bootstrap.

Esse desenho gerava:

- dependências invertidas;
- ordem de boot frágil;
- wiring acidental fora do módulo dono;
- dificuldade de auditoria por log;
- regressões silenciosas quando um módulo dependia de outro sem contrato explícito.

A necessidade desta decisão é formalizar o pipeline modular de composição de `NewScripts` com:

- duas fases globais;
- dependências explícitas por fase;
- fail-fast;
- idempotência;
- observabilidade mínima;
- fronteira auditável entre o que pertence ao módulo e o que pode permanecer no `GlobalCompositionRoot`.

## Decisão

### 1. Modelo canônico de duas fases

A composição modular de `NewScripts` passa a seguir obrigatoriamente duas fases globais:

1. **Fase 1 — Registration**
    - todos os `Installers` executam primeiro;
    - apenas registram contratos, serviços, configs, providers, registries e pré-requisitos do módulo.

2. **Fase 2 — Runtime Composition**
    - todos os `Bootstraps` / `Runtime Composers` executam depois;
    - compõem runtime usando apenas o DI já preenchido na Fase 1.

As fases são globais e ordenadas pelo pipeline.
O módulo continua dono do seu wiring interno.
O `GlobalCompositionRoot` apenas coordena a execução.

### 2. Descriptor canônico obrigatório

Todo módulo incluído no grafo deve expor um descriptor canônico com, no mínimo:

- `ModuleId`
- `InstallerEntry`
- `RuntimeComposerEntry`
- `InstallerDependencies`
- `BootstrapDependencies`
- `Optional`
- `InstallerOnly`
- `Description`

Regras:

- `ModuleId` é obrigatório, estável e único no grafo.
- `InstallerEntry` é obrigatório para módulo que participa da Fase 1.
- `RuntimeComposerEntry` é obrigatório apenas para módulo que participa da Fase 2.
- `InstallerOnly = true` implica:
    - `RuntimeComposerEntry = null`
    - `BootstrapDependencies = []`
- `Optional = true` só pode ser usado quando a omissão do módulo for decisão explícita do profile de boot.
- O descriptor não pode inferir identidade, dependência ou fase por nome de pasta, reflexão opaca ou convenção implícita.

### 3. Fronteira dura entre Installer e Bootstrap

#### O que pode entrar no Installer
- serviços
- interfaces
- providers
- factories
- configs
- registries
- contratos necessários para o bootstrap do próprio módulo

#### O que não pode entrar no Installer
- ativação de runtime
- orchestrators
- bridges
- listeners operacionais
- wiring de domínio
- composição final de runtime

#### O que pode entrar no Bootstrap / Runtime Composer
- composição entre serviços já registrados
- ativação de orchestrators, bridges, hosts e wiring operacional
- integração explícita entre contratos já instalados
- montagem final do runtime do módulo

#### O que não pode entrar no Bootstrap / Runtime Composer
- registro tardio de contratos estruturais para viabilizar o próprio boot
- compensação de ausência de installer
- entrypoint canônico por `Awake`, `Start` ou `OnEnable`

Bootstrap canônico só pode executar pela Fase 2 orquestrada do pipeline.

### 4. Dependências por fase

As dependências são separadas formalmente em:

- `InstallerDependencies`
- `BootstrapDependencies`

Regras:

- dependência ausente é erro fatal;
- dependência inválida é erro fatal;
- `ModuleId` duplicado é erro fatal;
- ciclo no grafo é erro fatal;
- dependências não podem ser inferidas por transitividade implícita;
- módulo `InstallerOnly` não pode ser dependência de bootstrap sem contrato correspondente;
- bootstrap não pode mascarar ausência de installer com registro tardio.

### 5. Optionalidade

Regras:

- `Optional = false`: o módulo não pode ser omitido pelo profile de boot quando sua presença for requerida pelo grafo selecionado.
- `Optional = true`: a omissão precisa ser explícita e auditável.
- módulo opcional pulado deve registrar `skip` com motivo e fase.
- módulo ausente do grafo, mas referenciado como obrigatório, é erro fatal.
- módulo opcional não pode virar dependência obrigatória silenciosa de outro módulo.

### 6. Idempotência e lifecycle

O pipeline deve ser idempotente por contrato:

- chamada duplicada de installer no mesmo domain lifetime deve resultar em `no-op` controlado e logado;
- chamada duplicada de bootstrap/composer no mesmo domain lifetime deve resultar em `no-op` controlado e logado;
- `Domain Reload OFF` não pode causar registro duplicado nem dupla composição;
- reentrada de boot não pode recriar o grafo se a composição global já concluiu com sucesso;
- `RestartFromFirstLevel` e `ResetCurrentLevel` não reiniciam a composição global.

Comportamento não idempotente é inválido para entrypoints de composição.

### 7. Fail-fast e falha parcial

Política mínima:

- falha em installer aborta a Fase 1 e impede a Fase 2;
- falha em bootstrap aborta a Fase 2 e coloca o sistema em fail-stop até restart limpo;
- falha por dependência ausente, ciclo ou `ModuleId` duplicado deve ocorrer antes da execução do entrypoint afetado;
- estado parcial não pode continuar como se a composição tivesse concluído;
- rollback não é obrigatório por padrão; módulos com mutação externa devem ser idempotentes ou ter compensação própria.

### 8. Observabilidade mínima

O pipeline deve emitir, no mínimo:

- início e fim da Fase 1;
- início e fim da Fase 2;
- ordem calculada dos módulos por fase;
- `installer concluído`;
- `runtime composition concluída`;
- `skip` canônico com motivo (`optional`, `installer-only`);
- dependência ausente;
- ciclo detectado;
- `ModuleId` duplicado;
- tentativa de bootstrap antes do fim da Fase 1;
- resumo final do pipeline.

Política de severidade:

- sucesso canônico do pipeline: `INFO`
- `skip` canônico: `INFO`
- detalhe interno/repetitivo: `VERBOSE`
- falha de conformidade do pipeline: `ERROR`

### 9. Fronteira auditável do GlobalCompositionRoot

Pode permanecer no `GlobalCompositionRoot` apenas o que é estritamente global:

- entry do pipeline;
- resolução e execução das fases;
- helpers globais de infraestrutura;
- infra transversal compartilhada;
- bridges globais estritamente compartilhadas;
- shims finos de orquestração ou delegação.

Não pode permanecer no `GlobalCompositionRoot`:

- wiring interno de módulo;
- conhecimento de tipos internos de módulo além do entrypoint contratual;
- lógica operacional específica de domínio;
- composição escondida por convenção de pasta ou lifecycle implícito.

Critério auditável:

> se um trecho existe apenas para um módulo específico, ele pertence ao módulo, não ao root global.

### 10. Convenção de nomenclatura

Padrão canônico esperado:

- `XxxCompositionDescriptor`
- `XxxInstaller`
- `XxxBootstrap` ou `XxxRuntimeComposer`

Localização padrão:

- `Modules/<ModuleName>/Bootstrap/`

IDs:

- `ModuleId` em `PascalCase`
- sem prefixo técnico
- sem duplicar o nome da pasta
- sem variantes instáveis entre fases

### 11. Papel de Loading neste ciclo

`Loading` **não é módulo first-class** neste ciclo.

Contrato atual:

- `Loading` é subcapability de `SceneFlow`;
- não possui descriptor próprio;
- não possui installer próprio;
- não possui bootstrap próprio;
- não possui entrada própria no grafo modular.

Promoção futura só deve ocorrer quando houver:

- contrato independente;
- `ModuleId` próprio;
- dependências de fase explícitas;
- ownership estrutural fora de `SceneFlow`.

## Fora de escopo

- promover `Loading` a módulo próprio neste ciclo;
- modularizar `Pause` nesta decisão;
- remover imediatamente o `GlobalCompositionRoot`;
- migrar todo o projeto de uma vez;
- criar sistema de plugin automático;
- reorganizar o repositório inteiro;
- reabrir ownership funcional de domínio.

## Consequências

### Benefícios
- boot determinístico e auditável;
- separação clara entre registro e composição runtime;
- redução de wiring interno no root global;
- contratos modulares homogêneos;
- menor risco de bootstrap invisível;
- melhor leitura de sucesso/skip/falha em produção.

### Trade-offs / Riscos
- exige disciplina na modelagem de descriptors;
- aumenta a necessidade de declarar dependências explicitamente;
- módulos legados ou híbridos podem exigir adaptação incremental;
- alguns concerns transversais ainda podem permanecer temporariamente no root até ganharem owner claro.

## Notas de implementação

### Estado consolidado da rodada
Na consolidação desta rodada:

- o pipeline modular passou a operar com Fase 1 e Fase 2 explícitas;
- logs canônicos do pipeline foram promovidos para `INFO`;
- `Gameplay` foi promovido a módulo `installer-only`;
- `GameplayStateGate` e `GameplayCameraResolver` saíram do root e passaram ao módulo `Gameplay`;
- `GamePauseGateBridge` saiu do root e foi movido para `GameLoopBootstrap`;
- `PostGame` e `WorldReset` ficaram explícitos como `installer-only`;
- `Loading` foi mantido como subcapability de `SceneFlow`;
- `IInputModeService` permaneceu no root como trilho transversal global legítimo nesta passada.

### Resultado esperado de auditoria
Ao auditar o repositório contra este ADR, espera-se encontrar:

- descriptors canônicos para os módulos do grafo;
- ausência de bootstrap canônico por `Awake`, `Start` ou `OnEnable`;
- dependências de fase explicitadas;
- `skip` canônico de módulos opcionais ou `installer-only`;
- root restrito a pipeline + infra global legítima.

## Evidências

- `Baseline 3.5`
- auditorias recentes de `module registration` vs `runtime composition`
- pilotos validados em:
    - `GameLoop`
    - `SceneFlow`
    - `Navigation`
    - `LevelFlow`
    - `WorldReset`
    - `Gameplay`
    - `PostGame`
- logs canônicos do pipeline com:
    - Fase 1 em `INFO`
    - Fase 2 em `INFO`
    - installers concluídos em `INFO`
    - runtime composition concluída em `INFO`
    - `skip` canônico em `INFO`

## Referências

- `ADR-0030`
- `ADR-0031`
- `ADR-0035`
- `ADR-0037`

## Checklist de conformidade

- [ ] Todo módulo do grafo expõe descriptor canônico.
- [ ] `ModuleId` é único e estável.
- [ ] `InstallerDependencies` estão explícitas.
- [ ] `BootstrapDependencies` estão explícitas.
- [ ] Não existe bootstrap canônico via `Awake`, `Start` ou `OnEnable`.
- [ ] Nenhum contrato estrutural é registrado na fase errada.
- [ ] Módulos `optional` registram `skip` auditável.
- [ ] Módulos `installer-only` são tratados explicitamente.
- [ ] O pipeline emite ordem, conclusão e resumo final.
- [ ] O `GlobalCompositionRoot` não contém wiring interno de módulo.
- [ ] `Loading` permanece em `SceneFlow` até haver contrato próprio.
