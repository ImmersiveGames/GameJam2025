# Architecture Audit (Step 0)

Gerado em: 2025-12-12 10:20:27

Total de arquivos analisados: **358**
Arquivos com achados: **84**

## Sumário de ocorrências (por padrão)

- **EventBinding**: 156
- **.Instance**: 79
- **FilteredEventBus**: 73
- **EventBus.Register**: 47
- **EventBus.Unregister**: 47
- **ObjectPool**: 32
- **PoolManager**: 27
- **PooledObject**: 15
- **Time.timeScale**: 11
- **SingletonBase**: 10
- **FindFirstObjectByType**: 2
- **FindObjectOfType**: 2
- **GameObject.Find**: 2
- **RegisterForObject**: 2
- **RegisterForScene**: 2

## Listas rápidas (arquivos)

### Time.timeScale
- Assets/_ImmersiveGames/Scripts/GameManagerSystems/GameManager.SceneFlow.cs
- Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopStates.cs
- Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs

### Find usage (FindObjectOfType/FindFirstObjectByType/GameObject.Find)
- Assets/Plugins/Trinary Software/Timing.cs
- Assets/_ImmersiveGames/Scripts/AudioSystem/Core/AudioRuntimeRoot.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/EaterDesireUI.cs
- Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Presentation/Bridges/WorldSpaceBillboard.cs

### Singleton/Instance usage
- Assets/_ImmersiveGames/Scripts/AnimationSystems/Base/AnimationControllerBase.cs
- Assets/_ImmersiveGames/Scripts/AudioSystem/Base/SoundBuilder.cs
- Assets/_ImmersiveGames/Scripts/AudioSystem/Core/AudioBootstrapper.cs
- Assets/_ImmersiveGames/Scripts/AudioSystem/Core/AudioSystemBootstrap.cs
- Assets/_ImmersiveGames/Scripts/AudioSystem/Services/AudioSfxService.cs
- Assets/_ImmersiveGames/Scripts/DamageSystem/DamageExplosionModule.cs
- Assets/_ImmersiveGames/Scripts/DamageSystem/DamageReceiver.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/Behavior/EaterBehavior.Core.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/Behavior/EaterBehavior.DesiresAndWorldHelpers.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/Behavior/EaterBehavior.StateMachine.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/EaterDesireService.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/EaterDesireUI.cs
- Assets/_ImmersiveGames/Scripts/GameManagerSystems/GameManager.SceneFlow.cs
- Assets/_ImmersiveGames/Scripts/GameManagerSystems/GameManager.cs
- Assets/_ImmersiveGames/Scripts/GameManagerSystems/PlayerManager.cs
- Assets/_ImmersiveGames/Scripts/GameplaySystems/GameplayManager.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefensePoolRunner.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefenseWaveRunner.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/PlanetsManager.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Services/PlanetInteractService.cs
- Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Shooting/PlayerShootController.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/ActorResourceCanvas.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/ActorResourceComponent.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/DynamicCanvasBinder.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/SceneCanvasBinder.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/CanvasPipelineManager.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Test/EntityDebugUtility.cs
- Assets/_ImmersiveGames/Scripts/SceneManagement/Editor/SceneFlowDebugTools.cs
- Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopStateMachine.cs
- Assets/_ImmersiveGames/Scripts/StateMachineSystems/StateDependentBehavior.cs
- Assets/_ImmersiveGames/Scripts/TimerSystem/GameTimer.cs
- Assets/_ImmersiveGames/Scripts/TimerSystem/TimerDisplay.cs
- Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs
- Assets/_ImmersiveGames/Scripts/UI/DirectionIndicatorManager.cs
- Assets/_ImmersiveGames/Scripts/UI/DirectionIndicatorObjectUI.cs
- Assets/_ImmersiveGames/Scripts/UI/GameLoop/GameLoopRequestButton.cs
- Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs
- Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyInjector.cs
- Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs
- Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PooledObject.cs

### EventBus/EventBinding usage
- Assets/_ImmersiveGames/Scripts/ActorSystems/ActorMaster.cs
- Assets/_ImmersiveGames/Scripts/AnimationSystems/Components/AnimationResolver.cs
- Assets/_ImmersiveGames/Scripts/CompassSystems/CompassDamageLifecycleAdapter.cs
- Assets/_ImmersiveGames/Scripts/DetectionsSystems/Mono/AbstractDetectable.cs
- Assets/_ImmersiveGames/Scripts/DetectionsSystems/Mono/AbstractDetector.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/Animations/EaterAnimationController.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/EaterDesireUI.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/EaterPredicates.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/States/EaterHungryState.cs
- Assets/_ImmersiveGames/Scripts/GameManagerSystems/GameManager.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Debug/PlanetsInitializationLogger.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/PlanetDefenseEventHandler.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Managers/PlanetMarkingManager.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/PlanetsManager.cs
- Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Animations/PlayerAnimationController.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bridges/ResourceThresholdBridge.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/CanvasPipelineManager.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/ResourceLinkService.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Test/EntityDebugUtility.cs
- Assets/_ImmersiveGames/Scripts/SkinSystems/Configurable/SkinConfigurable.cs
- Assets/_ImmersiveGames/Scripts/SkinSystems/Controllers/ActorSkinController.cs
- Assets/_ImmersiveGames/Scripts/SkinSystems/Threshold/ResourceThresholdListener.cs
- Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopStateMachine.cs
- Assets/_ImmersiveGames/Scripts/StateMachineSystems/StateDependentBehavior.cs
- Assets/_ImmersiveGames/Scripts/StateMachineSystems/StateDependentService.cs
- Assets/_ImmersiveGames/Scripts/TimerSystem/GameTimer.cs
- Assets/_ImmersiveGames/Scripts/TimerSystem/TimerDisplay.cs
- Assets/_ImmersiveGames/Scripts/UI/Compass/CompassPlanetHighlightController.cs
- Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/Bind/BaseBindHandler.cs
- Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/Bind/EventBinding.cs
- Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/EventBus.cs
- Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/InjectableEventBus.cs
- Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/Interfaces/IEvent.cs

### FilteredEventBus usage
- Assets/_ImmersiveGames/Scripts/ActorSystems/ActorMaster.cs
- Assets/_ImmersiveGames/Scripts/AnimationSystems/Components/AnimationResolver.cs
- Assets/_ImmersiveGames/Scripts/CompassSystems/CompassDamageLifecycleAdapter.cs
- Assets/_ImmersiveGames/Scripts/DamageSystem/DamageLifecycleModule.cs
- Assets/_ImmersiveGames/Scripts/DamageSystem/Events/DamageEventDispatcher.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/Animations/EaterAnimationController.cs
- Assets/_ImmersiveGames/Scripts/EaterSystem/EaterPredicates.cs
- Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Animations/PlayerAnimationController.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bridges/ResourceThresholdBridge.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/ResourceThresholdService.cs
- Assets/_ImmersiveGames/Scripts/SkinSystems/Configurable/SkinConfigurable.cs
- Assets/_ImmersiveGames/Scripts/SkinSystems/Controllers/ActorSkinController.cs
- Assets/_ImmersiveGames/Scripts/SkinSystems/Threshold/ResourceThresholdListener.cs
- Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/FilteredEventBus.cs
- Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs

### Dependency registrations (RegisterForGlobal/Scene/Object)
- Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs
- Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/IDependencyProvider.cs

### Pooling usage (PoolManager/ObjectPool/PooledObject)
- Assets/_ImmersiveGames/Scripts/AudioSystem/Components/SoundEmitter.cs
- Assets/_ImmersiveGames/Scripts/AudioSystem/Services/AudioSfxService.cs
- Assets/_ImmersiveGames/Scripts/DamageSystem/DamageDealer.cs
- Assets/_ImmersiveGames/Scripts/DamageSystem/DamageExplosionModule.cs
- Assets/_ImmersiveGames/Scripts/FXSystems/ExplosionEffect.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/Minions/DefenseMinionController.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefensePoolRunner.cs
- Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefenseWaveRunner.cs
- Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Shooting/PlayerShootController.cs
- Assets/_ImmersiveGames/Scripts/ProjectilesSystems/BulletPoolable.cs
- Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/ActorResourceCanvas.cs
- Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/IPoolable.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/LifetimeManager.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PoolData.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PoolManager.cs
- Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PooledObject.cs


## Detalhe por arquivo (linhas)

> Observação: a numeração de linhas é aproximada ao arquivo atual; mudanças no arquivo mudam as linhas.

### Assets/Plugins/Trinary Software/Timing.cs
- Namespace: `MEC`
- Classes: `Timing`, `MECExtensionMethods1`, `MECExtensionMethods2`

- **FindObjectOfType**: linhas 2498, 2501
- **GameObject.Find**: linhas 177

### Assets/_ImmersiveGames/Scripts/ActorSystems/ActorMaster.cs
- Namespace: `_ImmersiveGames.Scripts.ActorSystems`
- Classes: `ActorMaster`

- **EventBinding**: linhas 32, 33, 34, 35, 44, 45, 46, 47
- **FilteredEventBus**: linhas 43, 49, 50, 51, 52, 132, 133, 134, 135

### Assets/_ImmersiveGames/Scripts/AnimationSystems/Base/AnimationControllerBase.cs
- Namespace: `_ImmersiveGames.Scripts.AnimationSystems.Base`
- Classes: `AnimationControllerBase`

- **.Instance**: linhas 84, 85, 97

### Assets/_ImmersiveGames/Scripts/AnimationSystems/Components/AnimationResolver.cs
- Namespace: `_ImmersiveGames.Scripts.AnimationSystems.Components`
- Classes: `AnimationResolver`

- **EventBinding**: linhas 20, 21, 90, 91
- **FilteredEventBus**: linhas 93, 94, 110, 115

### Assets/_ImmersiveGames/Scripts/AudioSystem/Base/SoundBuilder.cs
- Namespace: `_ImmersiveGames.Scripts.AudioSystem`
- Classes: `SoundBuilder`, `NullHandle`

- **.Instance**: linhas 149, 157

### Assets/_ImmersiveGames/Scripts/AudioSystem/Components/SoundEmitter.cs
- Namespace: `_ImmersiveGames.Scripts.AudioSystem`
- Classes: `SoundEmitter`

- **ObjectPool**: linhas 18
- **PooledObject**: linhas 12

### Assets/_ImmersiveGames/Scripts/AudioSystem/Core/AudioBootstrapper.cs
- Namespace: `_ImmersiveGames.Scripts.AudioSystem.Core`
- Classes: `AudioBootstrapper`

- **SingletonBase**: linhas 9

### Assets/_ImmersiveGames/Scripts/AudioSystem/Core/AudioRuntimeRoot.cs
- Namespace: `_ImmersiveGames.Scripts.AudioSystem.Core`
- Classes: `AudioRuntimeRoot`

- **GameObject.Find**: linhas 65

### Assets/_ImmersiveGames/Scripts/AudioSystem/Core/AudioSystemBootstrap.cs
- Namespace: `_ImmersiveGames.Scripts.AudioSystem.Core`
- Classes: `AudioSystemBootstrap`

- **.Instance**: linhas 62, 81

### Assets/_ImmersiveGames/Scripts/AudioSystem/Services/AudioSfxService.cs
- Namespace: `_ImmersiveGames.Scripts.AudioSystem.Services`
- Classes: `AudioSfxService`, `SoundEmitterHandle`, `NullAudioHandle`

- **.Instance**: linhas 125
- **ObjectPool**: linhas 19
- **PoolManager**: linhas 125, 128, 142

### Assets/_ImmersiveGames/Scripts/CompassSystems/CompassDamageLifecycleAdapter.cs
- Namespace: `_ImmersiveGames.Scripts.CompassSystems`
- Classes: `CompassDamageLifecycleAdapter`

- **EventBinding**: linhas 23, 24, 25, 77, 78, 79
- **FilteredEventBus**: linhas 81, 82, 83, 99, 100, 101

### Assets/_ImmersiveGames/Scripts/DamageSystem/DamageDealer.cs
- Namespace: `_ImmersiveGames.Scripts.DamageSystem`
- Classes: `DamageDealer`

- **PooledObject**: linhas 23, 30, 35

### Assets/_ImmersiveGames/Scripts/DamageSystem/DamageExplosionModule.cs
- Namespace: `_ImmersiveGames.Scripts.DamageSystem`
- Classes: `DamageExplosionModule`

- **.Instance**: linhas 45
- **ObjectPool**: linhas 16
- **PoolManager**: linhas 45, 48

### Assets/_ImmersiveGames/Scripts/DamageSystem/DamageLifecycleModule.cs
- Namespace: `_ImmersiveGames.Scripts.DamageSystem`
- Classes: `DamageLifecycleModule`

- **FilteredEventBus**: linhas 35, 41, 57, 63, 70

### Assets/_ImmersiveGames/Scripts/DamageSystem/DamageReceiver.cs
- Namespace: `_ImmersiveGames.Scripts.DamageSystem`
- Classes: `DamageReceiver`

- **.Instance**: linhas 464

### Assets/_ImmersiveGames/Scripts/DamageSystem/Events/DamageEventDispatcher.cs
- Namespace: `_ImmersiveGames.Scripts.DamageSystem`
- Classes: `DamageEventDispatcher`

- **FilteredEventBus**: linhas 12, 17

### Assets/_ImmersiveGames/Scripts/DetectionsSystems/Mono/AbstractDetectable.cs
- Namespace: `_ImmersiveGames.Scripts.DetectionsSystems.Mono`
- Classes: `AbstractDetectable`

- **EventBinding**: linhas 17, 18, 42, 43
- **EventBus.Register**: linhas 45, 46
- **EventBus.Unregister**: linhas 78, 79

### Assets/_ImmersiveGames/Scripts/DetectionsSystems/Mono/AbstractDetector.cs
- Namespace: `_ImmersiveGames.Scripts.DetectionsSystems.Mono`
- Classes: `AbstractDetector`

- **EventBinding**: linhas 15, 16, 32, 33
- **EventBus.Register**: linhas 35, 36
- **EventBus.Unregister**: linhas 43, 44

### Assets/_ImmersiveGames/Scripts/EaterSystem/Animations/EaterAnimationController.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem.Animations`
- Classes: `EaterAnimationController`

- **EventBinding**: linhas 15, 16, 17, 30, 31, 32
- **FilteredEventBus**: linhas 67, 68, 69, 93, 98, 103

### Assets/_ImmersiveGames/Scripts/EaterSystem/Behavior/EaterBehavior.Core.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem.Behavior`
- Classes: `EaterBehavior`

- **.Instance**: linhas 43, 44

### Assets/_ImmersiveGames/Scripts/EaterSystem/Behavior/EaterBehavior.DesiresAndWorldHelpers.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem.Behavior`
- Classes: `EaterBehavior`

- **.Instance**: linhas 32

### Assets/_ImmersiveGames/Scripts/EaterSystem/Behavior/EaterBehavior.StateMachine.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem.Behavior`
- Classes: `EaterBehavior`, `FalsePredicate`

- **.Instance**: linhas 143, 159, 175, 202, 218, 235, 252

### Assets/_ImmersiveGames/Scripts/EaterSystem/EaterDesireService.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem`
- Classes: `EaterDesireService`

- **.Instance**: linhas 499

### Assets/_ImmersiveGames/Scripts/EaterSystem/EaterDesireUI.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem`
- Classes: `EaterDesireUI`

- **.Instance**: linhas 401, 499
- **EventBinding**: linhas 37, 38, 39, 111, 436, 439
- **EventBus.Register**: linhas 112, 437, 440
- **EventBus.Unregister**: linhas 130, 454, 459
- **FindFirstObjectByType**: linhas 510

### Assets/_ImmersiveGames/Scripts/EaterSystem/EaterPredicates.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem`
- Classes: `DeathEventPredicate`, `ReviveEventPredicate`, `WanderingTimeoutPredicate`, `HungryChasingPredicate`, `ChasingEatingPredicate`, `EatingWanderingPredicate`, `EatingHungryPredicate`, `PlanetUnmarkedPredicate`

- **EventBinding**: linhas 16, 17, 18, 29, 30, 31, 72, 73, 74, 86, 87, 88, 240, 245
- **EventBus.Register**: linhas 246
- **EventBus.Unregister**: linhas 264
- **FilteredEventBus**: linhas 33, 34, 35, 45, 46, 47, 90, 91, 92, 108, 109, 110

### Assets/_ImmersiveGames/Scripts/EaterSystem/States/EaterHungryState.cs
- Namespace: `_ImmersiveGames.Scripts.EaterSystem.States`
- Classes: `EaterHungryState`

- **EventBinding**: linhas 17, 214
- **EventBus.Register**: linhas 215
- **EventBus.Unregister**: linhas 228

### Assets/_ImmersiveGames/Scripts/FXSystems/ExplosionEffect.cs
- Namespace: `_ImmersiveGames.Scripts.FXSystems`
- Classes: `ExplosionEffect`

- **PooledObject**: linhas 14

### Assets/_ImmersiveGames/Scripts/GameManagerSystems/GameManager.SceneFlow.cs
- Namespace: `_ImmersiveGames.Scripts.GameManagerSystems`
- Classes: `GameManager`

- **.Instance**: linhas 250, 341
- **Time.timeScale**: linhas 248

### Assets/_ImmersiveGames/Scripts/GameManagerSystems/GameManager.cs
- Namespace: `_ImmersiveGames.Scripts.GameManagerSystems`
- Classes: `GameManager`

- **.Instance**: linhas 45, 84, 197, 223
- **EventBinding**: linhas 26, 27, 28, 29, 30, 47, 50, 53, 56, 59
- **EventBus.Register**: linhas 48, 51, 54, 57, 60
- **EventBus.Unregister**: linhas 71, 72, 73, 74, 75

### Assets/_ImmersiveGames/Scripts/GameManagerSystems/PlayerManager.cs
- Namespace: `_ImmersiveGames.Scripts.GameManagerSystems`
- Classes: `PlayerManager`

- **SingletonBase**: linhas 8

### Assets/_ImmersiveGames/Scripts/GameplaySystems/GameplayManager.cs
- Namespace: `_ImmersiveGames.Scripts.GameplaySystems`
- Classes: `GameplayManager`

- **SingletonBase**: linhas 13

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Debug/PlanetsInitializationLogger.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Debug`
- Classes: `PlanetsInitializationLogger`

- **EventBinding**: linhas 16, 20
- **EventBus.Register**: linhas 21
- **EventBus.Unregister**: linhas 28

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/Minions/DefenseMinionController.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Defense`
- Classes: `DefenseMinionController`

- **PooledObject**: linhas 260

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/PlanetDefenseEventHandler.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Defense`
- Classes: `PlanetDefenseEventHandler`

- **EventBinding**: linhas 17, 18, 19, 20, 27, 28, 29, 30
- **EventBus.Register**: linhas 43, 44, 45, 46
- **EventBus.Unregister**: linhas 51, 52, 53, 54

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefensePoolRunner.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Defense`
- Classes: `RealPlanetDefensePoolRunner`

- **.Instance**: linhas 42, 99
- **ObjectPool**: linhas 14
- **PoolManager**: linhas 42, 45, 99, 102

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Defense/RealPlanetDefenseWaveRunner.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Defense`
- Classes: `RealPlanetDefenseWaveRunner`, `WaveLoop`, `PendingTarget`

- **.Instance**: linhas 126, 130
- **ObjectPool**: linhas 28
- **PoolManager**: linhas 126, 130

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Managers/PlanetMarkingManager.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Managers`
- Classes: `PlanetMarkingManager`

- **EventBinding**: linhas 31, 32, 45, 46
- **EventBus.Register**: linhas 48, 49
- **EventBus.Unregister**: linhas 119, 125

### Assets/_ImmersiveGames/Scripts/PlanetSystems/PlanetsManager.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems`
- Classes: `PlanetsManager`

- **.Instance**: linhas 262
- **EventBinding**: linhas 79, 109
- **EventBus.Register**: linhas 110
- **EventBus.Unregister**: linhas 471
- **SingletonBase**: linhas 19

### Assets/_ImmersiveGames/Scripts/PlanetSystems/Services/PlanetInteractService.cs
- Namespace: `_ImmersiveGames.Scripts.PlanetSystems.Services`
- Classes: `PlanetInteractService`

- **.Instance**: linhas 12

### Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Animations/PlayerAnimationController.cs
- Namespace: `_ImmersiveGames.Scripts.PlayerControllerSystem.Animations`
- Classes: `PlayerAnimationController`

- **EventBinding**: linhas 11, 12, 13, 25, 26, 27
- **FilteredEventBus**: linhas 62, 63, 64, 82, 87, 92

### Assets/_ImmersiveGames/Scripts/PlayerControllerSystem/Shooting/PlayerShootController.cs
- Namespace: `_ImmersiveGames.Scripts.PlayerControllerSystem.Shooting`
- Classes: `PlayerShootController`

- **.Instance**: linhas 198
- **ObjectPool**: linhas 51
- **PoolManager**: linhas 198, 201

### Assets/_ImmersiveGames/Scripts/ProjectilesSystems/BulletPoolable.cs
- Namespace: `_ImmersiveGames.Scripts.ProjectilesSystems`
- Classes: `BulletPoolable`

- **PooledObject**: linhas 12

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/ActorResourceCanvas.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Bind`
- Classes: `ActorResourceCanvas`

- **.Instance**: linhas 48, 177
- **ObjectPool**: linhas 32, 150

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/ActorResourceComponent.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Bind`
- Classes: `ActorResourceComponent`

- **.Instance**: linhas 35

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/DynamicCanvasBinder.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Bind`
- Classes: `DynamicCanvasBinder`

- **.Instance**: linhas 19

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Bind/SceneCanvasBinder.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Bind`
- Classes: `SceneCanvasBinder`

- **.Instance**: linhas 18

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Bridges/ResourceThresholdBridge.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems`
- Classes: `ResourceThresholdBridge`

- **EventBinding**: linhas 14, 34
- **FilteredEventBus**: linhas 35, 56

### Assets/_ImmersiveGames/Scripts/RuntimeAttributeSystems/Presentation/Bridges/WorldSpaceBillboard.cs
- Namespace: `_ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges`
- Classes: `WorldSpaceBillboard`

- **FindFirstObjectByType**: linhas 54

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/CanvasPipelineManager.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Services`
- Classes: `CanvasPipelineManager`

- **.Instance**: linhas 28
- **EventBinding**: linhas 19, 20, 39, 42
- **EventBus.Register**: linhas 40, 43
- **EventBus.Unregister**: linhas 53, 56

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/ResourceLinkService.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Services`
- Classes: `ResourceLinkService`

- **EventBinding**: linhas 25, 30
- **EventBus.Register**: linhas 31
- **EventBus.Unregister**: linhas 105

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Services/ResourceThresholdService.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Services`
- Classes: `ResourceThresholdService`

- **FilteredEventBus**: linhas 50

### Assets/_ImmersiveGames/Scripts/ResourceSystems/Test/EntityDebugUtility.cs
- Namespace: `_ImmersiveGames.Scripts.ResourceSystems.Test`
- Classes: `EntityDebugUtility`

- **.Instance**: linhas 27, 67, 600
- **EventBinding**: linhas 49, 50, 51, 74, 77, 80
- **EventBus.Register**: linhas 75, 78, 81
- **EventBus.Unregister**: linhas 89, 90, 91

### Assets/_ImmersiveGames/Scripts/SceneManagement/Editor/SceneFlowDebugTools.cs
- Namespace: `_ImmersiveGames.Scripts.SceneManagement.Editor`
- Classes: `SceneFlowDebugTools`

- **.Instance**: linhas 155, 175, 177, 202, 228

### Assets/_ImmersiveGames/Scripts/SkinSystems/Configurable/SkinConfigurable.cs
- Namespace: `_ImmersiveGames.Scripts.SkinSystems.Configurable`
- Classes: `SkinConfigurable`

- **EventBinding**: linhas 22, 23, 84, 85
- **FilteredEventBus**: linhas 87, 88, 95, 96

### Assets/_ImmersiveGames/Scripts/SkinSystems/Controllers/ActorSkinController.cs
- Namespace: `_ImmersiveGames.Scripts.SkinSystems`
- Classes: `ActorSkinController`

- **EventBinding**: linhas 30, 31, 127, 128
- **FilteredEventBus**: linhas 132, 133, 144, 149, 263, 278, 296

### Assets/_ImmersiveGames/Scripts/SkinSystems/Threshold/ResourceThresholdListener.cs
- Namespace: `_ImmersiveGames.Scripts.SkinSystems.Threshold`
- Classes: `ThresholdConfig`, `ResourceThresholdListener`

- **EventBinding**: linhas 34, 72
- **EventBus.Register**: linhas 76
- **EventBus.Unregister**: linhas 103
- **FilteredEventBus**: linhas 83, 85, 95, 98

### Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopStateMachine.cs
- Namespace: `_ImmersiveGames.NewScripts.Gameplay.GameLoop`
- Classes: `GameLoopStateMachine`

- **EventBinding**: linhas 16, 17, 18, 19, 20, 21, 104, 107, 110, 113, 116, 119
- **EventBus.Register**: linhas 105, 108, 111, 114, 117, 120
- **EventBus.Unregister**: linhas 132, 138, 144, 150, 156, 162
- **SingletonBase**: linhas 12

### Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopStates.cs
- Namespace: `_ImmersiveGames.NewScripts.Gameplay.GameLoop`
- Classes: `GameStateBase`, `MenuState`, `PlayingState`, `PausedState`, `GameOverState`, `VictoryState`

### Assets/_ImmersiveGames/Scripts/StateMachineSystems/StateDependentBehavior.cs
- Namespace: `_ImmersiveGames.Scripts.StateMachineSystems`
- Classes: `StateDependentBehavior`

- **.Instance**: linhas 28
- **EventBinding**: linhas 9, 12
- **EventBus.Register**: linhas 13
- **EventBus.Unregister**: linhas 18

### Assets/_ImmersiveGames/Scripts/StateMachineSystems/StateDependentService.cs
- Namespace: `_ImmersiveGames.Scripts.StateMachineSystems`
- Classes: `StateDependentService`

- **EventBinding**: linhas 11, 18
- **EventBus.Register**: linhas 19
- **EventBus.Unregister**: linhas 40

### Assets/_ImmersiveGames/Scripts/TimerSystem/GameTimer.cs
- Namespace: `_ImmersiveGames.Scripts.TimerSystem`
- Classes: `GameTimer`

- **.Instance**: linhas 52, 367
- **EventBinding**: linhas 29, 30, 31, 32, 33, 34, 72, 75, 78, 81, 89, 92
- **EventBus.Register**: linhas 73, 76, 79, 87, 90, 93
- **EventBus.Unregister**: linhas 100, 106, 112, 118, 124, 130
- **SingletonBase**: linhas 22

### Assets/_ImmersiveGames/Scripts/TimerSystem/TimerDisplay.cs
- Namespace: `_ImmersiveGames.Scripts.TimerSystem`
- Classes: `TimerDisplay`

- **.Instance**: linhas 183
- **EventBinding**: linhas 35, 36, 83, 89
- **EventBus.Register**: linhas 84, 90
- **EventBus.Unregister**: linhas 98, 104

### Assets/_ImmersiveGames/Scripts/UI/Compass/CompassHUD.cs
- Namespace: `_ImmersiveGames.Scripts.UI.Compass`
- Classes: `CompassHUD`

- **.Instance**: linhas 70, 80, 95

### Assets/_ImmersiveGames/Scripts/UI/Compass/CompassPlanetHighlightController.cs
- Namespace: `_ImmersiveGames.Scripts.UI.Compass`
- Classes: `CompassPlanetHighlightController`

- **EventBinding**: linhas 28, 44
- **EventBus.Register**: linhas 45
- **EventBus.Unregister**: linhas 52

### Assets/_ImmersiveGames/Scripts/UI/DirectionIndicatorManager.cs
- Namespace: `_ImmersiveGames.Scripts.UI`
- Classes: `DirectionIndicatorManager`

- **.Instance**: linhas 27, 48

### Assets/_ImmersiveGames/Scripts/UI/DirectionIndicatorObjectUI.cs
- Namespace: `_ImmersiveGames.Scripts.UI`
- Classes: `DirectionIndicatorObjectUI`

- **.Instance**: linhas 20

### Assets/_ImmersiveGames/Scripts/UI/GameLoop/GameLoopRequestButton.cs
- Namespace: `_ImmersiveGames.Scripts.UI.GameLoop`
- Classes: `GameLoopRequestButton`

- **.Instance**: linhas 30

### Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/Bind/BaseBindHandler.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.BusEventSystems`
- Classes: `BaseBindHandler`

- **EventBinding**: linhas 11, 22
- **EventBus.Register**: linhas 23
- **EventBus.Unregister**: linhas 29

### Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/Bind/EventBinding.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.BusEventSystems`
- Classes: `EventBinding`

- **EventBinding**: linhas 3

### Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/EventBus.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.BusEventSystems`
- Classes: `EventBus`

- **EventBinding**: linhas 10, 11

### Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/FilteredEventBus.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.BusEventSystems`
- Classes: `FilteredEventBus`

- **FilteredEventBus**: linhas 9

### Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/InjectableEventBus.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.BusEventSystems`
- Classes: `InjectableEventBus`

- **EventBinding**: linhas 8, 10, 16, 26, 27

### Assets/_ImmersiveGames/Scripts/Utils/BusEventSystems/Interfaces/IEvent.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.BusEventSystems`

- **EventBinding**: linhas 5, 6

### Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyBootstrapper.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.DependencySystems`
- Classes: `DependencyBootstrapper`

- **.Instance**: linhas 78, 81, 94, 143, 158

### Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyInjector.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.DependencySystems`
- Classes: `DependencyInjector`, `ServiceRegistryExtensions`, `InjectAttribute`

- **.Instance**: linhas 56, 164, 173, 182

### Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/DependencyManager.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.DependencySystems`
- Classes: `DependencyManager`

- **RegisterForObject**: linhas 46
- **RegisterForScene**: linhas 55
- **SingletonBase**: linhas 12

### Assets/_ImmersiveGames/Scripts/Utils/DependencySystems/IDependencyProvider.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.DependencySystems`

- **RegisterForObject**: linhas 12
- **RegisterForScene**: linhas 15

### Assets/_ImmersiveGames/Scripts/Utils/Diagnostics/Editor/Diagnostics.Editor.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.Diagnostics.Editor`
- Classes: `ProjectArchitectureAudit`, `AuditResults`, `FileFindings`, `X`

- **.Instance**: linhas 135, 219, 315
- **FilteredEventBus**: linhas 17, 144, 228, 285
- **ObjectPool**: linhas 19, 153, 237, 287
- **PoolManager**: linhas 19, 152, 236, 287
- **PooledObject**: linhas 19, 154, 238, 287
- **SingletonBase**: linhas 16, 134, 283
- **Time.timeScale**: linhas 15, 127, 211, 281

### Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/IPoolable.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.PoolSystems`

- **ObjectPool**: linhas 8

### Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/LifetimeManager.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.PoolSystems`
- Classes: `LifetimeManager`

- **PooledObject**: linhas 105, 115

### Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/ObjectPool.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.PoolSystems`
- Classes: `ObjectPool`

- **.Instance**: linhas 213
- **ObjectPool**: linhas 10, 29, 39, 120, 153, 166, 172, 183, 228, 236

### Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PoolData.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.PoolSystems`
- Classes: `PoolData`

- **ObjectPool**: linhas 51, 78

### Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PoolManager.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.PoolSystems`
- Classes: `PoolManager`

- **ObjectPool**: linhas 11, 22, 38, 55
- **PoolManager**: linhas 9, 16, 17, 26, 32, 43, 48, 59, 65, 80

### Assets/_ImmersiveGames/Scripts/Utils/PoolSystems/PooledObject.cs
- Namespace: `_ImmersiveGames.Scripts.Utils.PoolSystems`
- Classes: `PooledObject`

- **.Instance**: linhas 44, 58, 75, 91, 96
- **ObjectPool**: linhas 10, 16, 111
- **PooledObject**: linhas 7, 32

## Próximos passos sugeridos (para a Etapa 1+)

- Consolidar referências por domínio (ActorRegistry / PlayerDomain / EaterDomain).
- Reduzir dependências em `.Instance` e `Find*` nos consumidores de gameplay.
- Separar FlowState / Gate (token-based) / TimePolicy antes de implementar reset in-place.
