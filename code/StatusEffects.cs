using RimWorld;
using System.Linq;
using Verse.AI;
using Verse;
using UnityEngine;

namespace WarframePsycasts
{
    // Generic non-elemental Status effects
    public class Hediff_Impact : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            if (base.pawn.IsHashIntervalTick(180)) // Check every 180 ticks
            {
                // make sure pawn is still living
                if (base.pawn.Dead)
                {
                    base.pawn.health.RemoveHediff(this);
                }

                // do a fleck
                FleckMaker.AttachedOverlay(base.pawn, DefDatabase<FleckDef>.GetNamed("WF_Impact_Fleck"), new Vector3(0f, 0f, 0f));
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            Pawn hediffedPawn = base.pawn;
            Map map = hediffedPawn.Map;

            // Make him jump
            FleckMaker.Static(pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Dust_Fleck"));
            PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, hediffedPawn, hediffedPawn.Position, null, DefDatabase<SoundDef>.GetNamed("WF_Impact"), false, null, null, hediffedPawn);
            if (pawnFlyer != null)
            {
                FleckMaker.ThrowDustPuff(hediffedPawn.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f), map, 2f);
                GenSpawn.Spawn(pawnFlyer, hediffedPawn.Position, map);
            }

            // Do a stun
            hediffedPawn.stances.stunner.StunFor(GenTicks.SecondsToTicks(3f), null, addBattleLog: false);
        }
    }

    public class Hediff_Puncture : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            if (base.pawn.IsHashIntervalTick(180)) // Check every 180 ticks
            {
                // make sure pawn is still living
                if (base.pawn.Dead)
                {
                    base.pawn.health.RemoveHediff(this);
                }

                // do a fleck
                FleckMaker.AttachedOverlay(base.pawn, DefDatabase<FleckDef>.GetNamed("WF_Puncture_Fleck"), new Vector3(0f, 0f, 0f));
            }
        }
    }

    public class Hediff_Slash : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            if (base.pawn.IsHashIntervalTick(180)) // Check every 180 ticks
            {
                // make sure pawn is still living
                if (base.pawn.Dead)
                {
                    base.pawn.health.RemoveHediff(this);
                }

                // do a fleck
                FleckMaker.AttachedOverlay(base.pawn, DefDatabase<FleckDef>.GetNamed("WF_Slash_Fleck"), new Vector3(0f, 0f, 0f));
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            Pawn hediffedPawn = base.pawn;

            // Lose some blood
            HediffDef bloodLossDef = HediffDef.Named("BloodLoss");
            HealthUtility.AdjustSeverity(pawn, bloodLossDef, 0.15f);

            // Spawn some filth
            FilthMaker.TryMakeFilth(hediffedPawn.Position, hediffedPawn.Map, ThingDefOf.Filth_Blood, hediffedPawn.LabelIndefinite());
        }
    }

    // Generic elemental status effects

    // Generic combined elemental status effects
    public class Hediff_Radiation : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(180)) // Check every 180 ticks
            {
                // make sure pawn can actually attack first
                if (base.pawn.DeadOrDowned)
                {
                    base.pawn.health.RemoveHediff(this);
                }

                // Find nearby pawns
                var nearbyPawns = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 10f, true).OfType<Pawn>();
                if (nearbyPawns.Count() == 0) return;

                // Make them attack each other
                foreach (var targetPawn in nearbyPawns)
                {
                    // make sure we target guys with a faction that is FRIENDLY to the guy with the hediff
                    if (targetPawn.Faction != null && !targetPawn.Faction.HostileTo(base.pawn.Faction))
                    {
                        if (Rand.Chance(0.5f)) // 50% chance to attack FIRST target, 50% chance to move to next one and try again
                        {
                            // do a fleck
                            FleckMaker.AttachedOverlay(base.pawn, DefDatabase<FleckDef>.GetNamed("WF_Radiation_Fleck"), new Vector3(0f, 0f, 0f));

                            // make sure we make him shoot or melee depending on weapon
                            if (base.pawn.equipment.Primary != null && base.pawn.equipment.Primary.def.IsRangedWeapon)
                            {
                                // Ranged attack
                                Job job = JobMaker.MakeJob(JobDefOf.AttackStatic, targetPawn);
                                job.maxNumStaticAttacks = 1; // Number of shots
                                job.expiryInterval = 400; // Duration of the job
                                base.pawn.jobs.TryTakeOrderedJob(job);
                            }
                            else
                            {
                                // Melee attack
                                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, targetPawn);
                                base.pawn.jobs.TryTakeOrderedJob(job);
                            }

                            // we are done here dont keep iterating
                            return;
                        }
                    }
                }
            }
        }
    }
}
