using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [CreateAssetMenu(fileName = "EaterDesireConfig", menuName = "ImmersiveGames/EaterDesireConfig")]
    public class EaterDesireConfigSo : ScriptableObject
    {
        [SerializeField, Tooltip("Intervalo normal para mudança de vontade (segundos)")]
        private float desireChangeInterval = 10f;
        [SerializeField, Tooltip("Intervalo reduzido quando recurso desejado não está disponível (segundos)")]
        private float noResourceDesireChangeInterval = 2f;
        [SerializeField, Tooltip("Número máximo de recursos recentes a evitar repetição")]
        private int maxRecentDesires = 3;
        [SerializeField, Tooltip("Fome restaurada ao consumir recurso desejado")]
        private float desiredHungerRestored = 50f;
        [SerializeField, Tooltip("Fome restaurada ao consumir recurso indesejado")]
        private float nonDesiredHungerRestored = 25f;
        [SerializeField, Tooltip("HP restaurado ao consumir recurso desejado")]
        private float desiredHealthRestored = 30f;

        public float DesireChangeInterval => desireChangeInterval;
        public float NoResourceDesireChangeInterval => noResourceDesireChangeInterval;
        public int MaxRecentDesires => maxRecentDesires;
        public float DesiredHungerRestored => desiredHungerRestored;
        public float NonDesiredHungerRestored => nonDesiredHungerRestored;
        public float DesiredHealthRestored => desiredHealthRestored;
    }
}