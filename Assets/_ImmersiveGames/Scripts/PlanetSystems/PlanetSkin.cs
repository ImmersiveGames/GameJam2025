using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using UnityUtils;
using Random = UnityEngine.Random;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetSkin : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] planetsParts;
        [SerializeField]
        private GameObject planetRing;
        [SerializeField]
        private Material[] planetColorMaterials;
        
        private MeshRenderer[] _planetMaterials;
        private PlanetsMaster _planetMaster; // Referência ao PlanetsMaster associado
        
        private EventBinding<PlanetCreatedEvent> _planetCreateBinding; // Binding para evento de desmarcação

        private void Awake()
        {
            _planetMaterials = GetComponentsInChildren<MeshRenderer>();
            _planetMaster = GetComponentInParent<PlanetsMaster>();
        }

        private void OnEnable()
        {
            _planetCreateBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreateBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetCreatedEvent>.Unregister(_planetCreateBinding);
        }
        
        private void OnPlanetCreated(PlanetCreatedEvent obj)
        {
            if(obj.PlanetsMaster != _planetMaster)
                return;
            Debug.Log($"PlanetSkin: OnPlanetCreated called for {obj.PlanetsMaster.Name}");
            ChangeMaterialColor();
            planetRing.SetActive(Random.value > 0.8f);
        }

        private void ChangeMaterialColor()
        {
            if (_planetMaterials == null || _planetMaterials.Length == 0)
                return;

            // Referência ao primeiro MeshRenderer válido
            var referenceRenderer = _planetMaterials[0];
            if (referenceRenderer == null)
                return;

            // Usa o número de materiais do primeiro renderer como padrão
            Material[] referenceMaterials = referenceRenderer.sharedMaterials;
            int materialCount = referenceMaterials.Length;

            if (materialCount == 0)
                return;

            // Gera a sequência de materiais aleatórios uma vez
            var newMaterials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                newMaterials[i] = planetColorMaterials.Random<Material>();
            }

            // Aplica nos renderizadores compatíveis
            foreach (var meshRenderer in _planetMaterials)
            {
                if (meshRenderer != null && meshRenderer.sharedMaterials.Length == materialCount)
                {
                    meshRenderer.sharedMaterials = newMaterials;
                }
#if UNITY_EDITOR
                else
                {
                    Debug.LogWarning($"[PlanetSkin] Renderer \"{meshRenderer?.name}\" skipped: material count mismatch.");
                }
#endif
            }
        }


    }
}