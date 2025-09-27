using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.Utils.DependencySystems
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
            {
                DebugUtility.LogError(typeof(DependencyInjector), "InjectDependencies: target é nulo.");
                throw new ArgumentNullException(nameof(target));
            }

            if (!_injectedObjectsThisFrame.Add(target))
            {
                DebugUtility.LogVerbose(typeof(DependencyInjector), $"Injeção ignorada para {target.GetType().Name}: já injetado neste frame.");
                return;
            }

            var type = target.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(InjectAttribute), false))
                    continue;

                var fieldType = field.FieldType;
                DebugUtility.LogVerbose(typeof(DependencyInjector), $"Procurando {fieldType.Name} para {type.Name} (objectId: {objectId ?? "global"})", "yellow");

                object service = null;
                bool serviceFound = false;

                var tryGetMethod = typeof(ServiceRegistry).GetMethod("TryGet", BindingFlags.Instance | BindingFlags.Public);
                if (tryGetMethod != null)
                {
                    var genericTryGet = tryGetMethod.MakeGenericMethod(fieldType);

                    if (objectId != null)
                    {
                        var parameters = new object[] { objectId, null };
                        serviceFound = (bool)genericTryGet.Invoke(_objectRegistry, parameters);
                        if (serviceFound)
                        {
                            service = parameters[1];
                            DebugUtility.LogVerbose(typeof(DependencyInjector), $"Injetando {fieldType.Name} do escopo objeto {objectId} para {type.Name}.", "green");
                        }
                    }

                    if (!serviceFound)
                    {
                        string activeScene = SceneManager.GetActiveScene().name;
                        var parameters = new object[] { activeScene, null };
                        serviceFound = (bool)genericTryGet.Invoke(_sceneRegistry, parameters);
                        if (serviceFound)
                        {
                            service = parameters[1];
                            DebugUtility.LogVerbose(typeof(DependencyInjector), $"Injetando {fieldType.Name} do escopo cena {activeScene} para {type.Name}.", "cyan");
                        }
                    }

                    if (!serviceFound)
                    {
                        var parameters = new object[] { null, null };
                        serviceFound = (bool)genericTryGet.Invoke(_globalRegistry, parameters);
                        if (serviceFound)
                        {
                            service = parameters[1];
                            DebugUtility.LogVerbose(typeof(DependencyInjector), $"Injetando {fieldType.Name} do escopo global para {type.Name}.", "cyan");
                        }
                    }
                }

                if (service != null)
                {
                    field.SetValue(target, service);
                }
                else
                {
                    DebugUtility.LogError(typeof(DependencyInjector), 
                        $"Falha ao injetar {fieldType.Name} para {type.Name}: serviço não encontrado. " +
                        "Certifique-se de registrar o serviço no DependencyManager no escopo apropriado (objeto, cena ou global).");
                }
            }

            if (target is MonoBehaviour mb)
            {
                mb.StartCoroutine(ClearInjectedObjectsNextFrame());
            }
        }

        private System.Collections.IEnumerator ClearInjectedObjectsNextFrame()
        {
            yield return null;
            _injectedObjectsThisFrame.Clear();
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute { }
}