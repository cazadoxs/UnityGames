namespace Estandartes.Core.Combat
{
    /// <summary>Datos de un candidato a rival (Vol. III §10.2).</summary>
    public struct TargetCandidate
    {
        public int Id;
        public float Distance;
        public bool AttackingMe;
        public bool FlankOrBackExposed;
        public float Health01;
        public bool KilledNearbyFriend;
        public bool InFrontOfMySlot;
        public int CurrentAttackers;
    }

    public static class TargetSelection
    {
        public const float SwitchHysteresis = 1.2f;

        public static float Score(in TargetCandidate c, float maxDistance)
        {
            return 1.0f * (1f - SimMath.Clamp01(c.Distance / maxDistance))
                 + (c.AttackingMe ? 0.8f : 0f)
                 + (c.FlankOrBackExposed ? 0.6f : 0f)
                 + 0.4f * (1f - SimMath.Clamp01(c.Health01))
                 + (c.KilledNearbyFriend ? 0.5f : 0f)
                 + (c.InFrontOfMySlot ? 0.3f : 0f)
                 - 0.4f * c.CurrentAttackers;
        }

        /// <summary>Elige rival con histéresis: solo cambia si el nuevo supera al actual ×1,2. Devuelve el Id o -1.</summary>
        public static int Select(System.ReadOnlySpan<TargetCandidate> candidates, int currentId, float maxDistance)
        {
            int best = -1;
            float bestScore = float.MinValue, currentScore = float.MinValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].Distance > maxDistance) continue;
                float s = Score(candidates[i], maxDistance);
                if (candidates[i].Id == currentId) currentScore = s;
                if (s > bestScore) { bestScore = s; best = candidates[i].Id; }
            }
            if (best < 0) return -1;
            if (currentScore > float.MinValue && best != currentId && bestScore < currentScore * SwitchHysteresis) return currentId;
            return best;
        }
    }
}
