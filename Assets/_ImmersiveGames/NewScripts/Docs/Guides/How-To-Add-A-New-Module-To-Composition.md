# How To Add A New Module To Composition

## 1. When a Module Needs Installer Only
Use somente installer quando o módulo só precisa registrar serviços, contratos e helpers pre-runtime.

Exemplo semelhante:
- `WorldReset`

## 2. When a Module Needs Installer + Bootstrap
Use as duas fases quando o módulo precisa registrar primeiro e depois compor runtime operacional.

Exemplos semelhantes:
- `GameLoop`
- `SceneFlow`
- `Navigation`
- `LevelFlow`

Nota:
- `Loading` nao entra aqui como modulo proprio; neste ciclo ele permanece como subcapability de `SceneFlow`.

## 3. Required Files
- `XxxInstaller`
- `XxxBootstrap` quando houver composição runtime real
- `XxxCompositionDescriptor`

## 4. Minimal Responsibilities
Installer:
- registra serviços, contratos, providers, factories e configs
- não compõe runtime
- não depende de bootstrap

Bootstrap:
- compõe runtime
- ativa bridges, coordinators e orchestrators
- usa apenas o DI já preenchido
- se o modulo possuir loading embutido, isso continua no ownership do modulo pai e nao vira grafo separado

Descriptor:
- expõe `ModuleId`
- expõe `InstallerEntry`
- expõe `RuntimeComposerEntry` quando houver runtime
- expõe `InstallerDependencies`
- expõe `BootstrapDependencies`
- expõe `Optional`
- expõe `InstallerOnly`
- referencia installer e bootstrap do módulo

## 5. Declaring Dependencies
Declare dependências explicitamente por fase:
- `InstallerDependencies`
- `BootstrapDependencies`

Regras:
- installer não depende de bootstrap
- bootstrap pode depender de bootstrap e de serviços já registrados
- a ordem não vem de prioridade numérica manual
- `InstallerOnly=true` exige bootstrap nulo
- módulo opcional só pode ser pulado de forma explícita no profile de boot

## 6. Registering the Module in the Root
O root apenas agrega o descriptor do módulo no grafo.

O root não deve conhecer o wiring interno do módulo.

## 7. Fail-Fast Rules
O pipeline deve falhar cedo em:
- dependência ausente
- dependência inválida
- ID duplicado
- ciclo
- dependência declarada na fase errada

## 8. Practical Checklist
- criar `XxxInstaller`
- criar `XxxCompositionDescriptor`
- criar `XxxBootstrap` somente se houver runtime real
- declarar dependências explícitas
- adicionar o descriptor ao grafo global
- verificar se o módulo não exige serviços de runtime na fase de installer

## 9. Common Mistakes
- misturar installer com runtime
- colocar wiring interno no root
- depender de bootstrap na fase de installer
- criar bootstrap artificial sem necessidade
- usar prioridade numérica como source-of-truth principal
