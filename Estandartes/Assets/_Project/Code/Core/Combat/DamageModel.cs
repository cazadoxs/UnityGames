using Estandartes.Core.Content;

namespace Estandartes.Core.Combat
{
    public enum HitZone { Head, Torso, Limbs }

    /// <summary>Modelo de daño (Vol. I §7.7). Función pura.</summary>
    public static class DamageModel
    {
        public static float ZoneMultiplier(HitZone z) => z == HitZone.Head ? 1.5f : (z == HitZone.Torso ? 1f : 0.75f);

        /// <summary>
        /// daño = base × mult_tipo × (1 − armadura × (1 − penetración)) × mult_zona × impulso.
        /// </summary>
        public static float Compute(float baseDamage, float typeMultiplier, float armor, float penetration, HitZone zone, float impulse)
        {
            float absorbed = SimMath.Clamp01(armor) * (1f - SimMath.Clamp01(penetration));
            return baseDamage * typeMultiplier * (1f - absorbed) * ZoneMultiplier(zone) * (impulse <= 0f ? 1f : impulse);
        }

        public static float Compute(WeaponDefinition weapon, ArmorValues armor, HitZone zone, float impulse = 1f) =>
            Compute(weapon.BaseDamage, 1f, armor.For(weapon.DamageType), weapon.Penetration, zone, impulse);

        /// <summary>Factor de impulso por velocidad relativa (cargas): 1 + max(0, v − 3) / 6, máximo 2,5.</summary>
        public static float ImpulseFromSpeed(float relativeSpeed) => SimMath.Clamp(1f + (relativeSpeed - 3f) / 6f, 1f, 2.5f);

        /// <summary>Daño esperado contra la distribución de zonas (cabeza 10 %, torso 55 %, extremidades 35 %).</summary>
        public static float Expected(WeaponDefinition weapon, ArmorValues armor, float impulse = 1f) =>
            0.10f * Compute(weapon, armor, HitZone.Head, impulse)
          + 0.55f * Compute(weapon, armor, HitZone.Torso, impulse)
          + 0.35f * Compute(weapon, armor, HitZone.Limbs, impulse);
    }
}
