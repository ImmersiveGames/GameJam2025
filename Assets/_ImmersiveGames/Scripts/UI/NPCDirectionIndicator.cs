using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.UI
{
    public class NpcDirectionIndicator : MonoBehaviour
    {
        [Header("Referências")]
        public RectTransform iconUI;
        public Image iconImage; // Para mudar a cor

        [Header("Configuração de Tela")]
        public float screenMargin = 50f; // Margem para não colar nas bordas

        [Header("Configuração de Distância")]
        public float nextDistance = 10f;
        public float mediaDistance = 30f;

        [Header("Cores por Distância")]
        public Color corNext = Color.green;
        public Color corMedia = Color.yellow;
        public Color corLonge = Color.red;
        
        private Transform _player;
        private Transform _npc;
        private Camera _mainCamera;

        private void Awake()
        {
            _player = GameManager.Instance.Player;
            _npc = GameManager.Instance.WorldEater;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_player == null || _npc == null || _mainCamera == null) return;

            var npcDistance = _npc.position - _player.position;
            npcDistance.y = 0; // Ignora eixo Y

            float distancia = npcDistance.magnitude;

            var telaPos = _mainCamera.WorldToScreenPoint(_npc.position);

            bool estaNaTela = telaPos.z > 0 &&
                telaPos.x > 0 && telaPos.x < Screen.width &&
                telaPos.y > 0 && telaPos.y < Screen.height;

            if (estaNaTela)
            {
                iconUI.gameObject.SetActive(false);
                return;
            }
            iconUI.gameObject.SetActive(true);

            // Centro da tela
            var centroTela = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            // Direção na tela
            var direcaoTela = (telaPos - centroTela).normalized;

            // Posição do ìcone nas bordas
            var posicaoIcone = centroTela + direcaoTela * (Mathf.Min(Screen.width, Screen.height) / 2f - screenMargin);

            // Aplica posição
            iconUI.position = posicaoIcone;

            // Rotaciona o �cone para apontar na direção
            float angulo = Mathf.Atan2(direcaoTela.y, direcaoTela.x) * Mathf.Rad2Deg;
            iconUI.rotation = Quaternion.Euler(0, 0, angulo - 90f);

            // Atualiza cor conforme distância
            if (distancia <= nextDistance)
            {
                iconImage.color = corNext;
            }
            else if (distancia <= mediaDistance)
            {
                iconImage.color = corMedia;
            }
            else
            {
                iconImage.color = corLonge;
            }
        }
    }
}
