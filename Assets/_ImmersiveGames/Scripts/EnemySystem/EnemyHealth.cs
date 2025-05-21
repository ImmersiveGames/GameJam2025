using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private Image _healthBar;
        [SerializeField] private bool _autoHide = true;
        [SerializeField] private float _hideAfterDamage = 3f;
        
        private Enemy _enemy;
        private float _hideTimer;
        private bool _showingHealth;
        
        private void Awake()
        {
            _enemy = GetComponentInParent<Enemy>();
            
            if (_enemy == null)
            {
                Debug.LogWarning("EnemyHealth não conseguiu encontrar o componente Enemy no pai!");
            }
            
            if (_healthBar != null && _autoHide)
            {
                _healthBar.gameObject.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (_enemy == null || _healthBar == null)
                return;
                
            // Atualizar barra de saúde
            _healthBar.fillAmount = _enemy.CurrentHealth / _enemy.MaxHealth;
            
            // Auto esconder a barra de saúde após um tempo
            if (_showingHealth && _autoHide)
            {
                _hideTimer -= Time.deltaTime;
                
                if (_hideTimer <= 0)
                {
                    _healthBar.gameObject.SetActive(false);
                    _showingHealth = false;
                }
            }
        }
        
        public void ShowHealth()
        {
            if (_healthBar != null)
            {
                _healthBar.gameObject.SetActive(true);
                _showingHealth = true;
                _hideTimer = _hideAfterDamage;
            }
        }
        
        // Este método pode ser chamado quando o inimigo toma dano
        public void OnEnemyDamaged()
        {
            ShowHealth();
        }
    }
}
