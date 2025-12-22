// Arquivo: IDependencyProvider.cs
using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Infrastructure.DI
{
    public interface IDependencyProvider
    {
        void RegisterGlobal<T>(T service, bool allowOverride = false) where T : class;
        bool TryGetGlobal<T>(out T service) where T : class;

        void RegisterForObject<T>(string objectId, T service, bool allowOverride = false) where T : class;
        bool TryGetForObject<T>(string objectId, out T service) where T : class;

        void RegisterForScene<T>(string sceneName, T service, bool allowOverride = false) where T : class;
        bool TryGetForScene<T>(string sceneName, out T service) where T : class;

        void GetAllForScene<T>(string sceneName, List<T> services) where T : class;

        bool TryGet<T>(out T service, string objectId = null) where T : class;
        void GetAll<T>(List<T> services) where T : class;

        void InjectDependencies(object target, string objectId = null);

        void ClearSceneServices(string sceneName);
        void ClearAllSceneServices();
        void ClearObjectServices(string objectId);
        void ClearAllObjectServices();
        void ClearGlobalServices();

        List<Type> ListServicesForObject(string objectId);
        List<Type> ListServicesForScene(string sceneName);
        List<Type> ListGlobalServices();
    }
}
