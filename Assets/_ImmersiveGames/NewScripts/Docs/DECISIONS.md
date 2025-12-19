# DECISIONS — Limites atuais

- Não inclui gameplay neste estágio; foco em infraestrutura mínima e ciclo de vida do mundo.
- Não mantém singletons globais de gameplay; persistência é explícita por cena ou ator.
- Não presume cenas/prefabs padrão; cada equipe monta a cena seguindo a arquitetura base e o bootstrapper de cena.
- Inicialização ocorre via `NewSceneBootstrapper`; nada cria registries ou serviços de ciclo de vida fora dele.
- Dependências são resolvidas explicitamente (DI/EventBus); nada é obtido por reflection ou heurística.
- Não assume rede online; foco exclusivo em multiplayer local.
- Não utiliza heurísticas por nome para identificar atores/jogadores.
- WorldLifecycleHookRegistry ownership: Bootstrapper-only.
- Controller/Orchestrator são consumidores; guardrails e logs para flagrar duplo-registro ou resolução fora de ordem.
- QA/Testers não garantem execução em `Awake`; devem tolerar boot order com lazy injection + retry/timeout e falhar com mensagem acionável se o bootstrapper não rodou.
- Fluxo operacional detalhado do WorldLifecycle vive em `WorldLifecycle/WorldLifecycle.md`; este arquivo mantém apenas normas/guardrails.

## Política de Uso do Legado
- NewScripts define a arquitetura e os contratos ideais como fonte de verdade; o legado não é baseline.
- Não assumir integração com legado (código, interfaces, serviços, adaptadores/bridges) por padrão.
- Qualquer proposta de reaproveitamento do legado deve vir como sugestão explícita, com: (a) motivo, (b) benefício, (c) riscos, (d) alternativas.
- Implementação só acontece após autorização explícita do usuário para cada caso.
- Enquanto não autorizado, evitar dependências e referências diretas ao namespace/infra do legado.
- O legado pode ser usado apenas como linhas gerais e comportamento histórico, nunca como padrão arquitetural.

### Checklist para PR/Commit
- Existe alguma referência ao legado?
- Foi explicitamente autorizado?
- Foi documentado o motivo e a alternativa considerada?
- A migração mantém NewScripts como source of truth?
