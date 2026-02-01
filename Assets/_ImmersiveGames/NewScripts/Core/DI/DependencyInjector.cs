using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Core.DI
{
    public class DependencyInjector
    {
        private readonly ObjectServiceRegistry _objectRegistry;
        private readonly SceneServiceRegistry _sceneRegistry;
        private readonly GlobalServiceRegistry _globalRegistry;

        // Dedupe por frame (não depende de coroutine / MonoBehaviour).
        private readonly HashSet<object> _injectedObjectsThisFrame = new();
        private int _lastInjectionFrame = -1;

        public DependencyInjector(ObjectServiceRegistry objectRegistry, SceneServiceRegistry sceneRegistry, GlobalServiceRegistry globalRegistry)
        {
            _objectRegistry = objectRegistry;
            _sceneRegistry = sceneRegistry;
            _globalRegistry = globalRegistry;
        }

        public void InjectDependencies(object target, string objectId = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!CanInjectThisFrame(target))
            {
                return;
            }

            List<FieldInfo> injectableFields = GetInjectableFields(target.GetType());
            InjectFields(target, injectableFields, objectId);
        }

        private bool CanInjectThisFrame(object target)
        {
            int frame = Time.frameCount;
            if (frame != _lastInjectionFrame)
            {
                _lastInjectionFrame = frame;
                _injectedObjectsThisFrame.Clear();
            }

            if (_injectedObjectsThisFrame.Add(target))
            {
                return true;
            }

            DebugUtility.LogVerbose(typeof(DependencyInjector),
                $"Injeção ignorada para {target.GetType().Name}: já injetado neste frame.");
            return false;
        }

        private static List<FieldInfo> GetInjectableFields(Type type)
        {
            var fields = new List<FieldInfo>();
            var currentType = type;

            while (currentType != null && currentType != typeof(object))
            {
                FieldInfo[] currentFields = currentType.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                fields.AddRange(currentFields.Where(field => field.IsDefined(typeof(InjectAttribute), false)));

                currentType = currentType.BaseType;
            }

            return fields;
        }

        private void InjectFields(object target, List<FieldInfo> fields, string objectId)
        {
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                object service = ResolveService(fieldType, objectId, target);

                if (service != null)
                {
                    field.SetValue(target, service);
                    LogInjectionSuccess(fieldType, target.GetType(), service.GetType());
                }
                else
                {
                    LogInjectionError(fieldType, target.GetType());
                }
            }
        }

        private object ResolveService(Type serviceType, string objectId, object target)
        {
            // Ordem: Objeto → Cena (da instância) → Global
            return ResolveFromObjectScope(serviceType, objectId, target.GetType())
                   ?? ResolveFromSceneScope(serviceType, target)
                   ?? ResolveFromGlobalScope(serviceType, target.GetType());
        }

        private object ResolveFromObjectScope(Type serviceType, string objectId, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                return null;
            }

            object service = _objectRegistry.TryGet(serviceType, objectId);
            if (service != null)
            {
                DebugUtility.LogVerbose(typeof(DependencyInjector),
                    $"Injetando {serviceType.Name} do escopo objeto '{objectId}' para {targetType.Name}.");
            }
            return service;
        }

        private object ResolveFromSceneScope(Type serviceType, object target)
        {
            string sceneName = GetTargetSceneName(target);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return null;
            }

            object service = _sceneRegistry.TryGet(serviceType, sceneName);
            if (service != null)
            {
                DebugUtility.LogVerbose(typeof(DependencyInjector),
                    $"Injetando {serviceType.Name} do escopo cena '{sceneName}' para {target.GetType().Name}.");
            }
            return service;
        }

        private static string GetTargetSceneName(object target)
        {
            // Regra correta para aditivo:
            // - Se for MonoBehaviour: usar a cena do próprio GameObject.
            // - Senão: fallback para a active scene.
            if (target is MonoBehaviour mb && mb != null)
            {
                var s = mb.gameObject.scene;
                if (s.IsValid() && s.isLoaded)
                {
                    return s.name;
                }

                // Se estiver em DontDestroyOnLoad / inválido, cai para active scene.
            }

            return SceneManager.GetActiveScene().name;
        }

        private object ResolveFromGlobalScope(Type serviceType, Type targetType)
        {
            object service = _globalRegistry.TryGet(serviceType);

            if (service != null)
            {
                DebugUtility.LogVerbose(typeof(DependencyInjector),
                    $"Injetando {serviceType.Name} do escopo global para {targetType.Name}.");
            }
            return service;
        }

        private static void LogInjectionSuccess(Type fieldType, Type targetType, Type serviceType)
        {
            DebugUtility.LogVerbose(typeof(DependencyInjector),
                $"Injeção bem-sucedida: {fieldType.Name} -> {targetType.Name} (implementação: {serviceType.Name})");
        }

        private static void LogInjectionError(Type fieldType, Type targetType)
        {
            DebugUtility.LogError(typeof(DependencyInjector),
                $"Falha ao injetar {fieldType.Name} para {targetType.Name}: serviço não encontrado. " +
                "Certifique-se de registrar o serviço no DependencyManager no escopo apropriado (objeto, cena ou global).");
        }
    }

    // Extensões para simplificar o TryGet genérico via Type
    public static class ServiceRegistryExtensions
    {
        public static object TryGet(this ObjectServiceRegistry registry, Type serviceType, string objectId)
        {
            var method = typeof(ObjectServiceRegistry).GetMethod("TryGet", BindingFlags.Instance | BindingFlags.Public);
            var genericMethod = method?.MakeGenericMethod(serviceType);
            object[] parameters = { objectId, null };
            bool success = genericMethod != null && (bool)genericMethod.Invoke(registry, parameters);
            return success ? parameters[1] : null;
        }

        public static object TryGet(this SceneServiceRegistry registry, Type serviceType, string sceneName)
        {
            var method = typeof(SceneServiceRegistry).GetMethod("TryGet", BindingFlags.Instance | BindingFlags.Public);
            var genericMethod = method?.MakeGenericMethod(serviceType);
            object[] parameters = { sceneName, null };
            bool success = genericMethod != null && (bool)genericMethod.Invoke(registry, parameters);
            return success ? parameters[1] : null;
        }

        public static object TryGet(this GlobalServiceRegistry registry, Type serviceType)
        {
            var method = typeof(GlobalServiceRegistry).GetMethod("TryGet", BindingFlags.Instance | BindingFlags.Public);
            var genericMethod = method?.MakeGenericMethod(serviceType);
            object[] parameters = { null, null };
            bool success = genericMethod != null && (bool)genericMethod.Invoke(registry, parameters);
            return success ? parameters[1] : null;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute { }
}
