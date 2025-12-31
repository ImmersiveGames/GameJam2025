# Relatório de Sistemas Incompletos — NewScripts

> **Escopo:** apenas `Assets/_ImmersiveGames/NewScripts`.
> **Critério:** sistema incompleto = fluxo intencional ainda não fechado, ou depende de peças ausentes para uso produtivo.

## 1) Start da run (intenção → transição → gameplay ativo)
**Situação:** o fluxo de start depende de `GameStartRequestedEvent`, mas não existe um emissor explícito no frontend/gameplay para disparar esse evento. O `MenuPlayButtonBinder` hoje dispara apenas a navegação de cenas.

**Impacto:** o `GameLoopSceneFlowCoordinator` pode ficar sem gatilho para iniciar o GameLoop após o carregamento.

**Melhorias recomendadas:**
- Publicar `GameStartRequestedEvent` no clique do botão Play **ou** após `SceneTransitionScenesReadyEvent` quando a cena de gameplay estiver pronta.
- Criar um binder dedicado no menu ou uma bridge de SceneFlow → GameLoop para garantir start determinístico.

---

## 2) Resultado da partida (Victory/Defeat)
**Situação:** o pipeline de pós-game depende de `GameRunEndedEvent`, mas não há um sistema de gameplay real emitindo esse evento (há suporte a QA e UI, mas não a lógica final de jogo).

**Impacto:** `PostGameOverlayController` e `GameRunStatusService` não são acionados em partidas reais.

**Melhorias recomendadas:**
- Implementar um “GameOutcomeResolver” que observe condições de vitória/derrota e publique `GameRunEndedEvent`.
- Centralizar a emissão do evento (evitar múltiplas fontes concorrentes).

---

## 3) Input do Player no multiplayer local
**Situação:** o `NewPlayerInputReader` usa `Input.GetAxis` (Input Manager antigo), enquanto o projeto também utiliza `PlayerInput` e `InputModeService` (Input System novo). Isso limita a escalabilidade para múltiplos players.

**Impacto:** múltiplos players locais não terão controle isolado por device/action map.

**Melhorias recomendadas:**
- Migrar o leitor de input do Player para `InputActionAsset` via `PlayerInput`.
- Isolar inputs por jogador (ex.: `PlayerInput` por instância) mantendo compatibilidade com `InputModeService`.

---

## 4) Movimento/AI do Eater
**Situação:** o `NewEaterRandomMovementController` é um placeholder com movimento aleatório simples e sem integração com reset de gameplay.

**Impacto:** comportamento limitado, sem reações a objetivos, sem reposicionamento consistente após reset.

**Melhorias recomendadas:**
- Adicionar implementação de `IGameplayResettable` para reiniciar estado do Eater.
- Evoluir IA (ex.: perseguição, pathfinding, limites de arena, reação a player).

---

## 5) UI Pós-Game (Input Mode)
**Situação:** o `PostGameOverlayController` não altera o modo de input (UI/Gameplay) ao abrir o overlay.

**Impacto:** dependendo da configuração, inputs de gameplay podem continuar ativos durante o pós-game.

**Melhorias recomendadas:**
- Integrar `IInputModeService` ao overlay para alternar para modo UI quando o pós-game estiver visível.

---

## 6) Catálogo de cenas (Navigation)
**Situação:** `GameNavigationCatalog` possui um placeholder explícito para o nome real da cena de gameplay.

**Impacto:** navegação pode falhar se o nome da cena não for ajustado.

**Melhorias recomendadas:**
- Atualizar o catálogo com os nomes reais das cenas (Menu, Gameplay, UIGlobal).
- Validar nomes durante bootstrap (fail fast com logs claros).

---

## Conclusão
As lacunas identificadas são **principalmente de integração** (start/end de run e input multiplayer). O core de infraestrutura já é sólido, mas precisa dessas peças finais para fechar o ciclo de jogo completo.

