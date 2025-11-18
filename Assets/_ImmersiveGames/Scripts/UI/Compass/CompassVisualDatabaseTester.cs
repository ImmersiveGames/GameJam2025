using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Componente auxiliar para validar em Play Mode se os assets de configuração
    /// da bússola estão acessíveis e retornando dados coerentes.
    /// </summary>
    public class CompassVisualDatabaseTester : MonoBehaviour
    {
        [Header("Assets de Teste")]
        [Tooltip("Asset de CompassSettings a ser validado em runtime.")]
        public ScriptableObject compassSettingsAsset;

        [Tooltip("Asset de CompassVisualDatabase a ser validado em runtime.")]
        public ScriptableObject visualDatabaseAsset;

        [Header("Parâmetros de Teste")]
        [Tooltip("Tipo de alvo usado para consultar a configuração visual ao pressionar a tecla V.")]
        public CompassTargetType testTargetType = CompassTargetType.Enemy;

        private void Awake()
        {
            CompassSettings settings = compassSettingsAsset as CompassSettings;
            if (settings != null)
            {
                Debug.Log($"[CompassTester] Settings -> halfAngle: {settings.compassHalfAngleDegrees}, maxDistance: {settings.maxDistance}, minDistance: {settings.minDistance}, clampIconsAtEdges: {settings.clampIconsAtEdges}");
            }
            else
            {
                Debug.LogWarning("[CompassTester] Nenhum CompassSettings válido atribuído.");
            }

            CompassVisualDatabase database = visualDatabaseAsset as CompassVisualDatabase;
            if (database != null)
            {
                Debug.Log($"[CompassTester] VisualDatabase -> configs: {database.ConfigsCount}");
            }
            else
            {
                Debug.LogWarning("[CompassTester] Nenhum CompassVisualDatabase válido atribuído.");
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.V))
            {
                return;
            }

            CompassVisualDatabase database = visualDatabaseAsset as CompassVisualDatabase;
            if (database == null)
            {
                Debug.LogWarning("[CompassTester] VisualDatabase inválida, atribua o asset no inspector.");
                return;
            }

            CompassTargetVisualConfig config = database.GetConfig(testTargetType);
            if (config == null)
            {
                Debug.Log($"[CompassTester] Nenhuma config encontrada para {testTargetType}.");
                return;
            }

            Debug.Log(
                $"[CompassTester] Config para {testTargetType} -> sprite: {config.iconSprite}, cor: {config.baseColor}, tamanho: {config.baseSize}, " +
                $"dynamicMode: {config.dynamicMode}, hideUntilDiscovered: {config.hideUntilDiscovered}, undiscoveredIcon: {config.undiscoveredPlanetIcon}");
        }
    }
}
