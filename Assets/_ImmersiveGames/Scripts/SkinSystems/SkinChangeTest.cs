using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Testa troca de skins em runtime, disparada no spawn ou por input, usando SkinConfigData para trocas individuais por ModelType.
    /// </summary>
    [DebugLevel(DebugLevel.Logs)]
    public class SkinChangeTest : MonoBehaviour
    {
        [SerializeField] private SkinCollectionData alternateSkinCollection; // Para troca coletiva
        [SerializeField] private SkinConfigData individualSkinData; // Para troca individual (contém ModelType e prefabs)

        private SkinController _skinController;
        private PooledObject _pooledObject;

        private void Awake()
        {
            // Obtém o SkinController
            _skinController = GetComponent<SkinController>();
            if (_skinController == null)
            {
                DebugUtility.LogError<SkinChangeTest>($"Componente SkinController não encontrado em '{name}'.", this);
                enabled = false;
                return;
            }

            // Verifica se é um objeto poolado
            _pooledObject = GetComponent<PooledObject>();
            if (_pooledObject == null) return;
            var pool = _pooledObject.GetPool;
            if (pool != null)
            {
                pool.OnObjectActivated.AddListener(OnPooledObjectActivated);
                DebugUtility.LogVerbose<SkinChangeTest>($"Registrado OnPooledObjectActivated para '{name}'.", "cyan", this);
            }
            else
            {
                DebugUtility.LogWarning<SkinChangeTest>($"ObjectPool é nulo em PooledObject para '{name}'.", this);
            }
        }

        private void Update()
        {
            // Troca coletiva com a tecla C
            if (Input.GetKeyDown(KeyCode.C) && alternateSkinCollection != null)
            {
                _skinController.ApplySkinCollection(alternateSkinCollection, _pooledObject?.Spawner);
                DebugUtility.LogVerbose<SkinChangeTest>($"Aplicada troca coletiva de skin para '{alternateSkinCollection.CollectionName}' em '{name}'.", "green", this);
            }

            // Troca individual com a tecla M
            if (Input.GetKeyDown(KeyCode.M) && individualSkinData != null)
            {
                _skinController.ApplySkin(individualSkinData, _pooledObject?.Spawner);
                DebugUtility.LogVerbose<SkinChangeTest>($"Aplicada troca individual de skin para ModelType '{individualSkinData.ModelType}' em '{name}'.", "green", this);
            }
        }

        private void OnPooledObjectActivated(IPoolable poolable)
        {
            if (poolable.GetGameObject() == gameObject)
            {
                DebugUtility.LogVerbose<SkinChangeTest>($"Recebido OnPooledObjectActivated para '{name}'.", "green", this);

                // Troca coletiva no spawn (50% de chance)
                if (alternateSkinCollection != null && Random.value > 0.5f)
                {
                    _skinController.ApplySkinCollection(alternateSkinCollection, _pooledObject?.Spawner);
                    DebugUtility.LogVerbose<SkinChangeTest>($"Aplicada troca coletiva de skin no spawn para '{alternateSkinCollection.CollectionName}' em '{name}'.", "green", this);
                }
                // Troca individual no spawn
                else if (individualSkinData != null)
                {
                    _skinController.ApplySkin(individualSkinData, _pooledObject?.Spawner);
                    DebugUtility.LogVerbose<SkinChangeTest>($"Aplicada troca individual de skin no spawn para ModelType '{individualSkinData.ModelType}' em '{name}'.", "green", this);
                }
            }
        }

        private void OnDestroy()
        {
            if (_pooledObject?.GetPool != null)
            {
                _pooledObject.GetPool.OnObjectActivated.RemoveListener(OnPooledObjectActivated);
            }
            DebugUtility.LogVerbose<SkinChangeTest>($"Destruído SkinChangeTest em '{name}'.", "blue", this);
        }
    }
}