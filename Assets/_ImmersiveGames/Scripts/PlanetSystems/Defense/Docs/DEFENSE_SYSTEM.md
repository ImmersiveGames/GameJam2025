# Defesa Planetária v2.2 — 02/12/2025 (Implementado)

**Consolidação**
- Interfaces centralizadas: IPlanetDefenseSetupOrchestrator, etc.
- Serviços: PlanetDefenseOrchestrationService (contexto/pool/waves), PlanetDefenseEventService.
- Eventos: apenas runtime data (sem SO propagado).

**Divisão SR**
- SpawnService dividido em Orchestration + EventService.
- Controller registra via DependencyManager.

**Correções**
- Struct config obsoleta removida; só WaveProfileSO (Inspector).
- Warm-up/start/stop via engajamento eventos.

**Registro**
- Manual/explícito; contratos segmentados.

**Compatibilidade**
- DefenseRoleConfig mantido só legado antigo.
