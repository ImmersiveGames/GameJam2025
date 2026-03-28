# Decisions & Guardrails — Preferences Save / Audio

Status: **Locked for current slice**
Data: 2026-03-28

## 1. O primeiro slice é Preferences Save / Audio

A implementação inicial do módulo de preferências começa por áudio.

Motivo:
- é o domínio com seam mais claro no estado atual;
- não depende de restore de mundo/cena;
- não invade o pipeline macro do jogo;
- exercita o desenho principal da camada de preferências.

## 2. Este slice não é o módulo completo de Save

Este trabalho **não** fecha:

- progressão persistente;
- checkpoint/resume;
- save points de gameplay;
- cloud save;
- múltiplos domínios de preferência.

Ele fecha apenas o primeiro slice vertical de **preferências de áudio**.

## 3. Backend provisório aprovado

Backend provisório deste slice:

- `PlayerPrefs`

Regra:
- `PlayerPrefs` é adapter de infraestrutura;
- `PlayerPrefs` não é o contrato do sistema;
- o backend deve ficar atrás de abstração.

## 4. UI não é owner da persistência

A UI futura de options:

- lê estado atual para sincronizar controles;
- publica intenção de mudança;
- não fala com backend;
- não grava direto em `PlayerPrefs`;
- não resolve persistência por conta própria.

## 5. Config de projeto não é preferência do jogador

Continuam sendo **config de projeto**:

- `AudioDefaultsAsset`
- qualquer seed técnico equivalente
- multiplicadores/categorias técnicas que não foram aprovadas como preferência de jogador

Portanto:
- defaults seedam o runtime;
- preferências do jogador sobrescrevem o que for suportado pelo contrato;
- save não deve reclassificar config técnica como dado persistido do usuário.

## 6. Slice mínimo persistido

Persistir apenas:

- `MasterVolume`
- `BgmVolume`
- `SfxVolume`

Não persistir neste slice:

- `BgmCategoryMultiplier`
- `SfxCategoryMultiplier`

## 7. Contrato nasce preparado para evolução

Mesmo neste slice mínimo, o contrato já deve prever:

- `profileId`
- `slotId`

Regras:
- não assumir singleton global acidental;
- mesmo que a implementação inicial use um perfil/slot principal, o contrato não deve nascer travado nisso.

## 8. Load e apply devem ser centralizados

Pontos aprovados como seams:

- `AudioInstaller` como ponto de load inicial
- `AudioRuntimeComposer` como ponto de apply
- `AudioBgmService` e `AudioGlobalSfxService` como consumidores do estado aplicado

Regra:
- esses pontos integram o slice;
- eles não viram repositório de persistência;
- eles não armazenam backend;
- eles não substituem a camada de Preferences.

## 9. Guardrails de arquitetura

É proibido neste slice:

- salvar direto em binder/controller de menu;
- salvar direto em serviços de áudio;
- introduzir `PlayerPrefs` em consumer de áudio;
- misturar este slice com progressão/checkpoint;
- usar legado `Scripts` como base de migração;
- copiar fallbacks frouxos do legado para dentro do novo design.

## 10. Observabilidade mínima obrigatória

O slice deve emitir observabilidade suficiente para responder:

- qual backend foi usado;
- qual perfil/slot foi usado;
- quando ocorreu load;
- quando ocorreu apply;
- quando ocorreu save explícito;
- quando houve skip por ausência de mudança real;
- quando houve falha de contrato/configuração.

## 11. Critério de “done” do slice

Considerar este slice fechado quando:

- o boot consegue carregar preferências de áudio;
- o runtime de áudio reflete o estado carregado;
- a mudança explícita de preferência consegue persistir;
- a UI não conhece backend;
- o contrato público não depende de `PlayerPrefs`;
- o slice não toca em progressão, checkpoint ou outros domínios.
