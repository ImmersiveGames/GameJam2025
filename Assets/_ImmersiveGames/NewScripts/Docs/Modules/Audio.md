# Audio Module Closure

## 1. Resumo executivo
- O Audio deixou de ser uma fronteira de arbitragem de dominio e passou a ser um modulo de playback.
- O shape-alvo final e simples: core runtime, bridges/adapters finos, authoring direto por cue/profile e bindings locais.
- Foram fechadas as ambiguidades de BGM, removida a semantica-first de entidade e decomposto o service monolitico de SFX.
- A dependência de pool passou a ser explicitamente injetada no composition root.
- O modulo agora esta coerente com a arquitetura consolidada do projeto e pronto para crescer sem trilhas antigas no centro.

## 2. Problema anterior
O modulo de Audio acumulava responsabilidades que pertenciam a outras boundaries: arbitrava fonte de verdade de BGM, mantinha uma camada semantic-first para entidade, escondia dependencias e concentrava policy, pooling e execucao no mesmo service. Isso misturava dominio com mecanismo tecnico e enfraquecia a leitura da arquitetura.

## 3. Shape anterior
- BGM com mais de uma fonte de verdade.
- Entity audio guiado por purpose maps e semantic service.
- `AudioGlobalSfxService` concentrando execucao, policy e pooling.
- Dependencias de infraestrutura resolvidas por locator runtime dentro do trilho de SFX.
- QA/editor ainda apontando para trilhas antigas.

## 4. Shape atual
- Core runtime de playback: `BGM`, `SFX`, `settings`, `routing` e `handles`.
- Bridges/adapters finos para integrar Audio com o resto do jogo.
- Authoring direto por cue/profile, sem malha semantica paralela.
- Bindings locais como `EntityAudioEmitter`, sem decisao de dominio.
- Dependencias estruturais explicitadas no composition root.

## 5. Mudancas arquiteturais consolidadas
- O ownership de BGM foi fechado fora do Audio, eliminando arbitragens internas de dominio.
- `EntityAudioEmitter` foi reduzido a binding puro de contexto e playback explicito.
- A camada semantic-first de entidade foi removida do wiring e depois removida do modulo.
- `AudioGlobalSfxService` foi decomposto em orquestracao, execution, pooling e policy.
- `IPoolService` deixou de ser resolvido por locator runtime e passou a entrar explicitamente pelo bootstrap.
- O composition root do Audio agora representa o shape real do modulo, nao uma composicao historica.

## 6. O que saiu da arquitetura-alvo
- `EntityAudioSemanticMapAsset`
- `AudioEntitySemanticService`
- `IEntityAudioService`
- `AudioSfxCueMigrationValidator`
- `AudioEntitySemanticQaSceneHarness`
- Qualquer uso semantic-first como primitive arquitetural do Audio

## 7. O que continua como core
- `AudioBgmService`
- `AudioGlobalSfxService`
- `AudioSettingsService`
- `AudioRoutingResolver`
- `AudioPlaybackContext`
- `AudioSfxCueAsset` e profiles diretos de authoring
- `EntityAudioEmitter` como binding local

## 8. Fora de escopo
- Reabrir `GameplaySessionFlow`, `GameLoop`, `SceneFlow`, `WorldReset` ou `Navigation`.
- Voltar a introduzir compat layers semantic-first.
- Expandir a limpeza para cosmetica ampla de QA/editor.
- Reescrever o motor de SFX ou o backbone do projeto.

## 9. Fechamento
A fase pode ser considerada encerrada porque o modulo de Audio agora tem ownership claro, bordas explicitas e um shape alinhado ao papel real que deve cumprir. As trilhas antigas que confundiam dominio, mecanismo e compatibilidade foram removidas do centro da arquitetura. O que sobra e o nucleo util do modulo, pronto para evoluir sem carregar a forma antiga.
