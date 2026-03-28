# Overview — Preferences baseline v1: Audio + Video

Status: **Closed for this cycle**
Data: 2026-03-28

## Objetivo

Consolidar o baseline v1 da camada canônica de preferências, cobrindo áudio e vídeo, de modo que o projeto possa:

- carregar preferências de áudio e vídeo no boot;
- aplicar essas preferências ao runtime já existente;
- alterar preferências por intenção explícita de UI;
- persistir preferências sem acoplar a UI ao backend;
- manter backend trocável.

## Escopo fechado

Entram neste baseline:

- `MasterVolume`
- `BgmVolume`
- `SfxVolume`
- resolução selecionada
- modo fullscreen/windowed
- contrato com `profileId`
- contrato com `slotId`
- restore defaults por domínio
- preview de SFX no release do slider de `Sfx`
- presets de resolução comuns para desktop
- toggle fullscreen/windowed
- load inicial antes da composição runtime de áudio e vídeo
- apply centralizado no runtime correspondente
- save explícito quando a preferência mudar
- backend provisório `PlayerPrefs` atrás de abstração

## Fora de escopo

Não entram neste baseline:

- progressão persistente
- checkpoint/resume
- idioma
- input remap
- acessibilidade
- `BgmCategoryMultiplier`
- `SfxCategoryMultiplier`
- qualidade gráfica
- VSync
- monitor selection
- refresh-rate UI
- persistência direta em binder/controller de menu
- qualquer migração de legado `Scripts`

## Leitura arquitetural

Este baseline **não** cria o módulo completo de Save.
Ele fecha o primeiro vertical slice da camada canônica de preferências, usando áudio como domoínio inicial e expandindo para vídeo como segundo domínio fechado.

Princípios que governam este baseline:

- backend trocável;
- API pública estável;
- ownership explícito;
- fail-fast para obrigatórios;
- UI como writer de intenção;
- persistência fora da UI;
- separação entre config de projeto e preferência do jogador.

## Estado consolidado

O que já existe e deve ser tratado como seam:

- `IAudioSettingsService` / `AudioSettingsService` como estado runtime mutável;
- `AudioInstaller` como ponto natural de load inicial;
- `AudioRuntimeComposer` como ponto natural de apply;
- `AudioBgmService` e `AudioGlobalSfxService` como consumidores do estado aplicado;
- `VideoDefaultsAsset` como default canônico de vídeo;
- `VideoPreferencesOptionsBinder` como writer de intenção de vídeo;
- `PreferencesService` como owner canônico do estado e do commit.

O que **não** deve ser confundido com save de preferências:

- `AudioDefaultsAsset` como default técnico de projeto;
- configs globais de runtime;
- estado transitório de serviços;
- qualquer fallback de UI ou bootstrap manual vindo do legado.

## Fluxo alvo

1. Boot resolve o slice de Preferences.
2. O backend carrega preferências de áudio e vídeo do contrato atual.
3. O estado carregado é aplicado ao runtime canônico correspondente.
4. Serviços de áudio e vídeo consomem esse estado normalmente.
5. A UI altera preferências por intenção explícita.
6. A camada canônica decide salvar.
7. O backend persiste.

## Owners e não-owners

### Owners
- camada de Preferences: contrato, persistência, snapshot, save/load
- backend: leitura e escrita concreta
- UI de options: writer de intenção

### Não-owners
- `AudioInstaller`
- `AudioRuntimeComposer`
- `AudioBgmService`
- `AudioGlobalSfxService`
- `VideoPreferencesOptionsBinder` como owner de persistência
- `FrontendPanelsController`
- `FrontendShowPanelButtonBinder`

Esses pontos podem integrar o baseline, mas não devem virar owner da persistência.

## Marco fechado

O baseline v1 fechado é:

- carregar `MasterVolume`, `BgmVolume`, `SfxVolume`;
- carregar resolução e fullscreen/windowed;
- aplicar ao runtime central correspondente;
- permitir save explícito;
- manter `PlayerPrefs` atrás de abstração;
- restaurar defaults por domínio;
- manter a UI fora da persistência.
