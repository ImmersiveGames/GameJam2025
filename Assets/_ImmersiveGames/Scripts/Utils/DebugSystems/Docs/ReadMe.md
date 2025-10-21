# 🛠️ Sistema de Debug — Guia de Uso (v1.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [Componentes Principais](#componentes-principais)
3. [Níveis de Log e Escopos](#níveis-de-log-e-escopos)
4. [Integração no Fluxo de Desenvolvimento](#integração-no-fluxo-de-desenvolvimento)
5. [Controle de Performance](#controle-de-performance)
6. [Troubleshooting](#troubleshooting)

---

## 🎯 Visão Geral

O **DebugSystem** centraliza logs, métricas e diagnósticos do projeto. Ele foi projetado para ambientes de **multiplayer local** onde cada frame importa, permitindo granularidade de logs sem comprometer o desempenho.

Principais vantagens:
* **Configuração Global** — Ajuste o nível de log padrão e estados de verbose em runtime.
* **Escopo Dinâmico** — Diferencie logs por tipo, instância ou atributo.
* **Utilidades Extras** — `FrameRateLimiter` facilita testes de estresse controlando FPS.

---

## 🧩 Componentes Principais

### `DebugManager`
Componente `MonoBehaviour` que aplica as preferências globais do sistema de debug.
Adicione-o ao mesmo GameObject do `GameManager` para centralizar as decisões de log.

* **Execução antecipada** — `DefaultExecutionOrder(-200)` garante que as flags sejam aplicadas antes dos demais gerenciadores.
* **Perfis por ambiente** — Permite níveis distintos para Editor e Player, além de respeitar `GameConfig.DebugMode` e forçar
  `Verbose` quando necessário.
* **Ferramentas em runtime** — Context menu `Debug/Apply Configuration` reaplica as configurações sem reiniciar a cena.

### `DebugUtility`
Classe estática com responsabilidade única de orquestrar logs. Principais áreas:
* **Inicialização** — `RuntimeInitializeOnLoadMethod` redefine estados em cada boot.
* **Configurações** — Métodos `SetGlobalDebugState`, `SetVerboseLogging`, `SetDefaultDebugLevel`, entre outros.
* **Logs Baseados em Tipo** — Métodos `Log`, `LogWarning`, `LogError`, `LogVerbose` (genéricos e não genéricos).
* **Convenções Visuais** — Use `DebugUtility.Colors.CrucialInfo` para inicializações e `DebugUtility.Colors.Success` para operações confirmadas.
* **Deduplicação** — `_callTracker` e `_messagePool` evitam spam em logs repetidos.
* **Verbose Seletivo** — `_disabledVerboseTypes` permite desativar detalhamento por tipo e `SetRepeatedCallVerbose(false)` desliga apenas alertas de chamadas repetidas.

### `DebugLevelAttribute`
Atributo aplicado em classes para definir o `DebugLevel` padrão sem modificar código de inicialização.

### `DebugLevel` (enum)
Níveis ordenados: `None`, `Error`, `Warning`, `Logs`, `Verbose`. Utilizado para comparação e filtros.

### `FrameRateLimiter`
Componente opcional para testes. Atalhos:
* `Shift + F1..F5` para alternar `Application.targetFrameRate` entre 10 e 900 FPS.
* Usa `DebugUtility.Log` para registrar o FPS atual, permitindo detectar gargalos em builds sem profiler.

---

## 📊 Níveis de Log e Escopos

`DebugUtility` avalia logs nesta ordem:
1. **Instância** — `SetLocalDebugLevel(instance, level)`.
2. **Registro de Script** — `RegisterScriptDebugLevel(type, level)`.
3. **Atributo** — `DebugLevelAttribute` aplicado ao tipo.
4. **Default** — `SetDefaultDebugLevel` (padrão `Logs`).

Cada mensagem avalia se o nível desejado é permitido no escopo atual. Logs Verbose podem ser filtrados por tipo ou desativados globalmente para builds.

---

## 🚀 Integração no Fluxo de Desenvolvimento

1. **Configurar via DebugManager**
   * Coloque `DebugManager` no mesmo GameObject do `GameManager` ou no bootstrap global.
   * Ajuste os níveis padrão para Editor/Player diretamente no inspetor.
   * Certifique-se de que o `GameManager` chame `debugManager.ApplyConfiguration(gameConfig)` no `Awake`.
2. **Anotar Sistemas**
   ```csharp
   [DebugLevel(DebugLevel.Logs)]
   public class DamageLifecycleModule { ... }
   ```
3. **Logar com Contexto**
   ```csharp
   DebugUtility.Log<ResourceSystem>(
       "Resource applied via DamageSystem",
       DebugUtility.Colors.Success,
       context: this);
   ```
   Utilize `LogVerbose` sem cor para detalhes ricos que podem ser silenciados.
   Prefira `Log` com `DebugUtility.Colors.CrucialInfo` para inicializações confirmadas e `DebugUtility.Colors.Success` quando uma operação crítica concluir corretamente.
4. **Controlar Verbose Dinâmico**
   ```csharp
   DebugUtility.DisableVerboseForType(typeof(ResourceLinkService));
   ```
5. **Isolar Chamadas Repetidas**
   ```csharp
   DebugUtility.SetRepeatedCallVerbose(false);
   ```

---

## ⏱️ Controle de Performance

* Os logs só são construídos quando aprovados pelo filtro (`ShouldLog`).
* `StringBuilder` e pools internos evitam GC spikes.
* Verbose é automaticamente desligado em builds (via `Application.isEditor`).
* `FrameRateLimiter` deve ser usado apenas em cenas de teste (evite em builds finais).

---

## 🧪 Troubleshooting

| Sintoma | Diagnóstico | Ação |
| --- | --- | --- |
| Nenhum log aparece | `SetGlobalDebugState(false)` foi chamado | Reativar ou verificar inicialização do `DependencyBootstrapper` |
| Verbose ignorado | Tipo está em `_disabledVerboseTypes` | Chame `EnableVerboseForType(type)` antes de logar |
| Logs excessivos em build | Flags de Player configuradas para Verbose | Ajuste o `DebugManager` para reduzir nível ou desativar Verbose no Player |
| Spam de logs repetidos | Faltou usar `deduplicate` ou notificação dedicada está ativa | Ative `deduplicate: true`, ajuste condições de disparo ou desligue com `SetRepeatedCallVerbose(false)` |
| FPS preso após testes | `FrameRateLimiter` ativo em cena de produção | Remover componente ou ajustar target manualmente |

Este módulo segue SRP ao separar logging, atributos e ferramentas auxiliares, permitindo substituição futura por outro provider sem quebrar consumidores.
