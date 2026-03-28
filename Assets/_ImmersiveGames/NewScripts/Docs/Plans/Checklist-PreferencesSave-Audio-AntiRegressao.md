# Checklist — Preferences Save / Audio (Anti-regressão)

Status: **Open**
Data: 2026-03-28

## A. Boundary do slice

- [ ] O slice continua restrito a **Preferences Save / Audio**
- [ ] Não entrou progressão persistente
- [ ] Não entrou checkpoint/resume
- [ ] Não entrou vídeo
- [ ] Não entrou idioma
- [ ] Não entrou input remap
- [ ] Não entrou acessibilidade

## B. Dados persistidos

- [ ] Persistimos apenas `MasterVolume`
- [ ] Persistimos apenas `BgmVolume`
- [ ] Persistimos apenas `SfxVolume`
- [ ] `BgmCategoryMultiplier` continua fora
- [ ] `SfxCategoryMultiplier` continua fora

## C. Ownership

- [ ] A UI não grava direto em backend
- [ ] A UI não chama `PlayerPrefs` diretamente
- [ ] `AudioInstaller` não virou owner de persistência
- [ ] `AudioRuntimeComposer` não virou owner de persistência
- [ ] `AudioBgmService` não virou owner de persistência
- [ ] `AudioGlobalSfxService` não virou owner de persistência
- [ ] `FrontendPanelsController` não virou owner de persistência
- [ ] `FrontendShowPanelButtonBinder` não virou owner de persistência

## D. Contrato

- [ ] Existe contrato canônico para preferências
- [ ] O contrato inclui `profileId`
- [ ] O contrato inclui `slotId`
- [ ] O contrato público não depende de `PlayerPrefs`
- [ ] O backend concreto fica atrás de abstração

## E. Integração

- [ ] O load acontece antes da composição runtime de áudio
- [ ] O apply acontece em ponto central
- [ ] Os serviços de áudio continuam consumindo estado runtime por contrato
- [ ] Não há roteamento manual espalhado pela UI

## F. Semântica correta

- [ ] `AudioDefaultsAsset` continua sendo config de projeto
- [ ] Defaults de projeto não foram promovidos indevidamente a preferência persistida
- [ ] Estado transitório de runtime não foi confundido com save
- [ ] Legado `Scripts` foi usado apenas como intenção, não como base de migração

## G. Backend

- [ ] O backend provisório atual é `PlayerPrefs`
- [ ] `PlayerPrefs` está encapsulado em adapter
- [ ] Existe caminho claro para trocar o backend depois
- [ ] Não há dependência do backend em consumers do slice

## H. Observabilidade

- [ ] Há log de load inicial
- [ ] Há log do backend selecionado
- [ ] Há log de apply do estado carregado
- [ ] Há log de save explícito
- [ ] Há log de skip quando não há mudança real
- [ ] Há falha explícita para contrato/configuração inválida

## I. Definição de pronto deste ciclo

- [ ] Carrega `MasterVolume`, `BgmVolume`, `SfxVolume`
- [ ] Aplica corretamente ao runtime
- [ ] Salva por intenção explícita
- [ ] Mantém backend trocável
- [ ] Mantém a UI fora da persistência
- [ ] Não invade outros domínios
