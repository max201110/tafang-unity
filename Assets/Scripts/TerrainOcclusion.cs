using UnityEngine;

namespace Verdant
{
    // Only solid terrain is on layer 8. Towers, enemies and effects never block shots.
    public static class TerrainOcclusion
    {
        public const int Layer = 8;
        public const int Mask = 1 << Layer;
        public const float ShotRadius = .045f;

        public static bool Sweep(Vector3 from, Vector3 to, float radius, out Vector3 contact)
        {
            contact = from;
            // SphereCast alone does not detect a cast starting inside a collider.
            if (Physics.CheckSphere(from, radius, Mask, QueryTriggerInteraction.Ignore)) return true;
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .000001f) return false;
            RaycastHit hit;
            if (!Physics.SphereCast(from, radius, delta.normalized, out hit, delta.magnitude, Mask, QueryTriggerInteraction.Ignore)) return false;
            contact = hit.point + hit.normal * .06f;
            return true;
        }

        public static bool Clear(Vector3 from, Vector3 to)
        {
            Vector3 contact;
            return !Sweep(from, to, .015f, out contact);
        }

        public static Vector3 ShotPoint(Vector3 origin, Vector3 target, float progress, TowerKind kind)
        {
            float t = Mathf.Clamp01(progress);
            return Vector3.Lerp(origin, target, t) + Vector3.up * (kind == TowerKind.Bloom ? Mathf.Sin(t * Mathf.PI) * 2 : 0);
        }

        public static bool CanFire(Vector3 origin, Vector3 target, TowerKind kind)
        {
            Vector3 previous = origin, contact;
            int steps = kind == TowerKind.Bloom ? 24 : 1;
            for (int i = 1; i <= steps; i++) {
                Vector3 next = ShotPoint(origin, target, (float)i / steps, kind);
                if (Sweep(previous, next, ShotRadius, out contact)) return false;
                previous = next;
            }
            return true;
        }
    }
}
