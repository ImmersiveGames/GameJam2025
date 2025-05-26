using UnityEngine;

public class PartBreaker : MonoBehaviour
{
    [SerializeField] GameObject[] partesDoPlaneta;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DestroyPlanetPiece(float vidaAtual, float vidaMaxima)
    {
        int totalPartes = partesDoPlaneta.Length;
        int partesAtivas = Mathf.CeilToInt((vidaAtual / vidaMaxima) * totalPartes);

        for (int i = 0; i < totalPartes; i++)
        {
            partesDoPlaneta[i].SetActive(i < partesAtivas);
        }
    }
}
