using System.Threading.Tasks;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.Extensions
{
    public static class AsyncOperationExtensions
    {
        public static Task ToTask(this AsyncOperation op)
        {
            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }
    }

}