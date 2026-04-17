# Base 1.0 - Checklist de Conformidade

Checklist curta para auditoria, smoke e revisao rapida da Base 1.0.

## Conformidade arquitetural

- [ ] O baseline tecnico fino nao voltou a absorver costura semantica de sessao.
- [ ] `SessionIntegration` continua sendo seam canonico, fino e explicito.
- [ ] `SessionIntegration` nao executa ownership de dominio, spawn ou reset.
- [ ] Consumers operacionais relevantes usam o seam correto, nao owners antigos por conveniencia.
- [ ] Nao surgiram bridges oportunistas novos em areas historicamente sensiveis.
- [ ] Logs principais distinguem claramente `owner`, `seam` e `executor`.
- [ ] Docs curtos e indices ainda leem a topologia certa da Base 1.0.

## Sinais de regressao

- [ ] Bootstrap voltou a compor semantica de sessao diretamente.
- [ ] Um consumer novo passou a ler owner antigo por atalho.
- [ ] `SessionIntegration` passou a assumir execucao concreta de dominio.
- [ ] Um bridge novo apareceu para esconder um seam que ja existe.
- [ ] Logs ou docs mascaram quem e owner, quem e seam e quem e executor.
- [ ] Termos historicos voltaram a aparecer como leitura operacional primaria.

## Validacao rapida

1. Verificar se `SessionIntegration` continua fino e sem ownership de dominio.
2. Verificar se `InputModes`, spawn e reset continuam consumindo o seam correto.
3. Conferir se os logs de bootstrap e runtime usam labels consistentes de `owner`, `seam` e `executor`.
4. Confirmar que docs curtos ainda apontam `ADR-0057` -> `ADR-0056` -> `ADR-0055` como trilho operacional.
5. Confirmar que nao existe novo bridge oportunista em superficie historicamente facil.

## Regra pratica

- Se dois ou mais itens de regressao marcarem `sim`, tratar como desvio de shape canonico.
- Se apenas itens historicos/documentais aparecerem sem impacto operacional, manter como residual e nao abrir arquitetura.
