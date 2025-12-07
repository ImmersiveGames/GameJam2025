using System.Collections;
using UnityEngine;
namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class GlobalCoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public Coroutine Run(IEnumerator coroutine) => StartCoroutine(coroutine);
    }
}