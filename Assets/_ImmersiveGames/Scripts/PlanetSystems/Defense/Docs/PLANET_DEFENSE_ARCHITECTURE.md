# Arquitetura do Sistema de Defesa Planetária

## Histórico de testes temporários
- **Etapa 3 (aplicação de profile no spawn)**: teste de validação visual concluído com sucesso. O script temporário `MinionSpawnTest` foi removido após confirmar que o profile é aplicado no instante do spawn, eliminando piscadas visuais.
- **Etapa 4 (remoção de campos duplicados em MonoBehaviours)**: campos de configuração visíveis em prefabs foram removidos do `DefenseMinionController`, mantendo o comportamento 100% guiado por profiles para evitar discrepâncias entre dados do prefab e do profile.
