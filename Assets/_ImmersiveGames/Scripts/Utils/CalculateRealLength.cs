using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils
{
    public static class CalculateRealLength
    {
        public static Bounds GetBounds(GameObject objeto)
        {
            var bounds = GetRenderBounds(objeto);
            if (bounds.extents.x != 0) return bounds;

            bounds = new Bounds(objeto.transform.position, Vector3.zero);
            foreach (Transform child in objeto.transform)
            {
                // Verifica se o filho tem o componente IgnoreBoundsFlag
                if (child.GetComponent<IgnoreBoundsFlag>() != null) continue;
                var childRender = child.GetComponent<Renderer>();
                bounds.Encapsulate(childRender ? childRender.bounds : GetBounds(child.gameObject));
            }
            return bounds;
        }

        private static Bounds GetRenderBounds(GameObject objeto)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var render = objeto.GetComponent<Renderer>();
            return render != null ? render.bounds : bounds;
        }
    }
}