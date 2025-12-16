Perfeito — encontrei o arquivo **ReadMe.md** (versão antiga) e ele ainda descreve o sistema antigo de câmera baseado em **Camera.main**, que não existe mais no fluxo atual.
Precisamos **reescrever essa seção completamente** para refletir:

* O novo **CameraResolverService**
* O **GameplayCameraBinder**
* O **CanvasCameraBinder** atualizado (evento + DI + multi-player ready)

A seguir está a **nova versão do ReadMe.md**, totalmente substituta e compatível com seu novo sistema de câmera.
Você pode colar direto no seu arquivo atual.

---

# 🎥 Sistema de Câmera — Guia de Uso (v2.0)

## 📚 Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura do Sistema](#arquitetura-do-sistema)
3. [Componentes](#componentes)
4. [Fluxo de Funcionamento](#fluxo-de-funcionamento)
5. [Integração no Projeto](#integração-no-projeto)
6. [Boas Práticas](#boas-práticas)

---

## 🎯 Visão Geral

O sistema de câmera utiliza agora um **resolver global** (`ICameraResolver`) para garantir que todos os sistemas dependentes de câmera (Player, Canvas, UI, efeitos, etc.) utilizem **a câmera correta da cena de gameplay**, independentemente de Bootstrap, UI Global ou multi-scene.

Este design substitui totalmente o antigo uso de `Camera.main`, tornando o sistema:

* Determinístico
* Multi-scene seguro
* Multiplayer-ready
* Compatível com DI (DependencyManager)
* Atualizável em runtime (camera swap, cutscenes, múltiplos jogadores)

---

## 🧩 Arquitetura do Sistema

O sistema é composto por três elementos principais:

### **1. ICameraResolver (interface global)**

Resolve qual câmera deve ser usada para um dado `playerId`.

Funções principais:

* Registrar / remover câmeras
* Obter câmera atual por jogador
* Obter câmera padrão (player 0)
* Notificar quando a câmera muda

---

### **2. CameraResolverService (implementação global)**

Registrado no `DependencyBootstrapper`, é responsável por:

* Armazenar câmeras indexadas por player
* Suportar multiplayer (várias câmeras simultâneas)
* Emitir eventos quando a câmera padrão muda
* Evitar dependência de `Camera.main`

---

### **3. Bind Components**

Dois componentes conectam objetos do jogo ao resolver:

#### **3.1. GameplayCameraBinder**

Adicionado à câmera principal da GameplayScene.
Responsável por registrar a câmera no resolver:

```
playerId = 0 (default)
```

#### **3.2. CanvasCameraBinder** (versão atualizada)

* Vincula um Canvas WorldSpace à câmera correta do resolver
* Reage automaticamente à troca de câmera
* Remove subscription de eventos no Disable/Destroy
* Evita erro de MissingReferenceException ao sair do Play Mode
* Não depende mais de `Camera.main`

---

## 🔁 Fluxo de Funcionamento

### **1. Bootstrap**

O `DependencyBootstrapper` registra:

```csharp
EnsureGlobal<ICameraResolver>(() => new CameraResolverService());
```

### **2. GameplayScene Carregada**

A câmera da gameplay registra-se automaticamente via `GameplayCameraBinder`.

### **3. PlayerMovementController**

O player obtém a câmera correta via DI:

```
camera = resolver.GetDefaultCamera();
```

E atualiza automaticamente caso a câmera mude:

```
resolver.OnDefaultCameraChanged += SetCamera;
```

### **4. CanvasCameraBinder**

Para canvases em `WorldSpace`, o sistema define:

```
canvas.worldCamera = resolver.GetDefaultCamera();
```

E também atualiza em caso de troca da câmera padrão.

---

## 🧱 Componentes — Descrição Resumida

### **GameplayCameraBinder**

* Deve estar na câmera de gameplay
* Responsável pelo registro no resolver
* Suporte a multiplayer via `playerId`

### **CanvasCameraBinder**

* Deve ser usado **somente** em Canvas WorldSpace
* Obtém câmera via resolver
* Atualiza automaticamente via evento
* Evita acesso a objetos destruídos

### **CameraResolverService**

* Armazena câmeras
* Resolve câmera padrão
* Notifica alterações
* Integra-se via `DependencyManager`

---

## 💡 Integração no Projeto

### **1. Adicione GameplayCameraBinder na câmera principal da GameplayScene**

### **2. Remova a dependência da tag MainCamera**

* BootstrapCamera não deve ser MainCamera
* UI cameras também não precisam mais ter tag
* PlayerMovementController não usa mais `Camera.main`

### **3. Para Canvas WorldSpace**

Adicionar `CanvasCameraBinder` é suficiente:

* Ele injeta `ICameraResolver`
* Ele atualiza automaticamente o `worldCamera`

### **4. Múltiplos Players (futuro)**

O sistema já suporta:

```
RegisterCamera(playerId, camera)
GetCamera(playerId)
```

---

## ✅ Boas Práticas

| Situação                                   | Recomendação                                              |
| ------------------------------------------ | --------------------------------------------------------- |
| Multi-scene com Bootstrap + Gameplay + UI  | Sempre usar CameraResolver                                |
| Troca de câmera em runtime (cutscene/zoom) | Invocar RegisterCamera novamente                          |
| Multiplayer local                          | Um GameplayCameraBinder por playerId                      |
| Canvas sendo destruído em trocas de cena   | CanvasCameraBinder já se desinscreve sozinho (seguro)     |
| Debug                                      | Use DebugUtility.LogWarning quando resolver retornar null |

---

## 📌 Conclusão

Esta versão (v2.0) substitui completamente o modelo antigo baseado em `Camera.main`.
O sistema agora é:

* Modular
* Determinístico
* Testável
* Integrado à DI
* Pronto para multiplayer
* Seguro contra destruição de objetos durante eventos

---

Se quiser, posso atualizar também:

* A documentação geral (`PlayerControllerSystem.md`)
* O ADR para incluir o CanvasCameraBinder atualizado
* Um diagrama de fluxo visual (em Mermaid)
* Uma secção “Como diagnosticar bugs de câmera no projeto”

Deseja incluir mais alguma dessas no README?
