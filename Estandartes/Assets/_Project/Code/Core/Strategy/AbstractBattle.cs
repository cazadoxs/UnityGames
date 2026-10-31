using System;
using System.Collections.Generic;

namespace Estandartes.Core.Strategy
{
    public enum GroupOrderType { Hold, MoveTo, Attack, Flank, Harass, Reserve, Retreat }

    /// <summary>Grupo abstracto = una escuadra en el modelo agregado (Vol. III §16.2).</summary>
    public struct AbstractGroup
    {
        public int Team;
        public Vec2 Position;
        public float Soldiers;      // efectivos (continuo)
        public float StartSoldiers;
        public float MeleeQuality;  // q: calidad de melé por soldado en contacto
        public float RangedRate;    // r: bajas por tirador y segundo a distancia media
        public float Range;         // alcance de tiro (0 = sin tiro)
        public float Speed;         // m/s de marcha
        public float Spacing;       // espaciado lateral
        public float Morale;        // 0..100
        public float Fatigue;       // 0..1
        public float Armor;         // 0..1 reduce el tiro recibido
        public bool Cavalry;
        public bool Routed;

        public float Width => Math.Max(1f, MathF.Sqrt(Math.Max(Soldiers, 1f) * 4f)) * Math.Max(Spacing, 0.5f);
        public bool Active => !Routed && Soldiers >= 1f;
    }

    public struct GroupOrder
    {
        public GroupOrderType Type;
        public int TargetGroup;   // -1 = punto
        public Vec2 Point;
        /// <summary>Segundos de espera antes de ejecutar (sincronización).</summary>
        public float Delay;
    }

    /// <summary>Coeficientes del modelo agregado (se calibran contra L1, Vol. III §16.3).</summary>
    public struct AggregateCoefficients
    {
        public float MeleeK;        // k: bajas por soldado en contacto y segundo
        public float FlankBonus;    // +50 % si se ataca por flanco o retaguardia
        public float MoraleLossPerCasualtyFraction; // μ
        public float EngageDistance;
        public float RoutMorale;

        public static AggregateCoefficients Default => new AggregateCoefficients
        {
            MeleeK = 0.012f, FlankBonus = 0.5f, MoraleLossPerCasualtyFraction = 220f, EngageDistance = 6f, RoutMorale = 25f,
        };
    }

    public struct BattleOutcome
    {
        public float OwnLosses;      // fracción 0..1
        public float EnemyLosses;
        public float OwnMorale;      // media final
        public float EnemyMorale;
        public float ObjectiveDelta; // control de objetivos ganado (−1..1)
        public float Time;
    }

    /// <summary>
    /// Simulación abstracta por grupos tipo Lanchester: melé lineal en el frente, tiro proporcional a los tiradores.
    /// Es la simulación lejana (L3) y la que usa el general para evaluar planes (Vol. III §6.5).
    /// </summary>
    public sealed class AbstractBattle
    {
        public readonly List<AbstractGroup> Groups = new List<AbstractGroup>();
        public readonly List<GroupOrder> Orders = new List<GroupOrder>();
        public AggregateCoefficients Coefficients = AggregateCoefficients.Default;
        /// <summary>Puntos de objetivo y su valor; el control se mide por cercanía de efectivos.</summary>
        public readonly List<(Vec2 position, float value, float radius)> Objectives = new List<(Vec2, float, float)>();
        public float Time;

        public int Add(AbstractGroup g, GroupOrder order)
        {
            if (g.StartSoldiers <= 0) g.StartSoldiers = g.Soldiers;
            Groups.Add(g);
            Orders.Add(order);
            return Groups.Count - 1;
        }

        public AbstractBattle Clone()
        {
            var b = new AbstractBattle { Coefficients = Coefficients, Time = Time };
            b.Groups.AddRange(Groups);
            b.Orders.AddRange(Orders);
            b.Objectives.AddRange(Objectives);
            return b;
        }

        /// <summary>Avanza la simulación <paramref name="dt"/> segundos. <paramref name="noise"/> multiplica las bajas (≈1).</summary>
        public void Step(float dt, ref SimRandom rng, float noise = 0.15f)
        {
            int n = Groups.Count;
            var losses = new float[n];
            var c = Coefficients;

            // Movimiento
            for (int i = 0; i < n; i++)
            {
                var g = Groups[i];
                if (!g.Active) continue;
                var o = Orders[i];
                if (o.Delay > 0f) { o.Delay -= dt; Orders[i] = o; continue; }
                Vec2 target = g.Position;
                switch (o.Type)
                {
                    case GroupOrderType.MoveTo:
                    case GroupOrderType.Flank:
                        target = o.Point; break;
                    case GroupOrderType.Retreat:
                        target = o.Point; break;
                    case GroupOrderType.Attack:
                    case GroupOrderType.Harass:
                        int t = o.TargetGroup >= 0 && Groups[o.TargetGroup].Active ? o.TargetGroup : NearestEnemy(i);
                        if (t >= 0)
                        {
                            target = Groups[t].Position;
                            if (o.Type == GroupOrderType.Harass && g.Range > 0)
                            {
                                var dir = (g.Position - target).Normalized;
                                target = target + dir * (g.Range * 0.7f);
                            }
                        }
                        break;
                }
                // Flanqueo: tras llegar al punto, ataca al enemigo más cercano
                if (o.Type == GroupOrderType.Flank && Vec2.Distance(g.Position, o.Point) < 10f)
                {
                    o.Type = GroupOrderType.Attack; o.TargetGroup = -1; Orders[i] = o;
                }
                var delta = target - g.Position;
                float dist = delta.Length;
                if (dist > c.EngageDistance * 0.5f && !InMelee(i))
                {
                    float speed = g.Speed * (1f - 0.5f * g.Fatigue);
                    g.Position += delta / dist * Math.Min(dist, speed * dt);
                    g.Fatigue = Math.Min(1f, g.Fatigue + dt * (g.Cavalry ? 0.002f : 0.003f));
                }
                Groups[i] = g;
            }

            // Combate
            for (int i = 0; i < n; i++)
            {
                var a = Groups[i];
                if (!a.Active) continue;
                for (int j = 0; j < n; j++)
                {
                    var b = Groups[j];
                    if (j == i || b.Team == a.Team || !b.Active) continue;
                    float d = Vec2.Distance(a.Position, b.Position);
                    float eff = (1f - 0.5f * a.Fatigue) * (0.5f + a.Morale / 200f);
                    if (d <= c.EngageDistance + (a.Width + b.Width) * 0.25f)
                    {
                        float front = Math.Min(a.Width, b.Width) / Math.Max(a.Spacing, 0.5f);
                        float contact = Math.Min(front, a.Soldiers);
                        bool flank = IsFlankAttack(i, j);
                        float charge = a.Cavalry && Orders[i].Type == GroupOrderType.Attack ? 1.5f : 1f;
                        losses[j] += c.MeleeK * contact * a.MeleeQuality * (flank ? 1f + c.FlankBonus : 1f) * eff * charge * dt;
                    }
                    else if (a.Range > 0 && d <= a.Range)
                    {
                        losses[j] += a.RangedRate * a.Soldiers * (1f - b.Armor) * (1f - 0.5f * d / a.Range) * eff * dt;
                    }
                }
            }

            // Aplicar bajas y moral
            for (int j = 0; j < n; j++)
            {
                if (losses[j] <= 0f) continue;
                var g = Groups[j];
                float l = losses[j] * (1f + noise * rng.NextGaussian());
                l = SimMath.Clamp(l, 0f, g.Soldiers);
                g.Soldiers -= l;
                g.Morale = Math.Max(0f, g.Morale - c.MoraleLossPerCasualtyFraction * l / Math.Max(g.StartSoldiers, 1f));
                if (g.Morale < c.RoutMorale || g.Soldiers < 1f) g.Routed = true;
                Groups[j] = g;
            }
            Time += dt;
        }

        private bool InMelee(int i)
        {
            var a = Groups[i];
            for (int j = 0; j < Groups.Count; j++)
            {
                var b = Groups[j];
                if (b.Team != a.Team && b.Active && Vec2.Distance(a.Position, b.Position) <= Coefficients.EngageDistance + (a.Width + b.Width) * 0.25f)
                    return true;
            }
            return false;
        }

        /// <summary>Flanco: el atacante llega cuando el defensor ya está trabado con otro grupo.</summary>
        private bool IsFlankAttack(int attacker, int defender)
        {
            var d = Groups[defender];
            for (int k = 0; k < Groups.Count; k++)
            {
                if (k == attacker || Groups[k].Team == d.Team || !Groups[k].Active) continue;
                if (Vec2.Distance(Groups[k].Position, d.Position) <= Coefficients.EngageDistance + (Groups[k].Width + d.Width) * 0.25f)
                    return true;
            }
            return false;
        }

        public int NearestEnemy(int i)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int j = 0; j < Groups.Count; j++)
            {
                if (Groups[j].Team == Groups[i].Team || !Groups[j].Active) continue;
                float d = Vec2.DistanceSquared(Groups[j].Position, Groups[i].Position);
                if (d < bestD) { bestD = d; best = j; }
            }
            return best;
        }

        public void Run(float duration, float dt, ref SimRandom rng)
        {
            float end = Time + duration;
            while (Time < end && !Decided()) Step(dt, ref rng);
        }

        public bool Decided()
        {
            bool a = false, b = false;
            foreach (var g in Groups) { if (!g.Active) continue; if (g.Team == 0) a = true; else b = true; }
            return !(a && b);
        }

        /// <summary>Resultado desde el punto de vista de <paramref name="team"/>.</summary>
        public BattleOutcome Outcome(int team)
        {
            float own0 = 0, own1 = 0, en0 = 0, en1 = 0, ownM = 0, enM = 0; int ownN = 0, enN = 0;
            foreach (var g in Groups)
            {
                if (g.Team == team) { own0 += g.StartSoldiers; own1 += g.Routed ? 0 : g.Soldiers; ownM += g.Routed ? 0 : g.Morale; ownN++; }
                else { en0 += g.StartSoldiers; en1 += g.Routed ? 0 : g.Soldiers; enM += g.Routed ? 0 : g.Morale; enN++; }
            }
            float obj = 0f, total = 0f;
            foreach (var (pos, value, radius) in Objectives)
            {
                float mine = 0, theirs = 0;
                foreach (var g in Groups)
                {
                    if (!g.Active || Vec2.Distance(g.Position, pos) > radius) continue;
                    if (g.Team == team) mine += g.Soldiers; else theirs += g.Soldiers;
                }
                total += value;
                if (mine + theirs > 0) obj += value * (mine - theirs) / (mine + theirs);
            }
            return new BattleOutcome
            {
                OwnLosses = own0 > 0 ? 1f - own1 / own0 : 0f,
                EnemyLosses = en0 > 0 ? 1f - en1 / en0 : 0f,
                OwnMorale = ownN > 0 ? ownM / ownN : 0f,
                EnemyMorale = enN > 0 ? enM / enN : 0f,
                ObjectiveDelta = total > 0 ? obj / total : 0f,
                Time = Time,
            };
        }
    }
}
