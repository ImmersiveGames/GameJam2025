# How To Add A New Module To Composition

## Regra base

- Use `Installer` quando o modulo so registra contratos, providers, factories e configuracao pre-runtime.
- Use `Installer + Bootstrap` quando o modulo tambem compoe runtime operacional.
- Nao crie trilho novo se a camada atual ja tem owner claro.
- Se o contexto tocar gameplay/session, leia a topologia via `Base 1.0` antes de decidir o boundary.

## Onde isso se aplica hoje

- `GameLoop`
- `SceneFlow`
- `Navigation`
- `Save`

`LevelFlow` aparece apenas como nome historico do boundary de conteudo.
Quando o assunto e fluxo ativo de level, a leitura operacional deve passar por `Base 1.0` e `GameplaySessionFlow`; `LevelLifecycle` nao deve ser tratado como owner semantico final.

## Arquivos obrigatorios

- `XxxInstaller`
- `XxxBootstrap` quando houver composicao runtime real
- `XxxCompositionDescriptor`

## Responsabilidades minimas

Installer:

- registra servicos, contratos, providers, factories e configs.
- nao compoe runtime.
- nao depende de bootstrap.

Bootstrap:

- compoe runtime.
- ativa bridges, coordinators e orchestrators.
- usa apenas o DI ja preenchido.
- mantem loading dentro da camada dona; nao cria grafo paralelo.

Descriptor:

- expõe `ModuleId`.
- expõe `InstallerEntry`.
- expõe `RuntimeComposerEntry` quando houver runtime.
- expõe `InstallerDependencies`.
- expõe `BootstrapDependencies`.
- expõe `Optional`.
- expõe `InstallerOnly`.

## Dependencias por fase

Declare dependencias explicitamente:

- `InstallerDependencies`
- `BootstrapDependencies`

Regras:

- installer nao depende de bootstrap.
- bootstrap pode depender de bootstrap e de servicos ja registrados.
- a ordem nao vem de prioridade numerica manual.
- `InstallerOnly=true` exige bootstrap nulo.
- modulo opcional so pode ser pulado de forma explicita no profile de boot.

## Root

- O root apenas agrega o descriptor do modulo no grafo.
- O root nao deve conhecer o wiring interno do modulo.

## Fail-fast

O pipeline deve falhar cedo em:

- dependencia ausente.
- dependencia invalida.
- ID duplicado.
- ciclo.
- dependencia declarada na fase errada.

## Checklist rapido

- criar `XxxInstaller`.
- criar `XxxCompositionDescriptor`.
- criar `XxxBootstrap` somente se houver runtime real.
- declarar dependencias explicitas por fase.
- adicionar o descriptor ao grafo global.
- verificar se o modulo nao exige servicos de runtime na fase de installer.

## Erros comuns

- misturar installer com runtime.
- colocar wiring interno no root.
- depender de bootstrap na fase de installer.
- criar bootstrap artificial sem necessidade.
- usar prioridade numerica como source of truth.
