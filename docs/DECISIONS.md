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
