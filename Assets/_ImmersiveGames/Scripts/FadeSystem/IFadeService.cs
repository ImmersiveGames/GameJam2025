using System.Collections;
using UnityEngine;
namespace _ImmersiveGames.Scripts.FadeSystem
{
    public interface IFadeService
    {
        void RequestFadeIn();
        void RequestFadeOut();
    }
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator coroutine);
    }
}