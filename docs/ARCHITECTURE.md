# Arquitetura Base

## Princípios Fundamentais
- **World-Driven**: a simulação parte do estado do mundo; cenas representam mundos autocontidos que dirigem o ciclo de jogo.
- **Actor-Centric**: atores são a unidade principal de comportamento e interação. Cada ator possui identidade, papel e contratos claros para serviços e eventos.
- **Reset por Despawn/Respawn**: a limpeza de estado ocorre descartando instâncias de atores e recriando-as a partir de perfis/configurações, evitando mutação global persistente.
- **Multiplayer local**: todo fluxo deve suportar múltiplos jogadores no mesmo dispositivo, mantendo fontes únicas de identidade e evitando heurísticas por nome.
- **SOLID e baixo acoplamento**: contratos em inglês, comentários e guias em português; implementação posterior deve respeitar responsabilidade única e inversão de dependência.

## Escopos
- **Global**: serviços de infraestrutura (logging, configuração, pooling) vivem apenas quando necessários e não carregam estado de gameplay.
- **Scene**: cada cena monta seu próprio grafo de serviços e registries; nada presume persistência entre cenas além de contratos explícitos.
- **Actor**: componentes e serviços específicos do ator; resetado via despawn/respawn.

## Fluxo de Vida
1. **Bootstrap futuro** (não implementado neste commit) prepara infraestrutura mínima global.
2. **Cena inicia** e cria serviços de cena/registries; atores são instanciados ou respawnados conforme configuração.
3. **Gameplay** ocorre por eventos e contratos entre atores/serviços.
4. **Reset** é realizado despawnando atores e recriando-os; serviços de cena são descartados no unload.

## Contratos Esperados
- **Identidade**: atores devem expor identificadores estáveis adequados a multiplayer local.
- **Eventos**: preferir EventBus ou mensageria de escopo conhecido para desacoplar emissores e ouvintes.
- **DI/Serviços**: dependências explicitamente declaradas; nada é resolvido implicitamente por singletons globais.

## Relação com Ferramentas (Uteis)
O guia `docs/UTILS-SYSTEMS-GUIDE.md` é a fonte de verdade para sistemas de utilitários (event bus, DI, debug, pooling etc.) e descreve ciclo de vida e boas práticas obrigatórias.

## Futuras Extensões
Este commit não inclui código, cenas nem bootstraps. Implementações futuras devem seguir este documento como contrato arquitetural base.
