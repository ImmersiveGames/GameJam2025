# Preferences baseline v1: Audio + Video concluídos

Data: 2026-03-28
Status final: PASS / Closed for this cycle

## Contexto

O primeiro vertical slice da camada canônica de Preferences foi fechado em duas frentes:

- Audio como trilho inicial do save canônico;
- Video como expansão inicial da mesma camada, com defaults, apply e persistência.

O objetivo desta rodada foi concluir o baseline de Preferences sem abrir progressão, checkpoint ou outros domínios.

Resultado macro registrado: Preferences baseline v1: Audio + Video concluídos.

## Escopo fechado

- Preferences / Audio
- Preferences / Video
- bootstrap/load inicial
- apply centralizado no runtime
- persistência atrás de backend canônico
- restore defaults por domínio
- UI como writer de intenção

## O que foi validado em Audio

- load no boot;
- apply durante drag;
- commit/save no release;
- restore defaults;
- preview de SFX no release do slider de `Sfx`;
- logs canônicos de `press/release`;
- logs canônicos de `save_executed / no_change`;
- logs canônicos de `preview requested / preview play dispatch`.

## O que foi validado em Video

- estado canônico de vídeo no slice de Preferences;
- defaults via `VideoDefaultsAsset`;
- load no boot;
- apply via `Screen.SetResolution(...)`;
- resolução por presets comuns;
- toggle fullscreen/windowed;
- persistência;
- restore defaults;
- UI atuando apenas como writer de intenção, sem chamar runtime/backend diretamente.

## Limite conhecido / próximo passo recomendado

- A trilha funcional foi confirmada em Editor pelos logs e pelo comportamento observado no fluxo.
- A confirmação final de resolução, janela e fullscreen ainda deve ser feita em build desktop.

Próximo passo recomendado:

- smoke test em build desktop para confirmar comportamento visual final de resolução/fullscreen.

## Status final

PASS / Closed for this cycle
