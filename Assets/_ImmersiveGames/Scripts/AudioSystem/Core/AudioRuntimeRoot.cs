using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Core
{
    /// <summary>
    /// Fornece um transform raiz global para objetos de áudio criados em runtime
    /// e helpers para organizar subgrupos lógicos de áudio.
    ///
    /// Exemplo de uso:
    /// - Pools de SoundEmitter podem ser filhos de AudioRuntimeRoot.Root.
    /// - Grupos por skin, por time, por tipo de som podem ser criados via GetOrCreateGroup.
    /// </summary>
    public static class AudioRuntimeRoot
    {
        private const string RootName = "AudioRuntimeRoot";
        private static Transform _root;

        /// <summary>
        /// Transform raiz global para todos os objetos de áudio em runtime.
        /// É marcado como DontDestroyOnLoad.
        /// </summary>
        public static Transform Root
        {
            get
            {
                if (_root == null || !_root)
                {
                    CreateRootIfNeeded();
                }

                return _root;
            }
        }

        /// <summary>
        /// Retorna (ou cria) um subgrupo de áudio com o nome especificado,
        /// como filho do Root. Útil para agrupar sons por skin, por time,
        /// por categoria, etc.
        /// </summary>
        /// <param name="groupName">Nome do grupo (ex.: "Skin_PlayerRed").</param>
        public static Transform GetOrCreateGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return Root;
            }

            var root = Root;
            var child = FindChildByName(root, groupName);
            if (child != null)
            {
                return child;
            }

            var go = new GameObject(groupName);
            go.transform.SetParent(root, false);
            return go.transform;
        }

        private static void CreateRootIfNeeded()
        {
            if (_root != null && _root)
                return;

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

        private static Transform FindChildByName(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
