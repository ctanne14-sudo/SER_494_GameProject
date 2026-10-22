using UnityEngine;

namespace Roguelike
{
    public enum SpawnType
    {
        Player,
        Prisoner,
        Guard,
        Boss
    }

    public class SpawnPoint : MonoBehaviour
    {
        public SpawnType type = SpawnType.Prisoner;

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor();
            float radius = type == SpawnType.Boss ? 1.5f : 0.5f;
            Vector3 center = transform.position + Vector3.up * radius;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.DrawLine(center, center + transform.forward * (radius + 1f));
        }

        private Color GizmoColor()
        {
            switch (type)
            {
                case SpawnType.Player:
                    return Color.cyan;
                case SpawnType.Prisoner:
                    return new Color(1f, 0.6f, 0.1f);
                case SpawnType.Guard:
                    return Color.yellow;
                default:
                    return Color.red;
            }
        }
    }
}
