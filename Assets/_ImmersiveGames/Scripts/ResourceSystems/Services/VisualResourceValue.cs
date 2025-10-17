using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Representa um valor de recurso que também expõe um ícone visual.
    /// </summary>
    public interface IResourceVisualValue : IResourceValue
    {
        Sprite GetIcon();
        void SetIcon(Sprite icon);
    }

    /// <summary>
    /// Implementação básica que encapsula um valor numérico e um ícone associado.
    /// </summary>
    public class VisualResourceValue : IResourceVisualValue
    {
        private readonly BasicResourceValue _value;
        private Sprite _icon;

        public VisualResourceValue(IResourceValue template = null, Sprite icon = null)
        {
            if (template != null)
            {
                _value = new BasicResourceValue(template.GetCurrentValue(), template.GetMaxValue());
            }
            else
            {
                _value = new BasicResourceValue(1f, 1f);
            }

            _icon = icon;
        }

        public Sprite GetIcon() => _icon;
        public void SetIcon(Sprite icon) => _icon = icon;

        public float GetCurrentValue() => _value.GetCurrentValue();
        public float GetMaxValue() => _value.GetMaxValue();
        public float GetPercentage() => _value.GetPercentage();
        public void Increase(float amount) => _value.Increase(amount);
        public void Decrease(float amount) => _value.Decrease(amount);
        public void SetCurrentValue(float value) => _value.SetCurrentValue(value);
        public bool IsDepleted() => _value.IsDepleted();
        public bool IsFull() => _value.IsFull();
    }
}
