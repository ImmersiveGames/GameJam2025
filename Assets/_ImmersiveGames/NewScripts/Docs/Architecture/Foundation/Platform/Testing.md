# Foundation / Platform / Testing

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Assets auxiliares de teste/QA vinculados à infraestrutura Foundation.

## Para que serve

- Testar pooling.
- Fornecer assets simples para validação manual ou QA local.

## Quando usar

Use apenas em contexto de QA, dev ou demonstração técnica do próprio Foundation.

## Como usar

Use os assets de teste como referência de configuração mínima para validar comportamento de pooling ou infraestrutura relacionada.

## Quando não usar

- Não usar assets de testing como conteúdo de produção.
- Não usar como fonte de verdade de configuração real.

## Arquivos principais

- `Foundation/Platform/Testing/PoolDefinitionAsset.asset`
- `Foundation/Platform/Testing/PoolDefinitionAssetSFXGlobal.asset`
- `Foundation/Platform/Testing/TestPools.prefab`

## Cuidados

- Testing é suporte.
- Config real deve morar no owner do módulo consumidor ou no asset canônico de runtime.
