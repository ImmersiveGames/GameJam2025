# ADR-0028 Audio Rollout Tracker

Data: 2026-03-20
Status geral: IN PROGRESS
Estratégia: 1 fase por PR (sequencial)
Pacote A: FECHADO (F0/F1/F2 concluídos, sem playback real ainda)

## Baseline obrigatório

- ADR: `Docs/ADRs/ADR-0028-AudioModule.md`
- Auditoria legado vs canônico: `Docs/Reports/Audits/2026-03-20/Legacy-AudioSystem-Audit-vs-Canonical-ADR.md`

## Guardrails ativos

- Não abrir Unity.
- Não executar build de player.
- Sem migração big-bang.
- Sem acoplamento estrutural de `Modules/Audio/**` com módulos consumidores.
- Sem remover comportamento antigo antes de substituto observável.

## Nota de escopo standalone

- F0 ate F7 pertencem ao core standalone de `Modules/Audio/**`.
- Integracoes com modulos consumidores entram apenas em F8+.
- Catalogos/collections/profiles especificos de `Navigation`, `Gameplay`, `LevelFlow`, `Skin` e similares nao pertencem ao core de `Modules/Audio/**`.

## Papel do IAudioRoutingResolver

- `IAudioRoutingResolver` pertence ao core standalone de audio como concern interno de roteamento base (mixer/routing).
- Nao e porta de integracao intermodular e nao deve ser promovido para dependencias de consumidores.
- Qualquer traducao de dominio consumidor para audio deve ocorrer em bridges/adapters (F8+), sem transferir ownership do routing para modulos consumidores.

## Fases (status)

| Fase | Status | Observações |
|---|---|---|
| F0 - Baseline e rastreio | DONE | Tracker criado, baseline congelado e matriz de paridade fixada para execução incremental. |
| F1 - Contratos e estrutura | DONE | Estrutura `Modules/Audio/**` criada com contratos e assets base do ADR-0028. |
| F2 - Bootstrap + defaults/settings/routing base | DONE | Estágio `Audio` adicionado ao pipeline global antes de GameLoop, com registro DI e logs de boot. |
| F3 - GlobalAudio BGM | NOT STARTED | Próxima fase. |
| F4 - GlobalAudio SFX direto | NOT STARTED | Dependente de F3. |
| F5 - GlobalAudio pooled voices | NOT STARTED | Dependente de F4. |
| F6 - EntityAudio semântico | NOT STARTED | Dependente de F5. |
| F7 - EntityAudioEmitter mínimo | NOT STARTED | Dependente de F6. |
| F8 - Integrações opcionais | NOT STARTED | Dependente de F7. |
| F9 - Tooling/QA/hardening | NOT STARTED | Dependente de F8. |
| F10 - Cleanup final | NOT STARTED | Dependente de F9. |

## Matriz de paridade (freeze executável)

A matriz de paridade oficial segue a definida no plano aprovado e deve ser usada como checklist de stop/go por fase.

## Stop/Go por fase

Só avança quando:

- Entregáveis completos.
- Teste manual objetivo definido para a fase.
- Risco residual registrado.
- Regressões da própria fase tratadas.

## Regressões abertas

- Nenhuma registrada até o fechamento do Pacote A.

## Riscos residuais conhecidos após Pacote A

- Sem playback real ainda (`IAudioBgmService`/`IGlobalAudioService`/`IEntityAudioService` permanecem somente contratos).
- `AudioDefaultsAsset` ausente no bootstrap entra em modo degradado com fallback runtime (já logado/trackeado).
- Contrato detalhado de mixer/routing segue base inicial (refino previsto para F3+).

## Atualização de cleanup (2026-03-20)

- Status: DONE (eixo de contrato `AudioSfxVoiceProfileAsset` x Pooling).
- `Modules/Audio/Config/AudioSfxVoiceProfileAsset.cs` removido de `PoolData` legado e alinhado para `PoolDefinitionAsset` canônico.
- Assets em `Modules/Audio/Content/VoicesProfile/**` mantidos sem pool configurado (null), já no novo shape serializado.
- Escopo respeitado: sem runtime de BGM, sem runtime de SFX pooled e sem consumo real de `IPoolService` nesta etapa.
