# Overview — Preferences Save / Audio

Status: **Working Baseline**
Data: 2026-03-28

## Objetivo

Consolidar o primeiro slice vertical da camada canônica de preferências, restrito a áudio, de modo que o projeto possa:

- carregar preferências de áudio no boot;
- aplicar essas preferências ao runtime já existente;
- alterar preferências por intenção explícita de UI;
- persistir preferências sem acoplar a UI ao backend;
- manter backend trocável.

## Escopo do slice

Entram neste slice:

- `MasterVolume`
- `BgmVolume`
- `SfxVolume`
- contrato com `profileId`
- contrato com `slotId`
- load inicial antes da composição runtime de áudio
- apply centralizado no estado runtime de áudio
- save explícito quando a preferência mudar
- backend provisório `PlayerPrefs` atrás de abstração

## Fora de escopo

Não entram neste slice:

- progressão persistente
- checkpoint/resume
- vídeo
- idioma
- input remap
- acessibilidade
- `BgmCategoryMultiplier`
- `SfxCategoryMultiplier`
- persistência direta em binder/controller de menu
- qualquer migração de legado `Scripts`

## Leitura arquitetural

Este slice **não** cria o módulo completo de Save.
Ele cria o primeiro vertical slice da camada canônica de preferências, usando áudio como domínio inicial.

Princípios que governam este slice:

- backend trocável;
- API pública estável;
- ownership explícito;
- fail-fast para obrigatórios;
- UI como writer de intenção;
- persistência fora da UI;
- separação entre config de projeto e preferência do jogador.

## Estado atual consolidado

Hoje o projeto não possui persistência canônica de preferências em `NewScripts`.

O que já existe e deve ser tratado como seam:

- `IAudioSettingsService` / `AudioSettingsService` como estado runtime mutável;
- `AudioInstaller` como ponto natural de load inicial;
- `AudioRuntimeComposer` como ponto natural de apply;
- `AudioBgmService` e `AudioGlobalSfxService` como consumidores do estado aplicado.

O que **não** deve ser confundido com save de preferências:

- `AudioDefaultsAsset` como default técnico de projeto;
- configs globais de runtime;
- estado transitório de serviços;
- qualquer fallback de UI ou bootstrap manual vindo do legado.

## Fluxo alvo

1. Boot resolve o slice de Preferences.
2. O backend carrega preferências de áudio do contrato atual.
3. O estado carregado é aplicado ao estado runtime canônico de áudio.
4. Serviços de áudio consomem esse estado normalmente.
5. A UI futura de options altera preferências por intenção explícita.
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
- `FrontendPanelsController`
- `FrontendShowPanelButtonBinder`

Esses pontos podem integrar o slice, mas não devem virar owner da persistência.

## Primeira entrega esperada

A primeira entrega boa deste slice é:

- carregar `MasterVolume`, `BgmVolume`, `SfxVolume`;
- aplicar ao runtime central de áudio;
- permitir save explícito;
- manter `PlayerPrefs` atrás de abstração;
- preparar o terreno para futuras preferências sem já expandir o módulo inteiro.
