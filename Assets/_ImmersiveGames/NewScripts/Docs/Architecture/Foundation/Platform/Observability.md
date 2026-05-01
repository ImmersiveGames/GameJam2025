# Foundation / Platform / Observability

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Infraestrutura de observabilidade técnica e asserção de invariantes do baseline.

## Para que serve

- Validar invariantes técnicas.
- Registrar inconsistências estruturais.
- Apoiar auditoria de runtime.

## Quando usar

Use para checagens técnicas que atravessam o baseline e precisam aparecer como evidência clara.

## Como usar

O pacote atual contém:

```text
Foundation/Platform/Observability/Baseline/BaselineInvariantAsserter.cs
```

Use esse tipo de peça para checar invariantes globais/técnicas, não para impor regra semântica de gameplay.

## Quando não usar

- Não usar observability como mecanismo de correção automática.
- Não usar invariant asserter para decidir fluxo de gameplay.
- Não transformar diagnóstico em owner de domínio.

## Arquivos principais

- `Foundation/Platform/Observability/Baseline/BaselineInvariantAsserter.cs`

## Cuidados

- Observabilidade deve detectar e explicar.
- Correção pertence ao módulo dono.
- Invariante estrutural quebrada deve ser tratada como problema explícito.
