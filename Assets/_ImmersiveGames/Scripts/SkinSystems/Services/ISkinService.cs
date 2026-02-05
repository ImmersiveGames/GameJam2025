using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Services
{
    /// <summary>
    /// Contrato para serviços responsáveis por gerenciar a aplicação de skins.
    /// Permite injetar implementações alternativas para testes ou variações de comportamento.
    /// </summary>
    public interface ISkinService
    {
        void Initialize(SkinCollectionData collection, Transform parent, IActor owner);
        IReadOnlyDictionary<ModelType, IReadOnlyList<GameObject>> ApplyCollection(SkinCollectionData collection, IActor owner);
        IReadOnlyList<GameObject> ApplyConfig(ISkinConfig config, IActor owner);
        IReadOnlyList<GameObject> GetInstancesOfType(ModelType type);
        bool HasInstancesOfType(ModelType type);
        Transform GetContainer(ModelType type);
    }
}
