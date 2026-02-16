# ADR-0019 — Navigation IntentCatalog: Core Slots explícitos + Extras extensíveis

## Status

- Estado: Concluído
- Data (decisão): 2026-02-16
- Última atualização: 2026-02-16 (rev. Core vs Custom + aliases)
- Tipo: Implementação
- Escopo: Navigation (`GameNavigationCatalogAsset`), contratos de configuração de intents
- Decisores: Time NewScripts (Navigation/SceneFlow)
- Tags: Navigation, Catalog, FailFast, Observability

## Contexto

Atualmente, parte do fluxo de navegação depende de ids string (ex.: `to-menu`, `to-gameplay`) propagados em múltiplos pontos de configuração.
Esse modelo aumenta risco de erro por digitação, divergência de nomenclatura entre assets e dificuldade para garantir consistência dos intents essenciais.

Além disso, a navegação possui intents de domínio considerados estruturais do loop do jogo, que precisam de contrato explícito e verificável:

- Menu
- Gameplay
- GameOver
- Victory
- Restart
- ExitToMenu

Ao mesmo tempo, o catálogo precisa continuar extensível para intents não-core (customizações de conteúdo/projetos futuros) sem quebrar o contrato dos intents essenciais.

## Decisão

### Objetivo de produção (sistema ideal)

`GameNavigationCatalogAsset` passa a modelar intents de navegação com duas camadas:

1. **Core intents como slots explícitos** (campos dedicados no catálogo).
2. **Intents extras como lista extensível** (ids string para casos não-core).

No runtime, o consumo dos intents core deve ocorrer por enum forte (`GameNavigationIntentKind`), removendo dependência direta de strings para os fluxos essenciais.


### Modelo canônico atual: Core vs Custom

- **Core**: intents oficiais suportadas pelo produto e mantidas no bloco `Core` do `GameNavigationIntentCatalogAsset`.
- **Custom**: intents não-canônicas (projeto/feature específica) mantidas no bloco `Custom`.

Core oficial (baseline atual):

1. `to-menu` → `Route_to-menu.asset` + `style.frontend` (**crítica/required**)
2. `to-gameplay` → `Route_to-gameplay.asset` + `style.gameplay` (**crítica/required**)
3. `exit-to-menu` → `Route_to-menu.asset` + `style.frontend` (alias core, não-crítica)
4. `restart` → `Route_to-gameplay.asset` + `style.gameplay` (alias core, não-crítica)

> `to-menu` e `to-gameplay` permanecem como intents críticas de baseline para validação fail-fast.

### Contrato de produção (mínimo)

1. Os intents core são representados no bloco `Core` do catálogo (com suporte a aliases oficiais).
   - Baseline obrigatório: `to-menu`, `to-gameplay` (críticos).
   - Aliases core (não-críticos por padrão): `exit-to-menu`, `restart`.

2. Runtime para core usa enum:
   - API de resolução/consulta de intent core recebe `GameNavigationIntentKind`.
   - Mapeamento enum -> slot é determinístico e único.

3. Intents extras permanecem suportados por lista:
   - Identificados por id string.
   - Mantêm compatibilidade para extensões de conteúdo fora do núcleo.

4. Política de integridade para core é estrita:
   - Slot core obrigatório ausente/inválido deve ser tratado como erro fatal de configuração.
   - Não há fallback silencioso para preencher ou inferir slot core.

5. Observabilidade:
   - Logs `[OBS]` devem continuar evidenciando resolução por AssetRef para rotas críticas/core quando aplicável.

### Não-objetivos (resumo)

- Não redefinir o ciclo de vida global do jogo.
- Não introduzir novo manifest/provider em cena.
- Não substituir o mecanismo de extras por enum; extras seguem string por decisão de extensibilidade.

## Fora de escopo

- Alterar `GameLoop` ou `WorldLifecycle`.
- Refatorar todos os chamadores legados em uma única etapa.
- Mudar políticas de módulos não relacionados ao catálogo de intents.

## Política de falhas e fallback (fail-fast)

- **Editor:** para slots core obrigatórios, qualquer configuração incompleta deve falhar em validação com log fatal + exceção (`throw`).
- **Runtime (strict):** ao resolver/usar intent core inválido ou sem `routeRef` obrigatório, falhar imediatamente (fatal), sem fallback degradado.
- **Extras:** podem manter política de compatibilidade temporária por id, desde que não comprometam os contratos dos core intents.

## Consequências

### Benefícios

- Reduz acoplamento frágil por strings nos fluxos centrais.
- Aumenta segurança de configuração no Editor para intents essenciais.
- Facilita evolução incremental: núcleo tipado + extensão aberta.

### Custos / Riscos

- Exige disciplina de migração dos consumidores core para enum.
- A coexistência core (slots) + extras (lista) adiciona uma camada de governança de catálogo.

### Critérios de pronto (DoD)

- [ ] `GameNavigationIntentCatalogAsset` possui separação explícita `Core` vs `Custom`.
- [ ] Bloco `Core` contém `to-menu`, `to-gameplay`, `exit-to-menu`, `restart` com direct refs válidas (routeRef + styleId).
- [ ] Runtime core consome intents via `GameNavigationIntentKind`.
- [ ] Editor aplica fail-fast (fatal + throw) para qualquer slot core obrigatório inválido/incompleto.
- [ ] Runtime strict falha sem fallback silencioso para core inválido.
- [ ] Extras continuam extensíveis por lista id string.
- [ ] Logs `[OBS]` comprovam resolução via AssetRef nos casos core críticos (`to-menu`, `to-gameplay`).

## Implementação (arquivos impactados)

- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs` (alvo da modelagem de slots core + extras)
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/*` (APIs/runtime de intents core por enum)

> Nota: este ADR registra a decisão arquitetural. Implementações de runtime além deste escopo devem ser planejadas em etapas próprias.

## Evidência

- Última evidência (log bruto): `Docs/Reports/lastlog.log`
- Fonte canônica atual: `Docs/Reports/Evidence/LATEST.md`
- Âncoras/assinaturas relevantes: `[OBS][SceneFlow] RouteResolvedVia=AssetRef`, `[FATAL][Config]`
- Contrato de observabilidade: `Docs/Standards/Standards.md#observability-contract`

## Referências

- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0018-Fade-TransitionStyle-SoftFail.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md`
