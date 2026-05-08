# Recursos do NewScripts

Este diretório centraliza todos os **ScriptableObject concretos** e **assets de configuração** utilizados pelo módulo NewScripts e seus subsistemas.

> **Política**: Todos os assets concretos devem residir em `Assets/_ImmersiveGames/NewScripts/Resources`. Nenhum asset concreto deve estar em `Assets/Resources` fora de NewScripts.

## Estrutura de Diretórios

### 📦 Bootstrap
Configurações iniciais do sistema e bootstrap.
- `BootstrapConfig.asset` — Configuração principal de bootstrap
- `RuntimeModeConfig.asset` — Modo de execução (Strict/Release/Auto) e políticas
- `RuntimePersistentScenesPolicyAsset.asset` — Definição de cenas persistentes

### 📦 Core
Configurações fundamentais de infraestrutura.
- `DOTweenSettings.asset` — Configuração do DOTween
- `LoggingConfig.asset` — Política e nível de logging

### 📦 SceneFlow
Tudo relacionado ao fluxo de cenas, fases e transições.

#### Phases/
Definições e catálogos de fases.
- `PhaseDefinitionCatalogExample.asset` — Catálogo exemplo de definições
- `PhaseDefinitionExample.asset` — Exemplo de definição de fase
- `PhaseDefinitionExample02.asset` — Segundo exemplo da definição
- `PhaseTestSlice01Scene.asset` — Teste de cena slice 01
- `PhaseTestSlice02Scene.asset` — Teste de cena slice 02

#### Profiles/
Perfis de transição e configurações de cena.
- `DefaultTransitionProfile.asset` — Perfil padrão de transição de cena

#### Routes/
Definições de rotas de navegação.
- `Route_bootMenu.asset` — Rota para menu de boot
- `Route_menu-gameplay.asset` — Rota de menu para gameplay

### 📦 Navigation
Configurações de navegação e carregamento.
- `RuntimeLoadingProfileAsset.asset` — Perfil padrão de carregamento em tempo de execução

### 📦 Animation
Configurações de animação por tipo de ator.
- `DefaultEaterAnimationConfig.asset` — Config padrão de animação do "Eater"
- `DefaultEnemyAnimationConfig.asset` — Config padrão de animação de inimigos
- `DefaultPlayerAnimationConfig.asset` — Config padrão de animação do jogador

### 📦 Audio
Configurações de áudio e mixers.

#### Mixers/
Mixer de áudio principal.
- `MainAudioMixer.mixer` — Unity Audio Mixer principal

### 📦 Video
Configurações e defaults de vídeo.
- `VideoDefaults.asset` — Configurações padrão de vídeo

### 📦 ActorsSystem
Catálogos e especificações de atores.
- `ActorSetCatalog.asset` — Catálogo de conjuntos de atores
- `ActorSpecsCatalog.asset` — Catálogo de especificações de atores

## Carregamento via Resources

Todos esses assets são carregados pelo sistema via `Resources.Load<T>()` ou `Resources.LoadAsync<T>()`.

### Exemplos

```csharp
// Carregar BootstrapConfig
var bootstrapConfig = Resources.Load<BootstrapConfig>("Bootstrap/BootstrapConfig");

// Carregar RuntimeModeConfig
var runtimeModeConfig = Resources.Load<RuntimeModeConfig>("Bootstrap/RuntimeModeConfig");

// Carregar SceneFlow profile
var transitionProfile = Resources.Load<SceneTransitionProfile>("SceneFlow/Profiles/DefaultTransitionProfile");

// Carregar rota
var route = Resources.Load<SessionOperationalRouteAsset>("SceneFlow/Routes/Route_bootMenu");
```

## Histórico de Reorganização

**Data**: 8 de Maio de 2026

- ✅ Todos os assets concretos de `Assets/Resources` foram consolidados em `Assets/_ImmersiveGames/NewScripts/Resources`
- ✅ Estrutura reorganizada por domínio (Bootstrap, Core, SceneFlow, Navigation, Animation, Audio, Video, ActorsSystem)
- ✅ Pasta `Assets/Resources` está agora vazia

## Adicionando Novos Assets

Ao adicionar novos ScriptableObject concretos:

1. **Identifique o domínio**: A qual módulo ou subsistema pertence?
2. **Use a pasta correta**: Coloque em `Assets/_ImmersiveGames/NewScripts/Resources/<DOMINIO>/`
3. **Mantenha a hierarquia**: Use subpastas quando necessário (ex: `Audio/Mixers/`, `SceneFlow/Phases/`)
4. **Nunca** coloque assets em `Assets/Resources`

## Convenções de Nomenclatura

- Asset files: PascalCase (ex: `BootstrapConfig.asset`)
- Pastas: Singular ou conceitual (ex: `Bootstrap`, não `Bootstraps`)
- Rotas: Prefixo `Route_` (ex: `Route_bootMenu.asset`)
- Fases: Prefixo `Phase` ou sufixo `Definition` (ex: `PhaseDefinitionExample.asset`)

## Troubleshooting

**Problema**: Asset não carrega via `Resources.Load()`
- Verifique se o caminho é relativo a `Resources/` (ex: `Bootstrap/BootstrapConfig`, não o caminho completo)
- Certifique-se de que o arquivo tem `.asset` na extensão
- Não use path separators em paths que contém subpastas: use `/` ou apenas o nome da pasta

**Problema**: Referências quebradas após reorganização
- Reimporte os assets no Unity Editor (Assets > Reimport All)
- Revise scripts que fazem `Resources.Load()` manualmente — o Unity não atualiza essas referências automaticamente

