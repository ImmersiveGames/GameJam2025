﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public class OrbitGizmoDrawer : MonoBehaviour
    {
        [SerializeField] private bool drawOrbitGizmos = true;
        [SerializeField] private Vector3 orbitCenter = Vector3.zero;
        private readonly List<float> _orbitRadii = new();

        public void UpdateOrbitRadii(List<float> radii)
        {
            _orbitRadii.Clear();
            _orbitRadii.AddRange(radii);
        }

        private void OnDrawGizmos()
        {
            if (!drawOrbitGizmos || _orbitRadii.Count == 0) return;

            #if UNITY_EDITOR
            Handles.color = Color.white;
            #endif

            for (int i = 0; i < _orbitRadii.Count; i++)
            {
                float t = i / (float)Mathf.Max(1, _orbitRadii.Count - 1);
                Gizmos.color = Color.Lerp(Color.cyan, Color.magenta, t);
                float radius = _orbitRadii[i];
                DrawCircle(orbitCenter, radius, 50);

                // Adicionar rótulo com o raio
                #if UNITY_EDITOR
                Vector3 labelPos = orbitCenter + new Vector3(radius, 0, 0);
                Handles.Label(labelPos, $"Orbit {i}: {radius:F2}", new GUIStyle { normal = { textColor = Color.white } });
                #endif
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }
}