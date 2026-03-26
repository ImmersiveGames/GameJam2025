# ADR-0038: Module Service Registration and Runtime Composition

## Status
- Aceito

## Evidências canônicas
- auditoria nova de module registration vs runtime composition em `NewScripts`
- Baseline 3.5
- ADR-0035: ownership canônico dos clusters de módulos
- ADR-0037: official baseline hooks and extension points
- ADR-0030 / ADR-0031: fronteiras canônicas do stack SceneFlow / pipeline macro
- `Modules/GameLoop/Bootstrap/GameLoopBootstrap.cs`

## Contexto
O ADR-0038 anterior partia de uma premissa incompleta: tratava registro de serviços e composição runtime como se fossem a mesma etapa. Isso empurrou responsabilidades operacionais para o boot e para o root global.

A auditoria mais recente corrigiu a base: `module installers` registram contratos no boot; `runtime composition` acontece depois, com o DI já preenchido; e o `GlobalCompositionRoot` apenas orquestra fases e entry points.

Essa separação é necessária para evitar regressões, reduzir acoplamento cruzado e manter módulos autocontidos dentro do próprio cluster de ownership.

## Decisão
- `Module Installer` serve apenas para registrar serviços, interfaces, providers, factories, configs e contratos explícitos de bootstrap/composer.
- `Module Installer` não compõe pipeline, não integra runtime e não executa comportamento de domínio.
- `Module Runtime Composer` / `Module Bootstrap` executa a composição operacional depois que os installers relevantes terminaram.
- `Module Runtime Composer` usa apenas dependências já registradas e permanece dentro do próprio módulo.
- `GlobalCompositionRoot` apenas ordena fases, chama entry points e passa contexto compartilhado.
- Não há auto-registro mágico, reflection opaca ou bootstrap invisível.
- Arquivos de installer e composer/bootstrap permanecem dentro do módulo dono.

## Canonical Two-Phase Model
- Fase 1: `service registration`
  - registro determinístico de contratos e implementações
  - validação de configuração obrigatória
  - preparação do DI para uso posterior
- Fase 2: `runtime composition`
  - wiring operacional entre serviços já registrados
  - ativação de controladores, adapters, coordinators e bootstrappers
  - ligação de contratos entre módulos por interfaces explícitas

A fase 2 nunca deve ser embutida no installer. Ela sempre assume que o DI relevante já foi preenchido.

## What Belongs in Module Installers
- serviços e suas interfaces
- providers e resolvers
- factories de composição
- configs obrigatórias do módulo
- contratos explícitos de bootstrap/composer, quando o módulo precisar expor um entry point local

## What Belongs in Module Runtime Composition
- composição entre serviços já registrados
- wiring operacional interno do módulo
- ativação de controladores e bootstrappers
- tradução de contratos do domínio para requests ou planos técnicos
- integração entre módulos via dependências explícitas já resolvidas

## What the Global Root Should Still Do
- ordenar fases
- chamar installers
- chamar bootstrappers/composers explícitos
- passar contexto compartilhado

O root global não deve conhecer detalhes internos de montagem, nem concentrar lógica operacional de domínio dos módulos.

## Positive Reference
`Modules/GameLoop/Bootstrap/GameLoopBootstrap.cs` é a referência positiva inicial para o padrão: entry point explícito, responsabilidade local e separação clara entre registro e uso operacional.

## Recommended Pilot
- Primeiro piloto recomendado sob esta premissa correta: `GameLoop`
- Candidato posterior: `SceneFlow/Transition`
- Candidato posterior e de maior risco/acoplamento: `LevelFlow`

## Non-Goals
- não migrar tudo agora
- não remover imediatamente o `GlobalCompositionRoot`
- não criar sistema de plugin automático
- não reorganizar pastas neste ADR
- não reabrir ownership de domínio

## Consequências
- installers ficam menores, previsíveis e verificáveis
- composição runtime deixa de competir com o boot por responsabilidade
- o root global reduz conhecimento de detalhes internos
- pilotos futuros passam a seguir uma fronteira homogênea e menos sujeita a regressões
