# DECISIONS — O que este projeto NÃO faz

- Não inclui código de gameplay neste commit inicial.
- Não mantém singletons globais de gameplay; qualquer persistência deverá ser explícita por cena ou ator.
- Não cria cenas, prefabs ou GameObjects padrão; cada equipe deve montar cenas conforme necessidade seguindo a arquitetura base.
- Não inicia bootstraps automáticos; inicialização será configurada em commits futuros.
- Não resolve dependências de forma implícita; uso de DI/EventBus seguirá contratos documentados.
- Não assume rede online; foco exclusivo em multiplayer local.
- Não utiliza configurações ocultas ou heurísticas por nome para identificar atores ou jogadores.
