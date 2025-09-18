using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DisallowMultipleComponent]
    public class ResourceSystemTester : MonoBehaviour
    {
        [Header("🆔 Identificador do recurso para teste")]
        [Tooltip("UniqueId definido no ResourceConfigSo")]
        public string uniqueIdParaTestar;

        [Header("⚙️ Parâmetros de Teste")]
        public float testAmount = 10f;
        public bool resetToInitial = true;

        private ResourceSystem _resource;

        private void Awake()
        {
            EncontrarRecurso();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                EncontrarRecurso();
        }

        private void EncontrarRecurso()
        {
            if (string.IsNullOrEmpty(uniqueIdParaTestar))
            {
                DebugUtility.LogError<ResourceSystemTester>("❌ UniqueId para teste está vazio.");
                return;
            }

            var recursos = GetComponents<ResourceSystem>();
            _resource = recursos.FirstOrDefault(r => r.Config != null && r.Config.UniqueId == uniqueIdParaTestar);

            if (_resource == null)
            {
                DebugUtility.LogError<ResourceSystemTester>($"❌ Nenhum ResourceSystem com UniqueId '{uniqueIdParaTestar}' encontrado em '{gameObject.name}'.");
            }
            else
            {
                DebugUtility.LogVerbose<ResourceSystemTester>($"✅ Recurso '{_resource.Config.ResourceType}' com UniqueId '{uniqueIdParaTestar}' encontrado.");
            }
        }

        public void IncreaseTest()
        {
            if (_resource == null) return;
            _resource.Increase(testAmount);
            DebugUtility.LogVerbose<ResourceSystemTester>($"⏫ Aumentado {testAmount} em '{uniqueIdParaTestar}'");
        }

        public void DecreaseTest()
        {
            if (_resource == null) return;
            _resource.Decrease(testAmount);
            DebugUtility.LogVerbose<ResourceSystemTester>($"⏬ Diminuído {testAmount} em '{uniqueIdParaTestar}'");
        }

        public void ResetTest()
        {
            if (_resource == null) return;

            if (_resource is IResettable resettable)
            {
                resettable.Reset(resetToInitial);
                DebugUtility.LogVerbose<ResourceSystemTester>($"🔄 Reset via IResettable em '{uniqueIdParaTestar}'");
            }
            else
            {
                float valor = resetToInitial ? _resource.Config.InitialValue : _resource.Config.MaxValue;
                _resource.Load(new ResourceSystem.ResourceSaveData { currentValue = valor });
                DebugUtility.LogVerbose<ResourceSystemTester>($"🔄 Reset manual ({valor}) em '{uniqueIdParaTestar}'");
            }
        }
    }
}
