using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime.Internal
{
    /// <summary>
    /// Cache auxiliar que impede o disparo múltiplo de eventos no mesmo frame
    /// e limpa entradas antigas sem gerar lixo em memória.
    /// </summary>
    internal sealed class FrameEventCache
    {
        private readonly Dictionary<IDetectable, int> _frameCache = new();
        private readonly List<IDetectable> _cleanupBuffer = new();

        public bool TryRegister(IDetectable detectable, int frame)
        {
            if (detectable == null)
            {
                return false;
            }

            if (_frameCache.TryGetValue(detectable, out int lastFrame) && lastFrame == frame)
            {
                return false;
            }

            _frameCache[detectable] = frame;
            return true;
        }

        public void Cleanup(int currentFrame, int maxFrameAge = 1)
        {
            int threshold = currentFrame - maxFrameAge;

            foreach (var kvp in _frameCache)
            {
                if (kvp.Value < threshold)
                {
                    _cleanupBuffer.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _cleanupBuffer.Count; i++)
            {
                _frameCache.Remove(_cleanupBuffer[i]);
            }

            _cleanupBuffer.Clear();
        }

        public void Clear()
        {
            _frameCache.Clear();
            _cleanupBuffer.Clear();
        }
    }
}
