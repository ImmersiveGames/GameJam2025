using System;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    /// <summary>
    /// Responsável por controlar o recurso associado a um planeta
    /// e o estado de descoberta desse recurso.
    ///
    /// Este componente não decide QUANDO o recurso é descoberto,
    /// apenas guarda o estado e expõe uma API simples para outros
    /// sistemas (PlanetsMaster, UI, detecção, etc.).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Planet Systems/Planet Resource State")]
    public sealed class PlanetResourceState : MonoBehaviour
    {
        [Header("Configuração de Recurso do Planeta")]
        [Tooltip("Dados do recurso associado a este planeta (tipo, ícone, etc.).")]
        [SerializeField]
        private PlanetResourcesSo resourceDefinition;

        [Tooltip("Indica se o recurso deste planeta já foi descoberto no gameplay.")]
        [SerializeField]
        private bool isDiscovered;

        /// <summary>
        /// ScriptableObject que descreve o recurso do planeta.
        /// Pode ser nulo quando nenhum recurso foi atribuído.
        /// </summary>
        public PlanetResourcesSo ResourceDefinition => resourceDefinition;

        /// <summary>
        /// Indica se há um recurso válido atribuído a este planeta.
        /// </summary>
        public bool HasAssignedResource => resourceDefinition != null;

        /// <summary>
        /// Indica se o recurso deste planeta já foi descoberto.
        /// </summary>
        public bool IsDiscovered => isDiscovered;

        /// <summary>
        /// Tipo de recurso do planeta (enum).
        /// Lança exceção se não houver recurso atribuído.
        /// </summary>
        public PlanetResources ResourceType
        {
            get
            {
                if (resourceDefinition == null)
                {
                    throw new InvalidOperationException(
                        "[PlanetResourceState] Nenhum PlanetResourcesSo foi atribuído a este planeta. " +
                        "Use HasAssignedResource antes de acessar RuntimeAttributeType."
                    );
                }

                return resourceDefinition.ResourceType;
            }
        }

        /// <summary>
        /// Atribui um recurso a este planeta.
        /// Zera o estado de descoberta.
        /// </summary>
        public void AssignResource(PlanetResourcesSo newResourceDefinition)
        {
            resourceDefinition = newResourceDefinition;
            isDiscovered = false;
        }

        /// <summary>
        /// Marca o recurso como descoberto.
        /// Não faz nada se não houver recurso ou se já estiver descoberto.
        /// </summary>
        public void RevealResource()
        {
            if (!HasAssignedResource)
            {
                return;
            }

            if (isDiscovered)
            {
                return;
            }

            isDiscovered = true;
        }

        /// <summary>
        /// Reseta o estado de descoberta.
        /// Não faz nada se já estiver oculto.
        /// </summary>
        public void ResetDiscovery()
        {
            if (!isDiscovered)
            {
                return;
            }

            isDiscovered = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!HasAssignedResource && isDiscovered)
            {
                // Não faz sentido recurso "descoberto" sem dado configurado.
                isDiscovered = false;
            }
        }
#endif
    }
}
