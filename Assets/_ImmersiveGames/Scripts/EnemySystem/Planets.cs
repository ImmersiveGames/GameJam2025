using System;
using _ImmersiveGames.Scripts.ScriptableObjects;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public class Planets : DestructibleObject
    {

        [SerializeField] public int resources;
        public override void Initialize()
        {
            base.Initialize();
        }


        /// <summary>
        /// Causa dano ao planeta
        /// </summary>
        /// <param name="dano">Quantidade de dano a ser causado</param>
        /// <returns>Verdadeiro se o planeta foi destruído</returns>
        public bool ReceberDano(int dano)
        {
            currentHealth = Mathf.Max(0, currentHealth - dano);

            if (currentHealth <= 0)
            {
                OnPlanetDestroy();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retorna a quantidade de resources disponível no planeta
        /// </summary>
        public int ObterQuantidadeRecurso()
        {
            return resources;
        }

        /// <summary>
        /// Coleta recursos do planeta
        /// </summary>
        /// <param name="quantidade">Quantidade a ser coletada</param>
        /// <returns>Quantidade efetivamente coletada</returns>
        public int ColetarRecurso(int quantidade)
        {
            int quantidadeColetada = Mathf.Min(quantidade, resources);
            resources -= quantidadeColetada;
            return quantidadeColetada;
        }

        private void OnPlanetDestroy()
        {
            // Lógica a ser executada quando o planeta for destruído
            // Pode disparar eventos, fazer animações, etc.
            Debug.Log($"Planets destruído! Recursos restantes: {resources}");
        }
    }
}