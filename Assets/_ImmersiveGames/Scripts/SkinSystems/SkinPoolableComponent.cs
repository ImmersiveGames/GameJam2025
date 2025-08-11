using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Gerencia skins para objetos no sistema de pooling.
    /// </summary>
    [DebugLevel(DebugLevel.Warning)]
    public class SkinPoolableComponent : SkinComponentBase
    {
        #region Fields
        private PooledObject pooledObject;
        public UnityEvent OnActivated { get; } = new UnityEvent();
        public UnityEvent OnDeactivated { get; } = new UnityEvent();
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();

            pooledObject = GetComponent<PooledObject>();
            if (pooledObject == null)
            {
                DebugUtility.LogError<SkinPoolableComponent>($"Componente PooledObject não encontrado em '{name}'.", this);
                return;
            }

            pooledObject.OnActivated.AddListener(OnPooledObjectActivated);
            pooledObject.OnDeactivated.AddListener(OnPooledObjectDeactivated);

            Initialize();
        }
        #endregion

        #region Activation
        public override void Activate()
        {
            base.Activate();
            OnActivated.Invoke();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            OnDeactivated.Invoke();
        }

        private void OnPooledObjectActivated() => Activate();
        private void OnPooledObjectDeactivated() => Deactivate();
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            if (pooledObject != null)
            {
                pooledObject.OnActivated.RemoveListener(OnPooledObjectActivated);
                pooledObject.OnDeactivated.RemoveListener(OnPooledObjectDeactivated);
            }
        }
        #endregion
    }
}