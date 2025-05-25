using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    public class PoolTest : MonoBehaviour
    {
        [SerializeField] private PoolableObjectData _data;

        private void Start()
        {
            // Registrar o pool
            PoolManager.Instance.RegisterPool(_data);
        }

        private void Update()
        {
            // Teste 1: Obter um objeto
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var obj = PoolManager.Instance.GetObject(_data.ObjectName, Vector3.zero);
                Debug.Log(obj != null ? "Objeto obtido com sucesso!" : "Falha ao obter objeto.");
            }

            // Teste 2: Retornar um objeto
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var pool = PoolManager.Instance.GetPool(_data.ObjectName);
                if (pool != null)
                {
                    var activeObjects = pool.GetActiveObjects();
                    if (activeObjects.Count > 0)
                    {
                        pool.ReturnObject(activeObjects[0]);
                        Debug.Log("Objeto retornado ao pool.");
                    }
                    else
                    {
                        Debug.Log("Nenhum objeto ativo para retornar.");
                    }
                }
                else
                {
                    Debug.Log("Pool não encontrado.");
                }
            }

            // Teste 3: Obter múltiplos objetos
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                for (int i = 0; i < 7; i++)
                {
                    var pos = new Vector3(i * 2f, 0, 0);
                    PoolManager.Instance.GetObject(_data.ObjectName, pos);
                }
                Debug.Log("Múltiplos objetos solicitados.");
            }

            // Teste 4: Obter pool
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                var pool = PoolManager.Instance.GetPool(_data.ObjectName);
                Debug.Log(pool != null ? $"Pool '{_data.ObjectName}' encontrado." : $"Pool '{_data.ObjectName}' não encontrado.");
            }

            // Teste 5: Obter pool inexistente
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                var pool = PoolManager.Instance.GetPool("NonExistentPool");
                Debug.Log(pool != null ? "Pool 'NonExistentPool' encontrado (inesperado)." : "Pool 'NonExistentPool' não encontrado (esperado).");
            }
        }
    }
}