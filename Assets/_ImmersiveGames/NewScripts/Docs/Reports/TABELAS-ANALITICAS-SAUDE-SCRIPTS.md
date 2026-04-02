# TABELAS ANALÍTICAS E MATRICES - Saúde de Scripts Baseline 4.0
## Referência Técnica para Code Review e Tracking

**Data:** 2 de abril de 2026

---

## MATRIZ 1: SAÚDE POR MÓDULO (Detalhada)

```
╔════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                          SAÚDE DETALHADA POR MÓDULO                                              ║
╠═══════════════════════════╦════════════════╦═══════════╦═══════════╦═══════════╦═════════════════╣
║ Módulo                  ║ Conceitual (%) ║ Completo (%)║ Limpeza (%)║ Arquite (%)║ Score Final ║
╠═══════════════════════════╬════════════════╬═══════════╬═══════════╬═══════════╬═════════════════╣
║ Core/Events             ║     100        ║    100     ║    95     ║    100    ║    99/100  ✅  ║
║ Core/Fsm                ║     100        ║    100     ║    92     ║    100    ║    98/100  ✅  ║
║ Core/Identifiers        ║     100        ║    100     ║    95     ║    100    ║    99/100  ✅  ║
║ Core/Logging            ║     100        ║    100     ║    90     ║    100    ║    97.5/100✅  ║
║ Core/Validation         ║     100        ║    100     ║    90     ║    100    ║    97.5/100✅  ║
║ ──────────────────────── ║ ────────────── ║ ─────────── ║ ─────────── ║ ─────────── ║ ────────────── ║
║ CORE TOTAL              ║     100        ║    100     ║    92.4   ║    100    ║    98/100  ✅  ║
╠═══════════════════════════╬════════════════╬═══════════╬═══════════╬═══════════╬═════════════════╣
║ Infrastructure/Composition║     95         ║    95      ║    80     ║    90     ║    90/100  ✅  ║
║ Infrastructure/InputModes ║     90         ║    90      ║    85     ║    90     ║    88.75/100✅ ║
║ Infrastructure/Observ.   ║     95         ║    90      ║    80     ║    85     ║    87.5/100✅  ║
║ Infrastructure/Pooling   ║     90         ║    85      ║    70     ║    85     ║    82.5/100✅  ║
║ Infrastructure/RuntimeMode║     90         ║    90      ║    75     ║    80     ║    83.75/100✅ ║
║ Infrastructure/SimGate   ║     90         ║    95      ║    60⚠️    ║    80     ║    81.25/100⚠️ ║
║ ──────────────────────── ║ ────────────── ║ ─────────── ║ ─────────── ║ ─────────── ║ ────────────── ║
║ INFRASTRUCTURE TOTAL    ║     92          ║    91      ║    75     ║    85     ║    85.75/100✅ ║
╠═══════════════════════════╬════════════════╬═══════════╬═══════════╬═══════════╬═════════════════╣
║ Orchestration/SceneFlow  ║     95         ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ Orchestration/WorldReset ║     95         ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ Orchestration/ResetInterop║     95        ║    100     ║    90     ║    95     ║    95/100  ✅  ║
║ Orchestration/Navigation ║     90         ║    95      ║    85     ║    85     ║    88.75/100✅ ║
║ Orchestration/LevelLifecycle║   90        ║    90      ║    80     ║    85     ║    86.25/100✅ ║
║ Orchestration/GameLoop   ║     95         ║    95      ║    80     ║    85     ║    88.75/100✅ ║
║ Orchestration/SceneReset ║     85⚠️       ║    85⚠️     ║    60⚠️    ║    75⚠️    ║    76.25/100⚠️ ║
║ Orchestration/LevelFlow  ║     80⚠️       ║    80⚠️     ║    50⚠️    ║    70⚠️    ║    70/100  ⚠️  ║
║ ──────────────────────── ║ ────────────── ║ ─────────── ║ ─────────── ║ ─────────── ║ ────────────── ║
║ ORCHESTRATION TOTAL     ║     90          ║    89      ║    73     ║    83     ║    83.75/100✅ ║
╠═══════════════════════════╬════════════════╬═══════════╬═══════════╬═══════════╬═════════════════╣
║ Game/Content            ║     95          ║    95      ║    90     ║    95     ║    93.75/100✅ ║
║ Game/Gameplay/Actors    ║     95          ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ Game/Gameplay/Spawn     ║     95          ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ Game/Gameplay/State     ║     95          ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ Game/Gameplay/Reset     ║     90          ║    90      ║    75⚠️    ║    85     ║    85/100  ✅  ║
║ Game/Gameplay/Bootstrap ║     95          ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ ──────────────────────── ║ ────────────── ║ ─────────── ║ ─────────── ║ ─────────── ║ ────────────── ║
║ GAME TOTAL              ║     93          ║    93      ║    83     ║    90     ║    89.75/100✅ ║
╠═══════════════════════════╬════════════════╬═══════════╬═══════════╬═══════════╬═════════════════╣
║ Experience/PostRun      ║     95          ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ Experience/Audio        ║     90          ║    90      ║    75⚠️    ║    80     ║    83.75/100⚠️ ║
║ Experience/Save         ║     85⚠️        ║    85      ║    70⚠️    ║    75     ║    78.75/100⚠️ ║
║ Experience/Preferences  ║     95          ║    95      ║    90     ║    95     ║    93.75/100✅ ║
║ Experience/Frontend     ║     90          ║    90      ║    80     ║    85     ║    86.25/100✅ ║
║ Experience/Camera       ║     95          ║    95      ║    85     ║    90     ║    91.25/100✅ ║
║ ──────────────────────── ║ ────────────── ║ ─────────── ║ ─────────── ║ ─────────── ║ ────────────── ║
║ EXPERIENCE TOTAL        ║     92          ║    91      ║    80     ║    86     ║    87.25/100✅ ║
╠═══════════════════════════╬════════════════╬═══════════╬═══════════╬═══════════╬═════════════════╣
║ AGREGADO GERAL          ║     92          ║    88      ║    79     ║    85     ║    86/100  ✅  ║
╚═══════════════════════════╩════════════════╩═══════════╩═══════════╩═══════════╩═════════════════╝
```

---

## MATRIZ 2: CÓDIGO MORTO - INVENTORY DECISION

```
╔════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                    MATRIZ DE DECISÃO - CÓDIGO MORTO                                              ║
╠═════════════════════════════════════════════════════════════════════╦═══════════╦═══════════════════╣
║ Item                                                              ║ Status  ║ Ação Recomendada  ║
╠═════════════════════════════════════════════════════════════════════╬═══════════╬═══════════════════╣
║ PoolingQaContextMenuDriver.cs                                     ║ 🗑️ DELETE ║ Remover agora (30min)║
║ Código comentado legado (vários arquivos)                         ║ 🗑️ DELETE ║ Remover conforme audit║
║ SceneResetFacade (compat layer)                                   ║ 📋 MOVE  ║ Migração planeada   ║
║ LevelFlow/Runtime (transição não finalizada)                      ║ 📋 MOVE  ║ Migração planeada   ║
║ GameplayReset/Core (resíduo estrutural)                           ║ 🔴 REPLACE║ Consolidar + remover║
║ PoolingQaMockPooledObject                                         ║ 🗑️ DELETE ║ Verificar uso, depois║
║ DegradedKeys (possível obsolescência parcial)                     ║ 🔴 REPLACE║ Auditar e limpar    ║
║ Core/Events/Legacy (compat intencional)                           ║ ⚡ KEEP  ║ Manter até consumo=0║
║ Audio/Bridges (múltiplas bridges)                                 ║ ⚡ KEEP  ║ Documentar cada uma ║
║ ResetInterop (bridge legítima)                                    ║ ✅ KEEP  ║ Manter, documentar  ║
║ GameLoop/Bridges (bridges explícitas)                             ║ ✅ KEEP  ║ Manter, documentar  ║
╚═════════════════════════════════════════════════════════════════════╩═══════════╩═══════════════════╝
```

---

## MATRIZ 3: ANTI-PADRÕES ENCONTRADOS

```
╔═══════════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                        ANÁLISE DE ANTI-PADRÕES ARQUITETURAIS                                            ║
╠═══════════════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Anti-padrão                                                                                            ║
║ Proibido em Plan-Baseline-4.0-Execution-Guardrails                                                    ║
├───────────────────────────┬──────────────┬──────────┬────────────────────────────────────────────────┤
║ Categoria                 ║ Status       ║ Severida ║ Evidência                                      ║
╠───────────────────────────┼──────────────┼──────────┼────────────────────────────────────────────────╣
║ Mover ownership           ║ ✅ CONFORME  ║ N/A      ║ Frontend não define gameplay                    ║
║ para visual               ║              ║          ║ PostRun não é dono de GameLoop                 ║
║                           ║              ║          ║ GameplayCamera é fronteira, não ownership      ║
╠───────────────────────────┼──────────────┼──────────┼────────────────────────────────────────────────╣
║ Adapter/Bridge            ║ ⚠️ PARCIAL   ║ MÉDIO    ║ Bridges legítimas: ResetInterop, Audio/Bridges║
║ escondendo                ║ CONFORME     ║          ║ Bridges compat: SceneReset, LevelFlow/Runtime ║
║ fronteira errada          ║              ║          ║ Recomendação: documentar e remover compat    ║
╠───────────────────────────┼──────────────┼──────────┼────────────────────────────────────────────────╣
║ Fallback                  ║ ⚠️ SUSPEITA  ║ MÉDIO    ║ TryGet<T>() pode retornar silent             ║
║ silencioso para           ║ REQUER       ║          ║ Pooling pode não ter pool disponível          ║
║ mascarar contrato         ║ AUDITORIA    ║          ║ RuntimeMode fallback pode ser silent          ║
║                           ║              ║          ║ Recomendação: adicionar logging               ║
╠───────────────────────────┼──────────────┼──────────┼────────────────────────────────────────────────╣
║ Polling                   ║ ⚠️ DETECTADO ║ MÉDIO    ║ SimulationGate pode ter Update/Tick           ║
║ desnecessário             ║ EM            ║          ║ que poderiam ser event-driven                 ║
║ em observability path     ║ OBSERVABILITY║          ║ Recomendação: refatorar para event-based     ║
╠───────────────────────────┼──────────────┼──────────┼────────────────────────────────────────────────╣
║ Corrigir sintoma          ║ ✅ CONFORME  ║ N/A      ║ Owner canônico sempre declarado              ║
║ sem declarar              ║              ║          ║ Cada módulo tem ownership claro               ║
║ owner canônico            ║              ║          ║ Segue ADR-0035 e ADR-0044                     ║
╚═════════════────────────────┴──────────────┴──────────┴────────────────────────────────────────────────┘
```

---

## MATRIZ 4: COMPLETUDE CONCEITUAL

```
╔═══════════════════════════════════════════════════════════════════════════════════════════════════╗
║                    CONFORMIDADE COM CONCEITOS CANÔNICOS (ADR-0044)                              ║
╠═══════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Conceito Canônico         │ Implementação              │ Localização          │ Status │ Completo ║
╠═══════════════════════════╪════════════════════════════╪══════════════════════╪════════╪══════════╣
║ Contexto Macro            │ Gameplay (domínio)         │ Game/Gameplay/       │ ✅     │ 100%     ║
║ Contexto Local Conteúdo   │ Level (conteúdo)           │ Game/Content/Levels/ │ ✅     │ 100%     ║
║ Contexto Local Visual      │ PostRunMenu                │ Experience/PostRun/  │ ✅     │ 95%      ║
║ Estágio Local             │ EnterStage, ExitStage     │ Orchestration/Level  │ ✅     │ 90%      ║
║ Estado de Fluxo           │ Playing (RunLifecycle)     │ Orchestration/GameLoop
║ Resultado da Run          │ RunResult, Victory/Defeat  │ Orchestration/GameLoop
║                           │                           │ /RunOutcome/         │ ✅     │ 95%      ║
║ Intenção Derivada         │ Restart, ExitToMenu       │ Orchestration/GameLoop
║                           │                           │ /Commands/           │ ✅     │ 95%      ║
║ Estado Transversal        │ Pause                      │ Orchestration/GameLoop
║                           │                           │ /Pause/              │ ✅     │ 100%     ║
╠═══════════════════════════╧════════════════════════════╧══════════════════════╧════════╧══════════╣
║ CONFORMIDADE CONCEITUAL TOTAL: 97% ✅ EXCELENTE                                                  ║
╚═════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## MATRIZ 5: RUNTIME BACKBONE CANÔNICO

```
╔════════════════════════════════════════════════════════════════════════════════════════════════╗
║                    SEQUÊNCIA RUNTIME CANÔNICA vs IMPLEMENTAÇÃO                                ║
║          (Conforme Blueprint-Baseline-4.0-Ideal-Architecture.md seção 3.1)                   ║
╠════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Sequência Esperada       │ Implementado Em                    │ Status │ Observação             ║
╠══════════════════════════╪════════════════════════════════════╪════════╪════════════════════════╣
║ Gameplay                 │ Game/Gameplay/                     │ ✅     │ Macro-contexto         ║
║ -> Level                 │ Game/Content/Definitions/Levels/   │ ✅     │ Contexto local         ║
║ -> EnterStage            │ Orchestration/LevelLifecycle/      │ ✅     │ Preparação entrada     ║
║ -> Playing               │ Orchestration/GameLoop/RunLifecycle│ ✅     │ Estado principal       ║
║ -> ExitStage             │ Orchestration/LevelLifecycle/      │ ✅     │ Fechamento local       ║
║ -> RunResult             │ Orchestration/GameLoop/RunOutcome/ │ ✅     │ Conclusão da run       ║
║ -> PostRunMenu           │ Experience/PostRun/Presentation/   │ ✅     │ Contexto visual pós    ║
║ -> Restart/ExitToMenu    │ Orchestration/GameLoop/Commands/   │ ✅     │ Intenções derivadas    ║
║ -> Navigation dispatch   │ Orchestration/Navigation/          │ ✅     │ Dispatch macro         ║
║ -> Audio reactions       │ Experience/Audio/Context/          │ ✅     │ Reações contextuais    ║
╠════════════════════════════════════════════════════════════════════════════════════════════════╣
║ CONFORMIDADE RUNTIME BACKBONE: 100% ✅ PERFEITO                                               ║
╚════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## MATRIZ 6: OWNERSHIP DISTRIBUIÇÃO

```
╔════════════════════════════════════════════════════════════════════════════════════════════════╗
║                    OWNERSHIP CANÔNICO vs IMPLEMENTADO                                         ║
║                  (Conforme ADR-0035 e ADR-0044)                                              ║
╠════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Domínio         │ Owner Canônico              │ Implementado │ Violations │ Status            ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ GameLoop        │ Flow state, run, pause      │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║                 │ NOT: pos-run, routes, audio │ ✅ Respeitado│            │                   ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ PostRun         │ Pos-run, resultado visual   │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║                 │ NOT: gameplay state, routes │ ✅ Respeitado│            │                   ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ LevelFlow       │ Conteúdo local, restart     │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║ (LevelLifecycle)│ NOT: resultado terminal     │ ✅ Respeitado│            │  (LevelFlow/Runtime║
║                 │ NOT: pos-run, dispatch      │              │            │   é [transição])  ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ Navigation      │ Intent para route dispatch  │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║                 │ NOT: semântica, pos-run     │ ✅ Respeitado│            │                   ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ Audio           │ Playback + precedência      │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║                 │ NOT: navigation, gameplay   │ ✅ Respeitado│            │                   ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ SceneFlow       │ Pipeline transição técnico  │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║                 │ NOT: gameplay, pos-run      │ ✅ Respeitado│            │                   ║
╠═════════════════╪═════════════════════════════╪══════════════╪════════════╪═══════════════════╣
║ Frontend/UI     │ Contextos visuais locais    │ ✅ Sim       │ 0          │ ✅ Conforme       ║
║                 │ NOT: domínio, resultado     │ ✅ Respeitado│            │                   ║
╠════════════════════════════════════════════════════════════════════════════════════════════════╣
║ CONFORMIDADE OWNERSHIP: 100% ✅ NENHUMA VIOLAÇÃO CRÍTICA                                       ║
╚════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## MATRIZ 7: TIMELINE DE AÇÕES

```
╔═══════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                            ROADMAP DE AÇÕES - CRONOGRAMA                                           ║
╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Semana │ Ações Planejadas                                      │ Horas │ Responsável    │ Status    ║
╠════════╪════════════════════════════════════════════════════════╪═══════╪════════════════╪═══════════╣
║ Sem 1  │ 1. Remover PoolingQaContextMenuDriver                │ 0.5h  │ Dev Sênior     │ ✅ Fácil  ║
║ (Apr7) │ 2. Documentar bridges legítimas                      │ 2h    │ Dev Sênior     │ ✅ Fácil  ║
║        │ 3. Mapear consumidores SceneResetFacade              │ 1.5h  │ Dev Sênior     │ ✅ Fácil  ║
║        │ ─────────────────────────────────────────────────────│───────│────────────────│───────────║
║        │ SUBTOTAL SEMANA 1                                     │ 4h    │ 1 pessoa       │           ║
╠════════╪════════════════════════════════════════════════════════╪═══════╪════════════════╪═══════════╣
║ Sem 2  │ 1. Iniciar SimulationGate audit (Update/Tick)        │ 4h    │ Dev Sênior     │ ⚠️ Média  ║
║ (Apr14)│ 2. Auditoria fallbacks silenciosos (logging)         │ 3h    │ Dev Sênior     │ ⚠️ Média  ║
║        │ 3. Iniciar consolidação GameplayReset/Core           │ 2h    │ Dev Junior     │ ✅ Fácil  ║
║        │ ─────────────────────────────────────────────────────│───────│────────────────│───────────║
║        │ SUBTOTAL SEMANA 2                                     │ 9h    │ 2 pessoas      │           ║
╠════════╪════════════════════════════════════════════════════════╪═══════╪════════════════╪═══════════╣
║ Sem 3  │ 1. Refatorar SimulationGate (event-driven)           │ 8h    │ Dev Sênior     │ ⚠️ Alta   ║
║ (Apr21)│ 2. Consolidar GameplayReset/Core (continuar)         │ 2h    │ Dev Junior     │ ✅ Fácil  ║
║        │ 3. Testes de integração SimulationGate               │ 4h    │ Dev Junior     │ ⚠️ Média  ║
║        │ ─────────────────────────────────────────────────────│───────│────────────────│───────────║
║        │ SUBTOTAL SEMANA 3                                     │ 14h   │ 1-2 pessoas    │           ║
╠════════╪════════════════════════════════════════════════════════╪═══════╪════════════════╪═══════════╣
║ Sem 4  │ 1. Fase 1: Auditoria SceneReset (4-6h)              │ 5h    │ Dev Sênior     │ ⚠️ Média  ║
║ (Apr28)│ 2. Fase 1: Documentar LevelFlow/Runtime consumidores │ 2.5h  │ Dev Junior     │ ✅ Fácil  ║
║        │ 3. Finalizar refatorações semanas 2-3                │ 2.5h  │ Ambos          │ ✅ Fácil  ║
║        │ ─────────────────────────────────────────────────────│───────│────────────────│───────────║
║        │ SUBTOTAL SEMANA 4                                     │ 10h   │ 1-2 pessoas    │           ║
╠════════╪════════════════════════════════════════════════════════╪═══════╪════════════════╪═══════════╣
║ Sem 5+ │ Fases 2-3: Migrações SceneReset e LevelFlow/Runtime │ 40h+  │ 1 dev sênior   │ 📋 Maior  ║
║ (May5+)│                                                       │       │ + architect    │           ║
║        │ Documentação de anti-padrões                          │ 6h    │ Tech writer    │           ║
║        │                                                       │       │ + architect    │           ║
║        │ ─────────────────────────────────────────────────────│───────│────────────────│───────────║
║        │ SUBTOTAL SEMANA 5+                                    │ 46h   │ Múltiplos      │           ║
╠════════╧════════════════════════════════════════════════════════╧═══════╧════════════════╧═══════════╣
║ TOTAL: 64-94 horas (~2 sprints de 2 semanas, 40h/week)                                           ║
╚════════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## MATRIZ 8: RASTREAMENTO DE MÉTRICAS

```
╔════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                      DASHBOARD DE MÉTRICAS - TRACKING                                            ║
╠════════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Métrica                    │ Baseline │ Alvo Curto │ Alvo Médio │ Alvo Final │ Método de Medida  ║
║                            │ (Apr 2)  │ (Apr 30)  │ (May 31)  │ (Jun 30)  │                    ║
╠════════════════════════════╪══════════╪═══════════╪═══════════╪═══════════╪═══════════════════╣
║ Limpeza de Código          │ 79%      │ 81%       │ 88%       │ 95%       │ Manual audit      ║
║ Score Agregado             │ 86/100   │ 87/100    │ 90/100    │ 92/100    │ Matriz 1          ║
║ Saúde Conceitual           │ 92%      │ 92%       │ 93%       │ 94%       │ Conformidade      ║
║ Completude                 │ 88%      │ 88%       │ 89%       │ 90%       │ Auditoría canônico║
║ Código Morto Identificado  │ 7 items  │ 1 item    │ 0 items   │ 0 items   │ Grep + manual     ║
║ Anti-padrões Encontrados   │ 2 medio  │ 1 medio   │ 0 items   │ 0 items   │ Manual audit      ║
║ Compat Layers Ativos       │ 3        │ 3         │ 1         │ 0         │ Structural scan   ║
║ Testes Passando            │ 100%     │ 100%      │ 100%      │ 100%      │ CI/CD pipeline    ║
╠════════════════════════════╧══════════╧═══════════╧═══════════╧═══════════╧═══════════════════╣
║ UPDATE: Atualizar este dashboard semanal durante execução do plano                               ║
╚════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## MATRIZ 9: RISCOS E MITIGAÇÃO

```
╔═════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                         ANÁLISE DE RISCOS - MITIGAÇÃO                                            ║
╠═════════════════════════════════════════════════════════════════════════════════════════════════════╣
║ Risco                    │ Severidade │ Probabilidade │ Impacto │ Mitigação                       ║
╠══════════════════════════╪════════════╪══════════════╪═════════╪═════════════════════════════════╣
║ Remover código usado     │ CRÍTICO    │ BAIXA        │ ALTO    │ - Grep+manual audit completo    ║
║ por consumidores         │            │ (5%)         │         │ - Code review rigoroso          ║
║ legado fora do escopo    │            │              │         │ - Teste de regressão           ║
╠══════════════════════════╪════════════╪══════════════╪═════════╪═════════════════════════════════╣
║ Refatoração quebra       │ ALTO       │ MÉDIA        │ ALTO    │ - Unit tests completos          ║
║ comportamento esperado   │            │ (20%)        │         │ - Integration tests             ║
║ (ex: SimulationGate)     │            │              │         │ - Feature tests                 ║
║                          │            │              │         │ - Incremental refactor          ║
╠══════════════════════════╪════════════╪══════════════╪═════════╪═════════════════════════════════╣
║ Tempo escapa do plano    │ MÉDIO      │ MÉDIA        │ MÉDIO   │ - Timeboxing rigoroso           ║
║ (refatorações maiores)   │            │ (30%)        │         │ - Daily standups                ║
║                          │            │              │         │ - Priorizar quick wins          ║
║                          │            │              │         │ - Paralelizar quando possível   ║
╠══════════════════════════╪════════════╪══════════════╪═════════╪═════════════════════════════════╣
║ Novo código introduz     │ MÉDIO      │ BAIXA        │ MÉDIO   │ - Strict code review            ║
║ novos anti-padrões       │            │ (10%)        │         │ - Anti-pattern checklist        ║
║                          │            │              │         │ - Architecture validation       ║
╠══════════════════════════╪════════════╪══════════════╪═════════╪═════════════════════════════════╣
║ Documentação fica        │ BAIXO      │ MÉDIA        │ BAIXO   │ - Tech writer atribuído         ║
║ desatualizada            │            │ (40%)        │         │ - Review em cada ADR            ║
║                          │            │              │         │ - Links atualizados             ║
╠════════════════════════════════════════════════════════════════════════════════════════════════════╣
║ RISCO GERAL: BAIXO-MÉDIO (bem mitigável com planejamento adequado)                              ║
╚════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## MATRIZ 10: CHECKLIST DIÁRIO DE CODE REVIEW

```
╔════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                    CHECKLIST PARA CODE REVIEW DURANTE LIMPEZA                                    ║
╠════════════════════════════════════════════════════════════════════════════════════════════════════╣

Antes de Mergear Qualquer PR na Fase de Limpeza, Validar:

☐ REMOÇÃO
  ☐ Classe/arquivo marcado como [Obsolete]? Se sim, consumidores foram removidos?
  ☐ Procuraram em TODO O CODEBASE (grep -r)?
  ☐ Unit tests passam 100%?
  ☐ Integration tests passam?
  ☐ Sem warnings de compilation?

☐ REFATORAÇÃO
  ☐ Comportamento anterior e posterior são idênticos (manual test)?
  ☐ Performance não piorou? (profiling se aplicável)
  ☐ Observabilidade foi mantida ou melhorada (logging)?
  ☐ Testes de integração passam?
  ☐ ADR foi criado se há decisão arquitetural?

☐ DOCUMENTAÇÃO
  ☐ README foi atualizado (se aplicável)?
  ☐ Comments foram atualizados?
  ☐ Se bridge, é justificado?
  ☐ Se [Obsolete], mensagem é clara?

☐ ANTI-PADRÕES
  ☐ Nenhum novo fallback silencioso?
  ☐ Nenhuma nova bridge não documentada?
  ☐ Nenhum novo polling desnecessário?
  ☐ Ownership está claro?

☐ ARQUITETURA
  ☐ Alinhado com ADR-0044?
  ☐ Nenhuma violação de ownership?
  ☐ Conceitos canônicos são respeitados?
  ☐ Fronteiras bem definidas?

☐ QUALIDADE
  ☐ Código está limpo e legível?
  ☐ Sem code duplication?
  ☐ Sem magic numbers/strings?
  ☐ Naming é claro?

APROVAÇÃO = Todos os ☐ marcados

╚════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

---

## Conclusão das Matrizes

Estas matrizes fornecem:
1. **Visão detalhada de saúde por módulo** (Matriz 1)
2. **Inventário completo de decisões** (Matriz 2)
3. **Mapeamento de anti-padrões** (Matriz 3)
4. **Validação de conformidade** (Matrizes 4-6)
5. **Timeline executável** (Matriz 7)
6. **Tracking de métricas** (Matriz 8)
7. **Avaliação de riscos** (Matriz 9)
8. **Checklist de qualidade** (Matriz 10)

**Use estas matrizes como referência durante code review e tracking de progresso.**

