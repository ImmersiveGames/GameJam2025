# ⚔️ Sistema de Dano — Documentação Oficial (v2.0)

## 📚 Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura Event-Driven](#arquitetura-event-driven)
3. [Pipeline Command](#pipeline-command)
4. [Componentes Principais](#componentes-principais)
5. [Estratégias e Modificadores](#estratégias-e-modificadores)
6. [Eventos Disponíveis](#eventos-disponíveis)
7. [Undo e Reversões](#undo-e-reversões)
8. [Integração com o Sistema de Recursos](#integração-com-o-sistema-de-recursos)
9. [Configuração via Inspector](#configuração-via-inspector)
10. [Extensibilidade](#extensibilidade)
11. [Debug e Troubleshooting](#debug-e-troubleshooting)
12. [Exemplos Práticos](#exemplos-práticos)

---

## 🎯 Visão Geral

O **Damage System** fornece um pipeline modular de processamento de dano totalmente orientado a eventos e ao padrão **Command**, alinhado com os princípios SOLID e a arquitetura reativa do projeto. Cada `DamageReceiver` monta suas estratégias, valida cooldowns, calcula valores efetivos, aplica o dano no `ResourceSystem` e publica eventos rastreáveis — com suporte nativo a **undo** para multiplayer local.

### ✨ Destaques

- **Orquestração por Comandos** — `DamageCommandInvoker` controla o fluxo, histórico e reversões.
- **Event-Driven** — Todos os estágios disparam eventos via `DamageEventDispatcher` e `FilteredEventBus`.
- **Estratégias Componíveis** — `DamageStrategyFactory` monta pipelines de `IDamageStrategy` em tempo de execução.
- **Undo Seguro** — Cada comando guarda snapshots para restaurar o estado anterior.
- **Integração Direta com Recursos** — Atualiza `ResourceSystem` e notifica módulos de ciclo de vida.

---

## 🧩 Arquitetura Event-Driven

```
DamageDealer (colisão / trigger)
    └─ cria DamageContext → DamageReceiver
            └─ DamageCommandInvoker
                ├─ ResolveResourceSystemCommand
                ├─ DamageCooldownCommand
                ├─ CalculateDamageCommand
                ├─ ApplyDamageCommand
                ├─ RaiseDamageEventsCommand
                └─ CheckDeathCommand

Eventos disparados:
    DamagePipelineStarted / Completed / Failed / Undone
    DamageEvent / DamageEventReverted
    DeathEvent / ReviveEvent / ResetEvent
```

Cada comando trabalha em cima de um `DamageCommandContext` compartilhado, permitindo que serviços externos escutem os eventos emitidos e reajam sem acoplamento direto ao `DamageReceiver`.

---

## 🧭 Pipeline Command

| Ordem | Comando | Responsabilidade | Undo |
| --- | --- | --- | --- |
| 1 | `ResolveResourceSystemCommand` | Obtém `ResourceSystem` via `InjectableEntityResourceBridge`. | Limpa a referência para liberar GC. |
| 2 | `DamageCooldownCommand` | Verifica cooldown por par atacante/alvo (`DamageCooldownModule`). | Restaura timestamp anterior ou remove registro. |
| 3 | `CalculateDamageCommand` | Calcula o dano final aplicando pipeline de `IDamageStrategy`. | Restaura valor calculado anterior. |
| 4 | `ApplyDamageCommand` | Salva snapshot e modifica o recurso (`ResourceSystem.Modify`). | Restaura snapshot completo e timestamp de dano. |
| 5 | `RaiseDamageEventsCommand` | Dispara `DamageEvent` via `DamageEventDispatcher`. | Publica `DamageEventReverted` e limpa cache. |
| 6 | `CheckDeathCommand` | Atualiza estado de vida via `DamageLifecycleModule`. | Restaura estado anterior emitindo eventos coerentes. |

Falhas em qualquer etapa abortam o fluxo, executam `Undo` parcial e emitem `DamagePipelineFailed`.

---

## 🏗️ Componentes Principais

### `DamageReceiver`
- Requer `ActorMaster` e `InjectableEntityResourceBridge` no mesmo objeto.
- Configura `strategyPipeline`, cooldown e recurso alvo via Inspector.
- Constrói `DamageCommandInvoker` e pipeline de estratégias em `Awake`/`OnValidate`.
- API pública:
  - `ReceiveDamage(DamageContext)` — inicia o pipeline.
  - `UndoLastDamage()` — reverte o último registro executado.
  - `GetReceiverId()` — expõe o identificador do ator para colisões e eventos.

### `DamageDealer`
- Também depende de `ActorMaster`.
- Detecta colisões (`OnCollisionEnter`) ou delegações via `DamageChildCollider`.
- Cria `DamageContext` com hit position/normal e invoca `IDamageReceiver`.

### `DamageContext`
- DTO imutável contendo atacante, alvo, valor bruto, tipo de dano, recurso alvo e vetores de contato.
- Marca o timestamp (`Time.time`) no momento da criação para auditoria.

### `DamageCommandInvoker`
- Recebe a sequência de `IDamageCommand` no construtor.
- Mantém histórico (`Stack`) de execuções bem-sucedidas para suportar `Undo`.
- Emite eventos de pipeline (`Started`, `Completed`, `Failed`, `Undone`).

### Módulos auxiliares
- `DamageCooldownModule` — mapa local de cooldown por par atacante/alvo.
- `DamageLifecycleModule` — avalia morte/revive/reset e dispara eventos dedicados.

---

## 🧪 Estratégias e Modificadores

### Interface `IDamageStrategy`
Define `float CalculateDamage(DamageContext ctx)` permitindo implementar novas regras.

### Implementações padrão
- `BasicDamageStrategy` — retorna o valor solicitado sem alterações.
- `CriticalDamageStrategy` — aplica chance de crítico com multiplicador configurável.
- `ResistanceDamageStrategy` — consulta `DamageModifiers` para multiplicadores por `DamageType`.
- `CompositeDamageStrategy` — encadeia múltiplas estratégias executando-as em série.

### `DamageStrategyFactory`
- Converte `DamageStrategySelection` em instâncias de `IDamageStrategy`.
- Garante valores seguros (clamp de chances, multiplicadores ≥ 0).
- `CreatePipeline` monta automaticamente um `CompositeDamageStrategy` quando mais de uma seleção é fornecida.

### `DamageModifiers`
- Lista serializável de entradas (`DamageType` ↔ multiplicador).
- Cache interno para leituras rápidas e API de CRUD para ajustes em runtime.

---

## 📢 Eventos Disponíveis

| Evento | Quando ocorre | Observações |
| --- | --- | --- |
| `DamagePipelineStarted` | Antes do primeiro comando | Útil para UI/FX iniciais. |
| `DamagePipelineCompleted` | Após todo o pipeline concluir | Inclui valor final aplicado. |
| `DamagePipelineFailed` | Se algum comando retornar `false` | Informa o comando que falhou. |
| `DamagePipelineUndone` | Após `UndoLastDamage` | Exibe o dano restaurado. |
| `DamageEvent` | Dano aplicado com sucesso | Contém dados de hit e tipo. |
| `DamageEventReverted` | Undo efetuado | Permite desfazer efeitos visuais. |
| `DeathEvent` / `ReviveEvent` / `ResetEvent` | Alterações de ciclo de vida | Filtrados pelo `ActorId`. |

Todos os eventos usam `DamageEventDispatcher.RaiseForParticipants`, garantindo publicação tanto para atacante quanto para alvo (quando IDs válidos).

---

## ♻️ Undo e Reversões

- `DamageCommandInvoker.UndoLast()` percorre o histórico e chama `Undo` em ordem reversa.
- `DamageReceiver.UndoLastDamage()` expõe a operação para sistemas externos (ex.: rollback de jogadas, rewinds, cheats de debugging).
- Snapshots incluem estado completo do recurso (`ResourceSystem.Set` por tipo) e timestamp de último dano para manter lógica de regeneração/cooldown consistente.

---

## 🔗 Integração com o Sistema de Recursos

- `ResolveResourceSystemCommand` depende de `InjectableEntityResourceBridge` para obter o `ResourceSystem` já registrado no `DependencyManager`.
- `ApplyDamageCommand` modifica diretamente o recurso configurado (`ResourceType targetResource`).
- `DamageLifecycleModule` reutiliza o mesmo `ResourceSystem` para detectar morte/ressurreição.
- Eventos resultantes podem ser consumidos por bridges do ResourceSystem (UI, thresholds, auto-flow) mantendo coerência reativa.

---

## 🛠️ Configuração via Inspector

1. **DamageReceiver**
   - `Target Resource`: escolha o recurso alvo (ex.: Health).
   - `Damage Cooldown`: tempo mínimo entre acertos do mesmo atacante.
   - `Strategy Pipeline`: lista ordenada de estratégias (Basic, Critical, Resistance).
2. **DamageDealer**
   - `Base Damage`, `Damage Type`, `Target Resource`.
   - `Target Layers`: máscara de colisão válida.
3. **ActorMaster + InjectableEntityResourceBridge**
   - Garantem que o receptor consiga localizar seu `ResourceSystem`.

> 💡 *Dica:* Utilize múltiplos `DamageStrategySelection` para compor críticas + resistências sem criar scripts customizados.

---

## 🚀 Extensibilidade

### Adicionando uma nova estratégia
1. Criar classe que implementa `IDamageStrategy`.
2. Atualizar `DamageStrategyType` e `DamageStrategyFactory.Create` para suportar a nova opção.
3. Adicionar campos serializáveis em `DamageStrategySelection` conforme necessário.

### Inserindo novos comandos
1. Implementar `IDamageCommand` com lógica e undo.
2. Registrar o comando no array montado em `DamageReceiver.BuildCommandPipeline()` na posição desejada.
3. Emitir eventos próprios via `DamageEventDispatcher` se necessário.

### Assinando eventos
- Use `FilteredEventBus<T>` para registrar handlers filtrados pelo `ActorId`.
- Combine com o Event Bus do ResourceSystem para sincronizar UI, FX e telemetria.

---

## 🧪 Debug e Troubleshooting

- **Falha silenciosa?** Verifique se `DamageContext` contém attacker/target válidos e se o `InjectableEntityResourceBridge` está no mesmo GameObject.
- **Cooldown nunca reseta?** Certifique-se de chamar `DamageCooldownModule.ClearForActor` quando um ator sai da cena.
- **Undo não reverte efeitos visuais?** Garanta que consumidores de `DamageEvent` também escutem `DamageEventReverted`.
- **Morte não dispara?** Confirme se o recurso alvo está configurado e registrado no `ResourceSystem` do ator.

---

## 📘 Exemplos Práticos

```csharp
// Disparando dano manualmente (ex.: habilidade)
public void CastSpell(IDamageReceiver target, float amount)
{
    var ctx = new DamageContext(
        attackerId: caster.ActorId,
        targetId: target.GetReceiverId(),
        damageValue: amount,
        targetResource: ResourceType.Health,
        damageType: DamageType.Fire
    );

    target.ReceiveDamage(ctx);
}

// Revertendo o último hit (ex.: rewind local)
public void RewindLastHit(IDamageReceiver target)
{
    target.UndoLastDamage();
}
```

---

### Contato

Qualquer ajuste futuro deve manter a compatibilidade com o padrão Command, o disparo de eventos centralizado e o suporte a undo — pilares que sustentam o fluxo multiplayer local e a arquitetura reativa do projeto.
