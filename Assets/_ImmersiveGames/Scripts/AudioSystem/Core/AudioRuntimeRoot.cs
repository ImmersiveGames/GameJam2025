using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Core
{
    /// <summary>
    /// Fornece um transform raiz global para objetos de áudio criados em runtime.
    /// </summary>
    public static class AudioRuntimeRoot
    {
        private const string RootName = "AudioRuntimeRoot";
        private static Transform _root;

        public static Transform Root
        {
            get
            {
                if (_root == null || !_root)
                {
                    InitializeRoot();
                }

                return _root;
            }
        }

        private static void InitializeRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                _root = existing.transform;
                Object.DontDestroyOnLoad(existing);
                return;
            }

            var rootObject = new GameObject(RootName);
            Object.DontDestroyOnLoad(rootObject);
            _root = rootObject.transform;
        }
    }
}
