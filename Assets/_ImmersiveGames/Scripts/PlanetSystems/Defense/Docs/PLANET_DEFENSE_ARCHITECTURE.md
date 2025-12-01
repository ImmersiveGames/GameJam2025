# Arquitetura do Sistema de Defesa Planetária

## Histórico de testes temporários
- **Etapa 3 (aplicação de profile no spawn)**: teste de validação visual concluído com sucesso. O script temporário `MinionSpawnTest` foi removido após confirmar que o profile é aplicado no instante do spawn, eliminando piscadas visuais.
- **Etapa 4 (remoção de campos duplicados em MonoBehaviours)**: campos de configuração visíveis em prefabs foram removidos do `DefenseMinionController`, mantendo o comportamento 100% guiado por profiles para evitar discrepâncias entre dados do prefab e do profile.
- **Etapa 5 (SRP aplicado aos minions)**: `DefenseMinionController` agora coordena três handlers especializados (entrada, espera em órbita e perseguição), permitindo desabilitar etapas individualmente sem alterar a lógica central.
- **Etapa 6 (WaveProfile como pacote completo)**: `DefenseWaveProfileSO` passou a definir também o `defaultMinionProfile`, permitindo criar ondas com comportamentos distintos (ex.: rápida + zigzag vs. lenta + arco) apenas trocando o asset de wave sem tocar em código.
- **Etapa 7 (Loadouts por planeta)**: cada planeta agora recebe um `PlanetDefenseLoadoutSO` completo (pool, wave profile e estratégia), garantindo que loadouts distintos gerem defesas totalmente diferentes sem depender de configurações globais.
- **Etapa 8 (Estratégias ativas)**: `IDefenseStrategy` passou a decidir o profile aplicado por alvo, permitindo respostas distintas (ex.: minions rápidos e zigzag para Eater; lentos e em arco para Player) apenas trocando o loadout do planeta.

## Atualização 01/12/2025 – Loadouts por Planeta
- Atribuição runtime no `PlanetsManager` via array de loadouts para variações em instâncias de um único prefab. Exemplo: para 3 planetas, use `loadouts[0]` no primeiro e sorteie para os demais, garantindo defesas distintas sem alterar prefabs.

## Atualização 01/12/2025 – Estratégias de Defesa Vivas
- `IDefenseStrategy` agora é usada para resolver profiles de minion por alvo. Exemplo de configuração: atribua `AggressiveEaterStrategy` no loadout de um planeta com profile zigzag rápido e `DefensivePlayerStrategy` com profile lento de arco para Players. Ao detectar cada role, o runner aplica o profile correspondente antes do spawn, sem tocar em código.

## Atualização 02/12/2025 – Limpeza Final
- Remoção de aliases obsoletos e referências mortas para eliminar warnings do console e manter o sistema de defesa sem código legado. O controle de alvo agora ocorre apenas via `SetTarget`, alinhando o fluxo de roles centralizado no runner.
