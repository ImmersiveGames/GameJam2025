# ADR-0029 Pooling Final Audit

## 1. Resumo executivo

O estado atual do modulo canonico de pooling em `Infrastructure/Pooling/**` cobre os requisitos obrigatorios do ADR-0029 para infraestrutura compartilhada: ownership no `GlobalCompositionRoot`, identidade por `PoolDefinitionAsset`, runtime de prewarm/rent/return/expand/limit/cleanup, suporte operacional de `autoReturnSeconds`, e harness standalone de QA por `ContextMenu`.

A auditoria estatica nao encontrou acoplamento estrutural com dominios (`Audio`, `Gameplay`, `UI`, `VFX`, `Actor`, `spawner`), nem uso de `PersistentSingleton`, `RuntimeInitializeOnLoadMethod` ou `Resources.Load` no modulo.

Foram identificadas observacoes nao-bloqueantes (principalmente de observabilidade/documentacao de logs e dependencia de reflection no QA snapshot), sem lacuna obrigatoria para fechamento do ADR-0029.

## 2. Matriz de cobertura do ADR

| Requisito / feature ADR | Arquivo(s) | Status | Observacao |
|---|---|---|---|
| Pooling em `Infrastructure/Pooling/**` | `Infrastructure/Pooling/**` | DONE | Estrutura canonica presente (Contracts/Config/Runtime/QA). |
| Ownership no `GlobalCompositionRoot` | `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` | DONE | Estagio `Pooling` no pipeline e registro global de `IPoolService`. |
| Identidade por `PoolDefinitionAsset` | `Infrastructure/Pooling/Contracts/IPoolService.cs`, `Infrastructure/Pooling/Runtime/PoolService.cs` | DONE | API e cache usam referencia de asset como chave. |
| Ausencia de string como identidade estrutural | `Infrastructure/Pooling/Runtime/PoolService.cs`, `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DIFFERENT-BUT-EQUIVALENT | Strings usadas apenas para log/host name (`poolLabel`), nao para lookup estrutural. |
| Ausencia de `Resources.Load` | `Infrastructure/Pooling/**` | DONE | Nenhuma ocorrencia encontrada. |
| Ausencia de `RuntimeInitializeOnLoadMethod` como owner | `Infrastructure/Pooling/**`, `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` | DONE | Bootstrap via composition root. |
| Ausencia de `PersistentSingleton` | `Infrastructure/Pooling/**` | DONE | Nenhuma ocorrencia encontrada. |
| Nucleo sem acoplamento de dominio | `Infrastructure/Pooling/**` | DONE | Sem referencias estruturais a modulos de dominio. |
| `IPoolService` | `Infrastructure/Pooling/Contracts/IPoolService.cs` | DONE | Contrato canonico presente. |
| `IPoolableObject` | `Infrastructure/Pooling/Contracts/IPoolableObject.cs` | DONE | Hooks de lifecycle presentes. |
| `PooledBehaviour` | `Infrastructure/Pooling/Contracts/PooledBehaviour.cs` | DONE | Base opcional no-op com estado previsivel. |
| `PoolDefinitionAsset` | `Infrastructure/Pooling/Config/PoolDefinitionAsset.cs` | DONE | Campos obrigatorios e `OnValidate` defensivo presentes. |
| `PoolService` | `Infrastructure/Pooling/Runtime/PoolService.cs` | DONE | Ensure/cache/prewarm/rent/return/shutdown implementados. |
| `GameObjectPool` | `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DONE | Core de pool, counts, expand, max-limit, cleanup. |
| `PoolRuntimeHost` | `Infrastructure/Pooling/Runtime/PoolRuntimeHost.cs` | DONE | Host e hierarquia `Available` para ownership runtime. |
| `PoolRuntimeInstance` | `Infrastructure/Pooling/Runtime/PoolRuntimeInstance.cs` | DONE | Vinculo instancia-origem + estado rented/rentCount. |
| `PoolAutoReturnTracker` | `Infrastructure/Pooling/Runtime/PoolAutoReturnTracker.cs` | DONE | Agendamento/cancelamento/clear/cleanup operacional via coroutine host. |
| Prewarm | `Infrastructure/Pooling/Runtime/PoolService.cs`, `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DONE | Prewarm por definicao e por target size. |
| Rent | `Infrastructure/Pooling/Runtime/PoolService.cs`, `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DONE | Rent com hooks, parent opcional e logs. |
| Return | `Infrastructure/Pooling/Runtime/PoolService.cs`, `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DONE | Return manual canonico com validacao de ownership/rented. |
| Expansao controlada | `Infrastructure/Pooling/Runtime/GameObjectPool.cs`, `Infrastructure/Pooling/Config/PoolDefinitionAsset.cs` | DONE | `canExpand` + `maxSize` aplicados no runtime. |
| Falha ao atingir teto | `Infrastructure/Pooling/Runtime/GameObjectPool.cs`, `Infrastructure/Pooling/Runtime/PoolService.cs` | DONE | `InvalidOperationException` + logs explicitos de limite. |
| Cleanup | `Infrastructure/Pooling/Runtime/GameObjectPool.cs`, `Infrastructure/Pooling/Runtime/PoolService.cs`, `Infrastructure/Pooling/Runtime/PoolRuntimeHost.cs` | DONE | Cleanup de instancias, timers, host e root global. |
| Auto-return | `Infrastructure/Pooling/Runtime/PoolAutoReturnTracker.cs`, `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DONE | Retorno automatico quando `autoReturnSeconds > 0`. |
| Cancelamento por return manual | `Infrastructure/Pooling/Runtime/GameObjectPool.cs` | DONE | `Cancel(..., \"manual-return\")` antes de retorno manual. |
| Cancelamento por cleanup | `Infrastructure/Pooling/Runtime/GameObjectPool.cs`, `Infrastructure/Pooling/Runtime/PoolAutoReturnTracker.cs` | DONE | `Clear(\"pool-cleanup\")` + `Cleanup()` no tracker. |
| Validacao standalone / QA harness | `Infrastructure/Pooling/QA/PoolingQaContextMenuDriver.cs`, `Infrastructure/Pooling/QA/PoolingQaMockPooledObject.cs` | DONE | Harness com `ContextMenu` cobrindo cenarios centrais do modulo. |

## 3. Aderencia arquitetural

- Standalone/shared infrastructure: conforme. O modulo esta isolado em `Infrastructure/Pooling/**` e registrado globalmente no composition root.
- Sem dependencia estrutural de dominio: conforme. Nao ha dependencia estrutural em `Audio`, `Gameplay`, `UI`, `VFX`, `Actor`, `spawner`.
- Sem regra hardcoded de posicionamento: conforme. Nao ha regra tipo `y = 0`.
- Sem fallback estrutural indevido: conforme. Identidade e lookup por `PoolDefinitionAsset`, sem fallback por string.
- Sem bootstrap paralelo fora do `GlobalCompositionRoot`: conforme para ownership estrutural.

Risco residual arquitetural baixo:

- `PoolService.Shutdown()` destroi o root global; se o mesmo service for reutilizado apos shutdown no mesmo ciclo sem reinstalacao, a semantica de reuso nao esta explicitada no contrato.

## 4. Diferencas entre contrato e implementacao final

1) Identidade textual (`poolLabel`) ainda aparece em host/log.
- Classificacao: aceitavel.
- Motivo: uso observacional, nao estrutural.

2) Observabilidade de snapshot no QA via reflection (`_pools` privado).
- Classificacao: aceitavel com observacao.
- Motivo: funciona para QA, mas e acoplamento fragil a shape interno de `PoolService`.

3) Logs de boot ainda mencionam "Package B runtime core".
- Classificacao: aceitavel com observacao.
- Motivo: nao impacta funcionalidade; apenas nomenclatura historica de rollout.

4) Cleanup no `IPoolService` e global (`Shutdown`) e nao por definicao.
- Classificacao: aceitavel com observacao.
- Motivo: cobre requisito de cleanup, mas sem granularidade por pool no contrato publico.

## 5. QA harness / evidencias

Cobertura forte:

- Ensure/prewarm/rent/return/burst/rent-until-max/rent-past-max(expect-fail)/cleanup.
- Cenarios de auto-return: rent aware e scenario com espera + snapshot antes/depois.
- Mock lifecycle observavel (`MockCreated`, `MockRent`, `MockReturn`, `MockDestroyed`).

Cobertura parcial:

- Snapshot depende de reflection e pode quebrar se o shape interno de `PoolService` mudar.
- Evidencia e manual (Play Mode), sem suite automatizada de regressao.

Lacuna de observabilidade:

- O driver soma `totalReturnOperations` tambem para reconciliacao de instancias inativas; e util para estado local, mas nao diferencia explicitamente return manual vs auto-return por contador separado.

## 6. Lacunas encontradas

Lacunas obrigatorias: nenhuma.

Lacunas nao-bloqueantes:

- ausencia de contador separado manual vs auto-return no QA driver.
- dependencia de reflection para snapshot no QA.
- nomenclatura de logs ainda com referencia a "Package B" em pontos de boot.

## 7. Itens aceitaveis mesmo com shape diferente

- Host naming por `poolLabel` (observabilidade) em vez de identidade estrutural.
- QA harness via `ContextMenu` como estrategia standalone (em vez de automacao de testes formais).
- Auto-return implementado por tracker com coroutine host interno (shape diferente de outras abordagens, resultado equivalente).

## 8. Riscos residuais

- Risco baixo de fragilidade no QA snapshot por reflection de membro privado.
- Risco baixo de interpretacao ambigua em logs de rollout por mensagens historicas de pacote.
- Risco baixo de comportamento pos-shutdown sem contrato explicito de rebootstrap no mesmo service.

## 9. Veredito final

**PASS WITH OBSERVATIONS**

Correcoes obrigatorias:

- nenhuma.

Melhorias opcionais:

- separar contador de return manual vs auto-return no QA driver para evidencia mais limpa.
- substituir reflection do snapshot por API publica minima de observabilidade (somente leitura).
- atualizar textos de log historicos que ainda citam "Package B" para refletir fechamento final.

Decisao de continuidade:

- o projeto pode voltar para o Audio; nao ha bloqueio obrigatorio remanescente no Pooling para o escopo do ADR-0029.
