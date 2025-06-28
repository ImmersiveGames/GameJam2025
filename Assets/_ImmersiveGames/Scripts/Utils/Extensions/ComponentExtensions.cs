using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.Extensions
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Tenta obter um componente do tipo T no Actor atual ou em seus pais.
        /// </summary>
        /// <typeparam name="T">Tipo do componente a buscar.</typeparam>
        /// <param name="component">Componente inicial para a busca.</param>
        /// <param name="result">O componente encontrado, ou null se não encontrado.</param>
        /// <returns>True se o componente for encontrado, false caso contrário.</returns>
        public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : Component
        {
            result = null;
            if (component == null) return false;

            // Tenta no Actor atual
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
        /// Tenta obter um componente do tipo T no Actor atual ou em seus filhos.
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

            // Tenta no Actor atual
            if (component.TryGetComponent<T>(out result))return true;

            // Busca nos filhos
            foreach (Transform child in component.transform)
            {
                if (!includeInactive && !child.gameObject.activeInHierarchy) continue;

                if (child.TryGetComponent<T>(out result))return true;

                // Busca recursivamente nos filhos dos filhos
                if (child.TryGetComponentInChildren<T>(out result, includeInactive))
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Tenta obter um componente no filho de um objeto. Se não encontrar, cria um objeto filho e adiciona o componente nesse filho.
        /// </summary>
        /// <typeparam name="T">Tipo do componente</typeparam>
        /// <param name="parent">O objeto pai</param>
        /// <param name="childName">Nome do objeto filho (se precisar criar)</param>
        /// <returns>O componente encontrado ou criado</returns>
        public static T GetOrCreateComponentInChild<T>(this Component parent, string childName = null) where T : Component
        {
            // Tenta encontrar o componente nos filhos
            T component = parent.GetComponentInChildren<T>();

            // Se o componente existir, retorna ele
            if (component != null)
            {
                return component;
            }

            // Se o nome do filho não foi fornecido, usa o nome do tipo
            if (string.IsNullOrEmpty(childName))
            {
                childName = typeof(T).Name;
            }

            // Cria um novo Actor como filho
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent.transform);
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;

            // Adiciona o componente ao objeto filho e retorna
            return childObject.AddComponent<T>();
        }

        /// <summary>
        /// Tenta obter um componente no filho de um objeto. Se não encontrar, cria um objeto filho e adiciona o componente nesse filho.
        /// </summary>
        /// <typeparam name="T">Tipo do componente</typeparam>
        /// <param name="parentGameObject">O objeto pai</param>
        /// <param name="childName">Nome do objeto filho (se precisar criar)</param>
        /// <returns>O componente encontrado ou criado</returns>
        public static T GetOrCreateComponentInChild<T>(this GameObject parentGameObject, string childName = null) where T : Component
        {
            // Tenta encontrar o componente nos filhos
            T component = parentGameObject.GetComponentInChildren<T>();

            // Se o componente existir, retorna ele
            if (component != null)
            {
                return component;
            }

            // Se o nome do filho não foi fornecido, usa o nome do tipo
            if (string.IsNullOrEmpty(childName))
            {
                childName = typeof(T).Name;
            }

            // Cria um novo Actor como filho
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parentGameObject.transform);
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;

            // Adiciona o componente ao objeto filho e retorna
            return childObject.AddComponent<T>();
        }
    }
}