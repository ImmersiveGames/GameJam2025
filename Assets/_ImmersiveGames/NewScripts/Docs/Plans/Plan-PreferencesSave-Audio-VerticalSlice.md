# Plan - Preferences Save / Audio Vertical Slice

Status: **Draft**

## Objetivo

Criar o primeiro slice vertical da camada canônica de preferências, restrito a áudio, com backend provisório `PlayerPrefs` atrás de abstração.
O slice deve permitir carregar, aplicar, alterar e persistir preferências de áudio sem acoplar persistência à UI e sem tocar em progressão, checkpoint, vídeo, idioma, input remap ou acessibilidade.

## Escopo

Entram neste slice:

- `MasterVolume`
- `BgmVolume`
- `SfxVolume`
- contrato canônico de preferências com `profileId` e `slotId`
- load inicial da preferência antes da composição runtime de áudio
- apply no estado runtime de áudio já existente
- save explícito quando a preferência mudar por intenção de UI
- backend provisório `PlayerPrefs` via port/adapters

## Fora de escopo

Ficam fora deste slice:

- progressão persistente
- checkpoint/resume
- vídeo
- idioma
- input remap
- acessibilidade
- `BgmCategoryMultiplier` e `SfxCategoryMultiplier`
- persistência direta em binder/controller de menu
- persistência implícita em serviço global
- qualquer migração do legado `Scripts`

## Estado atual resumido

- Não existe preferência persistida canônica em `NewScripts`.
- O áudio já possui estado runtime mutável em `IAudioSettingsService` / `AudioSettingsService`.
- `AudioInstaller` seeda o estado a partir de `AudioDefaultsAsset` e `AudioRuntimeComposer` compõe o runtime.
- `AudioBgmService` e `AudioGlobalSfxService` consomem `IAudioSettingsService`.
- A UI de frontend atual só troca painéis; não há binder de options de áudio em `NewScripts`.
- O legado `AudioSettingsUI.cs` deve ser tratado apenas como intenção funcional:
  - abrir options e sincronizar sliders com estado atual;
  - mudar BGM atualiza estado e pode aplicar imediatamente;
  - mudar SFX atualiza estado e o runtime consome normalmente;
  - UI é writer de intenção, não owner da persistência.

## Alvo arquitetural

O slice deve nascer como camada de aplicação/orquestração com API estável e backend trocável.

Contrato alvo:

- `PreferencesSnapshot`
- `IPreferencesSaveService`
- `IPreferencesStateService`
- `IPreferencesBackend`
- adapter provisório `PlayerPrefs`

Seam atual a reutilizar:

- `AudioInstaller` como ponto de load inicial
- `AudioRuntimeComposer` como ponto de apply
- `AudioBgmService` e `AudioGlobalSfxService` como consumidores do estado aplicado

Regras de arquitetura:

- `AudioInstaller`, `AudioRuntimeComposer`, `AudioBgmService` e `AudioGlobalSfxService` não são owners do slice
- o backend provisório fica atrás de abstração
- o fluxo deve ser fail-fast para obrigatórios
- autosave só em marcos explícitos
- `profileId` e `slotId` entram no contrato desde o início

## Fases

### F0 - Fechar contrato e seams

- Definir o recorte mínimo do snapshot de áudio.
- Fixar `profileId` e `slotId` no contrato.
- Separar claramente `Preferences` de qualquer noção de progressão.
- Mapear o ponto de load inicial e o ponto de apply no runtime atual.

Critério de aceitação:

- o slice de áudio está delimitado em contrato, sem misturar outros domínios;
- os seams existentes estão nomeados e não exigem mudança estrutural ampla.

### F1 - Estruturar aplicação e backend abstrato

- Definir a API canônica de preferências.
- Definir o port de backend.
- Definir o modelo de snapshot mínimo para áudio.
- Preparar a composição para carregar estado antes do runtime de áudio.

Critério de aceitação:

- existe um contrato estável para leitura/aplicação;
- existe separação entre API do jogo e backend;
- nada depende de `PlayerPrefs` diretamente no consumidor.

### F2 - Adapter provisório `PlayerPrefs`

- Implementar o backend provisório como adapter concreto.
- Persistir apenas `MasterVolume`, `BgmVolume` e `SfxVolume`.
- Manter `BgmCategoryMultiplier` e `SfxCategoryMultiplier` como config técnica de projeto.

Critério de aceitação:

- o backend salva e lê o slice mínimo de áudio;
- falhas de contrato/configuração são explícitas;
- o adapter não invade UI nem módulos de macroflow.

### F3 - Integração com apply runtime

- Carregar preferências no boot antes do composer de áudio.
- Aplicar o estado carregado em `IAudioSettingsService`.
- Confirmar que `AudioBgmService` e `AudioGlobalSfxService` continuam consumindo o estado por contrato.

Critério de aceitação:

- o áudio runtime reflete a preferência carregada sem roteamento manual espalhado;
- o apply acontece em ponto central, não na UI.

### F4 - Writer de intenção e save explícito

- Criar o ponto de intenção para mudanças de preferência de áudio.
- Conectar a UI futura de options como writer, sem ownership de persistência.
- Disparar save apenas quando a mudança for explícita.

Critério de aceitação:

- a UI não conhece backend;
- a UI não chama persistência direta;
- mudanças de preferência viram intenção e save explícito.

## Riscos

- Confundir `AudioDefaultsAsset` com preferência persistida do jogador.
- Salvar preferências direto em binder/controller de menu.
- Reusar `RuntimeModeConfig.inputModes` como se fosse preferências de usuário.
- Fazer `AudioInstaller` ou `AudioRuntimeComposer` virarem owner de persistência.
- Tentar resolver isso junto com progressão/checkpoint e aumentar o acoplamento cedo demais.
- Introduzir `PlayerPrefs` no consumer e quebrar o backend trocável.

## Critérios de aceitação

- `MasterVolume`, `BgmVolume` e `SfxVolume` podem ser carregados, aplicados e persistidos.
- `profileId` e `slotId` fazem parte do contrato.
- o backend é trocável sem alterar o contrato público do slice.
- a UI não é owner da persistência.
- `AudioInstaller` e `AudioRuntimeComposer` funcionam como seams de integração, não como repositório de estado salvo.
- o slice não toca em progressão, checkpoint ou outros domínios.

## Arquivos/pontos prováveis de integração

- [Modules/Audio/Runtime/IAudioSettingsService.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Runtime/IAudioSettingsService.cs)
- [Modules/Audio/Runtime/AudioSettingsService.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Runtime/AudioSettingsService.cs)
- [Modules/Audio/Bootstrap/AudioInstaller.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Bootstrap/AudioInstaller.cs)
- [Modules/Audio/Bootstrap/AudioRuntimeComposer.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Bootstrap/AudioRuntimeComposer.cs)
- [Modules/Audio/Runtime/AudioBgmService.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Runtime/AudioBgmService.cs)
- [Modules/Audio/Runtime/AudioGlobalSfxService.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Runtime/AudioGlobalSfxService.cs)
- [Modules/Audio/Config/AudioDefaultsAsset.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Audio/Config/AudioDefaultsAsset.cs)
- [Infrastructure/Config/BootstrapConfigAsset.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset.cs)
- [Modules/Navigation/Bindings/FrontendPanelsController.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Bindings/FrontendPanelsController.cs)
- [Modules/Navigation/Bindings/FrontendShowPanelButtonBinder.cs](C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Bindings/FrontendShowPanelButtonBinder.cs)

## Observabilidade mínima esperada

- load inicial da preferência com perfil/slot usados
- backend selecionado
- apply do estado de áudio carregado
- save explícito quando volume muda
- skip de save quando não houver mudança real
- falha explícita para configuração ausente ou contrato inválido
- log canônico suficiente para auditar origem da preferência e do backend
