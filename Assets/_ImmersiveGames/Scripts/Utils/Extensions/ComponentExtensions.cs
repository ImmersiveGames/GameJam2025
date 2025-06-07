using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.Extensions
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Tenta obter um componente do tipo T no GameObject atual ou em seus pais.
        /// </summary>
        /// <typeparam name="T">Tipo do componente a buscar.</typeparam>
        /// <param name="component">Componente inicial para a busca.</param>
        /// <param name="result">O componente encontrado, ou null se não encontrado.</param>
        /// <returns>True se o componente for encontrado, false caso contrário.</returns>
        public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : Component
        {
            result = null;
            if (component == null) return false;

            // Tenta no GameObject atual
            if (component.TryGetComponent<T>(out result))
            {
                return true;
            }

            // Busca nos pais
            Transform current = component.transform.parent;
            while (current != null)
            {
                if (current.TryGetComponent<T>(out result))
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Tenta obter um componente do tipo T no GameObject atual ou em seus filhos.
        /// </summary>
        /// <typeparam name="T">Tipo do componente a buscar.</typeparam>
        /// <param name="component">Componente inicial para a busca.</param>
        /// <param name="result">O componente encontrado, ou null se não encontrado.</param>
        /// <param name="includeInactive">Se deve incluir GameObjects inativos na busca.</param>
        /// <returns>True se o componente for encontrado, false caso contrário.</returns>
        public static bool TryGetComponentInChildren<T>(this Component component, out T result, bool includeInactive = false) where T : Component
        {
            result = null;
            if (component == null) return false;

            // Tenta no GameObject atual
            if (component.TryGetComponent<T>(out result))
            {
                return true;
            }

            // Busca nos filhos
            foreach (Transform child in component.transform)
            {
                if (!includeInactive && !child.gameObject.activeInHierarchy) continue;

                if (child.TryGetComponent<T>(out result))
                {
                    return true;
                }

                // Busca recursivamente nos filhos dos filhos
                if (child.TryGetComponentInChildren<T>(out result, includeInactive))
                {
                    return true;
                }
            }

            return false;
        }
    }
}