using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Infrastructure.DI
{
    
    public class DependencyInjector
    {
        private readonly ObjectServiceRegistry _objectRegistry;
        private readonly SceneServiceRegistry _sceneRegistry;
        private readonly GlobalServiceRegistry _globalRegistry;
        private readonly HashSet<object> _injectedObjectsThisFrame = new();

        public DependencyInjector(ObjectServiceRegistry objectRegistry, SceneServiceRegistry sceneRegistry, GlobalServiceRegistry globalRegistry)
        {
            _objectRegistry = objectRegistry;
            _sceneRegistry = sceneRegistry;
            _globalRegistry = globalRegistry;
        }

        public void InjectDependencies(object target, string objectId = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!CanInject(target))
                return;

            List<FieldInfo> injectableFields = GetInjectableFields(target.GetType());
            InjectFields(target, injectableFields, objectId);

            ScheduleClearInjectedObjects(target);
        }

        private bool CanInject(object target)
        {
            if (_injectedObjectsThisFrame.Add(target)) return true;
            DebugUtility.LogVerbose(typeof(DependencyInjector), 
                $"Injeção ignorada para {target.GetType().Name}: já injetado neste frame.");
            return false;
        }

        private List<FieldInfo> GetInjectableFields(Type type)
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
                object service = ResolveService(fieldType, objectId, target.GetType());

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

        private object ResolveService(Type serviceType, string objectId, Type targetType)
        {
            // Tenta resolver na ordem: Objeto → Cena → Global
            return ResolveFromObjectScope(serviceType, objectId, targetType) 
                ?? ResolveFromSceneScope(serviceType, targetType) 
                ?? ResolveFromGlobalScope(serviceType, targetType);
        }

        private object ResolveFromObjectScope(Type serviceType, string objectId, Type targetType)
        {
            if (objectId == null) return null;

            object service = _objectRegistry.TryGet(serviceType, objectId);
            if (service != null)
            {
                DebugUtility.LogVerbose(typeof(DependencyInjector),
                    $"Injetando {serviceType.Name} do escopo objeto {objectId} para {targetType.Name}.");
            }
            return service;
        }

        private object ResolveFromSceneScope(Type serviceType, Type targetType)
        {
            string activeScene = SceneManager.GetActiveScene().name;
            object service = _sceneRegistry.TryGet(serviceType, activeScene);
            
            if (service != null)
            {
                DebugUtility.LogVerbose(typeof(DependencyInjector),
                    $"Injetando {serviceType.Name} do escopo cena {activeScene} para {targetType.Name}.");
            }
            return service;
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

        private void LogInjectionSuccess(Type fieldType, Type targetType, Type serviceType)
        {
            DebugUtility.LogVerbose(typeof(DependencyInjector),
                $"Injeção bem-sucedida: {fieldType.Name} -> {targetType.Name} (implementação: {serviceType.Name})");
        }

        private void LogInjectionError(Type fieldType, Type targetType)
        {
            DebugUtility.LogError(typeof(DependencyInjector), 
                $"Falha ao injetar {fieldType.Name} para {targetType.Name}: serviço não encontrado. " +
                "Certifique-se de registrar o serviço no DependencyManager no escopo apropriado (objeto, cena ou global).");
        }

        private void ScheduleClearInjectedObjects(object target)
        {
            if (target is MonoBehaviour mb)
            {
                mb.StartCoroutine(ClearInjectedObjectsNextFrame());
            }
        }

        private IEnumerator ClearInjectedObjectsNextFrame()
        {
            yield return null;
            _injectedObjectsThisFrame.Clear();
        }
    }

    // Extensões para simplificar o TryGet genérico
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
