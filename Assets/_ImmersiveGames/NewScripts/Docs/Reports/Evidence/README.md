# Evidências — metodologia e estrutura

Este diretório contém **evidências arquivadas** (snapshots) usadas para:

- Sustentar ADRs com prova observável.
- Detectar regressões ao longo do tempo.
- Reduzir ruído em `Docs/Reports` mantendo um conjunto mínimo e organizado de artefatos.

## Princípios

1. **Log do Console como evidência primária**
   - A evidência primária é o **log bruto do Console (Unity)** produzido por uma execução real.
   - Quando necessário, o log pode ser copiado para um arquivo `.log` apenas como **espelho estático** do Console.
   - Logs gerados por ferramentas/scripts podem falhar; se houver divergência, o Console continua sendo a fonte de verdade.

2. **Contrato como fonte de tokens canônicos**
   - `Docs/Reports/Observability-Contract.md` define o contrato de evidências (tokens/strings).
   - Verificações automatizadas são opcionais; o snapshot deve sempre conter um resumo curado com âncoras observáveis.

3. **Snapshot datado e imutável**
   - Um snapshot é identificado por data (`YYYY-MM-DD`) e não deve ser reescrito.
   - Cada snapshot representa o estado do sistema em um momento, e serve como baseline para regressão.

## Estrutura

- `Evidence/YYYY-MM-DD/`
  - `Baseline-2.1-Evidence-YYYY-MM-DD.md` (resumo canônico do snapshot)
  - `Logs/` (opcional): espelho do Console em `.log`
  - `Verifications/` (opcional): checagens/âncoras curadas para navegação

- `Evidence/LATEST.md`
  - Aponta para o snapshot mais recente considerado canônico.

## Regras para ADR

- **ADR em status “Aceito”**
  - Deve referenciar pelo menos **um** snapshot datado (`Evidence/YYYY-MM-DD/...`).
  - Pode também referenciar `Evidence/LATEST.md` como ponte para regressão contínua.

- **ADR em status “Proposto/Em andamento”**
  - Pode referenciar apenas `Evidence/LATEST.md` até o fechamento.

## Como arquivar uma nova evidência (quando um ADR for fechado)

1. Rodar o fluxo/cenário relevante (ex.: Baseline 2.1).
2. Copiar o log do **Console** (fonte de verdade) e, se desejado, salvar um espelho em `Evidence/YYYY-MM-DD/Logs/`.
3. Gerar um resumo canônico (arquivo `Baseline-2.1-Evidence-YYYY-MM-DD.md`) com links para os artefatos e âncoras.
4. Atualizar `Evidence/LATEST.md` para apontar para a nova data.
5. Atualizar o(s) ADR(s) com os links de evidência.
6. Registrar no `Docs/CHANGELOG-docs.md`.

## Retenção e limpeza

- É aceitável remover relatórios antigos em `Docs/Reports` desde que:
  - `Observability-Contract.md` seja mantido; e
  - `Evidence/` contenha os snapshots datados necessários.
