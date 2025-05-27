using _ImmersiveGames.Scripts.EaterSystem;
using UnityEngine;
using UnityEngine.UI;

public class NPCDirectionIndicator : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Transform npc;
    public Camera mainCamera;
    public RectTransform iconUI;
    public Image iconImage; // Para mudar a cor

    [Header("Configuração de Tela")]
    public float screenMargin = 50f; // Margem para não colar nas bordas

    [Header("Configuração de Distância")]
    public float distanciaProxima = 10f;
    public float distanciaMedia = 30f;

    [Header("Cores por Distância")]
    public Color corProxima = Color.green;
    public Color corMedia = Color.yellow;
    public Color corLonge = Color.red;

    void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;
        npc = GameObject.FindWithTag("WorldEater").transform;
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (player == null || npc == null || mainCamera == null) return;

        Vector3 direcao = npc.position - player.position;
        direcao.y = 0; // Ignora eixo Y

        float distancia = direcao.magnitude;

        Vector3 telaPos = mainCamera.WorldToScreenPoint(npc.position);

        bool estaNaTela = telaPos.z > 0 &&
                           telaPos.x > 0 && telaPos.x < Screen.width &&
                           telaPos.y > 0 && telaPos.y < Screen.height;

        if (estaNaTela)
        {
            iconUI.gameObject.SetActive(false);
            return;
        }
        else
        {
            iconUI.gameObject.SetActive(true);
        }

        // Centro da tela
        Vector3 centroTela = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // Direção na tela
        Vector3 direcaoTela = (telaPos - centroTela).normalized;

        // Posição do ícone nas bordas
        Vector3 posicaoIcone = centroTela + direcaoTela * (Mathf.Min(Screen.width, Screen.height) / 2f - screenMargin);

        // Aplica posição
        iconUI.position = posicaoIcone;

        // Rotaciona o ícone para apontar na direção
        float angulo = Mathf.Atan2(direcaoTela.y, direcaoTela.x) * Mathf.Rad2Deg;
        iconUI.rotation = Quaternion.Euler(0, 0, angulo - 90f);

        // Atualiza cor conforme distância
        if (distancia <= distanciaProxima)
        {
            iconImage.color = corProxima;
        }
        else if (distancia <= distanciaMedia)
        {
            iconImage.color = corMedia;
        }
        else
        {
            iconImage.color = corLonge;
        }
    }
}
