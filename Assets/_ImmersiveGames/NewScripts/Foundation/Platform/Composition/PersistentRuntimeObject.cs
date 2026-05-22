using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    [DisallowMultipleComponent]
    public sealed class PersistentRuntimeObject : MonoBehaviour
    {
        private static readonly Dictionary<string, PersistentRuntimeObject> Registry = new(StringComparer.Ordinal);

        [SerializeField] private string identityKey = string.Empty;
        private bool _registered;

        private void Awake()
        {
            if (GetComponent("SceneBootstrapper") != null || GetComponent("SceneScopeCompositionRoot") != null)
            {
                string message =
                    $"[FATAL][Config][PersistentRuntimeObject] uso proibido em root scene-scoped. gameObject='{name}'.";
                DebugUtility.LogError<PersistentRuntimeObject>(message);
                throw new InvalidOperationException(message);
            }

            string key = Normalize(identityKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                string message = $"[FATAL][Config][PersistentRuntimeObject] identityKey obrigatorio ausente. gameObject='{name}'.";
                DebugUtility.LogError<PersistentRuntimeObject>(message);
                throw new InvalidOperationException(message);
            }

            if (Registry.TryGetValue(key, out PersistentRuntimeObject existing) &&
                existing != null &&
                !ReferenceEquals(existing, this))
            {
                string message =
                    $"[FATAL][Config][PersistentRuntimeObject] duplicate identityKey detectado. key='{key}' existing='{existing.gameObject.name}' duplicate='{name}'.";
                DebugUtility.LogError<PersistentRuntimeObject>(message);
                throw new InvalidOperationException(message);
            }

            Registry[key] = this;
            _registered = true;
            identityKey = key;

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (!_registered)
            {
                return;
            }

            string key = Normalize(identityKey);
            if (Registry.TryGetValue(key, out PersistentRuntimeObject existing) && ReferenceEquals(existing, this))
            {
                Registry.Remove(key);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
