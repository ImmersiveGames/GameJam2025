# DECISIONS — O que este projeto NÃO faz

- Não inclui código de gameplay neste commit inicial.
- Não mantém singletons globais de gameplay; qualquer persistência deverá ser explícita por cena ou ator.
- Não cria cenas, prefabs ou GameObjects padrão; cada equipe deve montar cenas conforme necessidade seguindo a arquitetura base.
- Não inicia bootstraps automáticos; inicialização será configurada em commits futuros.
- Não resolve dependências de forma implícita; uso de DI/EventBus seguirá contratos documentados.
- Não assume rede online; foco exclusivo em multiplayer local.
- Não utiliza configurações ocultas ou heurísticas por nome para identificar atores ou jogadores.

## Proibições por padrão
- **Reflection**
  - Regra: chamadas de reflection (por exemplo, `System.Reflection`) são proibidas.
  - Exceções permitidas: somente quando o uso for aprovado pelo revisor técnico e documentado no PR com o tipo, membro e motivo específico.
  - Critério de aceite: o revisor deve encontrar uma justificativa escrita e rastreável no PR para cada ponto de reflection usado, incluindo limites de uso (onde, quando e para quê).
- **Coroutines**
  - Regra: `Coroutine`, `StartCoroutine` ou `IEnumerator` para fluxo de tempo são proibidos.
  - Exceções permitidas: apenas quando um revisor técnico aprovar explicitamente o uso, registrando no PR qual rotina depende de yield e o impacto no pipeline.
  - Critério de aceite: toda coroutine aprovada deve listar a razão de ser, a duração prevista e o comportamento de cancelamento; PRs sem essa descrição não passam na revisão.
