> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0018 — Fade / TransitionStyle / Resiliência (nome histórico preservado)

## Status

- Estado: **Implementado parcialmente; runtime atual é híbrido**
- Data (decisão): **2026-02-15**
- Última atualização: **2026-03-25**
- Escopo: `SceneFlow` Fade + `TransitionStyleAsset`

## Precedência

Este ADR prevalece sobre `ADR-0009` **apenas** no tema de policy/resiliência do fade/style.

O envelope macro de ordem continua definido por `ADR-0009`.

## Contexto

O nome deste ADR foi preservado, mas o runtime atual não implementa um “soft-fail universal” para fade/style.

A leitura correta hoje é:
- a configuração estrutural de style/profile continua obrigatória e fail-fast;
- a execução do fade tem tratamento de degradação apenas em parte do trilho;
- o comportamento final varia entre validação/configuração e falha operacional de execução.

## Decisão canônica atual do runtime

### 1) `TransitionStyleAsset.profileRef` é obrigatório

`TransitionStyleAsset` continua sendo owner estrutural de:
- `profileRef`
- `useFade`

Quando `profileRef` está ausente, o contrato atual é **fail-fast**, não soft-fail.

Isso aparece em:
- validação/editor (`SceneFlowConfigValidator`);
- `TransitionStyleAsset.ToDefinitionOrFail(...)`.

### 2) Falha estrutural de config não entra em soft-fail

Continuam obrigatórios/fail-fast:
- `startupTransitionStyleRef` válido no bootstrap;
- `TransitionStyleAsset` com `profileRef` válido;
- wiring estrutural necessário para a transição.

### 3) Falha operacional do fade tem tratamento híbrido

No runtime atual:
- `FadeService` lança erro explícito quando `FadeScene`/`FadeController` não podem ser garantidos;
- `SceneFlowFadeAdapter` captura falhas de execução e aplica:
  - **degradação em `UNITY_EDITOR` / `DEVELOPMENT_BUILD`**;
  - **exceção fatal em build não-development**.

Ou seja, o comportamento atual é o inverso do antigo texto “soft-fail sempre”: ele é **mais tolerante em dev** e **mais duro em runtime final**.

## O que este ADR passa a significar

A decisão real não é mais “fade/style é sempre soft-fail”.

A decisão correta, refletindo o runtime atual, é:

- **config estrutural** de style/profile = **fail-fast**;
- **falha operacional** durante execução do fade = **degrada em dev/editor** e **fatal em runtime final**.

## Consequências

### Positivas
- erro estrutural de configuração continua visível cedo;
- falha operacional de fade não bloqueia iteração em ambiente de desenvolvimento;
- o fade continua explicitamente separado do resto da semântica do fluxo.

### Trade-offs
- o título do ADR ficou mais amplo que a implementação atual;
- a policy real é híbrida e precisa ser lida com cuidado;
- a documentação antiga de “soft-fail puro” estava desalinhada com o código atual.

## Relação com outros ADRs

- `ADR-0009`: envelope macro do fade no SceneFlow.
- `ADR-0019`: `TransitionStyleAsset` como owner estrutural do style.
- `ADR-0025`: gate macro com etapa de level antes do `FadeOut`.

## Resumo operacional

Para o runtime atual, leia assim:
- `TransitionStyleAsset` inválido **não** é soft-fail;
- fade quebrado em execução pode degradar em dev/editor;
- build final atual trata essa falha de forma fatal.
