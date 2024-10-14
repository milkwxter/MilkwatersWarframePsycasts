using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI.Group;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace WarframePsycasts
{
    public class DeathActionWorker_RemoveBody : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            // cool effect
            FleckMaker.Static(corpse.Position, corpse.Map, FleckDefOf.PsycastAreaEffect);
            
            // bye bye corpse
            corpse.Destroy();
        }
    }

    public class CompExplodeOnEnemyNearbyElectric : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();

            // Check every 60 ticks
            if (!this.parent.IsHashIntervalTick(60)) return;

            // Get pawn with this comp
            Pawn pawn = this.parent as Pawn;

            // make sure pawn exists
            if (pawn == null || pawn.Map == null) return;

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 3f, true))
            {
                if (thing is Pawn enemy)
                {
                    // Make sure only target only living enemies
                    if (enemy.Downed || enemy.Dead) continue;
                    if (enemy.Faction != null && !enemy.Faction.HostileTo(pawn.Faction)) continue;

                    // Die
                    pawn.Destroy();

                    // Radial explosion
                    Explosion(enemy.Position, enemy.Map);

                    break;
                }
            }
        }

        public void Explosion(IntVec3 pos, Map map)
        {
            // make a sound
            DefDatabase<SoundDef>.GetNamed("WF_TeslaNervos_Explode_Sound").PlayOneShot(new TargetInfo(pos, map, false));

            // make our damage def
            DamageInfo shocked = new DamageInfo(DamageDefOf.Burn, 25f, 1f, -1f, base.parent);

            // do an epic loop
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, 3f, true))
            {
                // cool effect
                FleckMaker.Static(cell, map, DefDatabase<FleckDef>.GetNamed("WF_Electric_Fleck"));

                // epic loop
                Thing[] array = cell.GetThingList(map).ToArray();
                foreach (Thing thing in array)
                {
                    // make sure we do stuff to only pawns
                    if (thing.GetType() != typeof(Pawn)) continue;

                    // make sure we only do stuff to people that we hate
                    Pawn pawn = thing as Pawn;
                    if (pawn.Faction != null && !pawn.Faction.HostileTo(base.parent.Faction)) continue;

                    // deal damage
                    pawn.TakeDamage(shocked);
                }
            }
        }
    }

    [DefOf]
    public static class WF_ThingDefOf 
    {
        static WF_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WF_ThingDefOf));
        }

        public static ThingDef BloodAltar;
	}
	
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
                        if (Rand.Chance(0.5f)) // 50% chance to attack FIRST target, 50% chane to move to next one and try again
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
                        }
                    }
                }
            }
        }
    }

    public class Hediff_Healing : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(180)) // Check every 180 ticks / 3 seconds
            {
                // do a fleck
                FleckMaker.AttachedOverlay(base.pawn, DefDatabase<FleckDef>.GetNamed("WF_Healing_Fleck"), new Vector3(0f, 0f, 0f));

                // Tend a random injury 25% of times the hediff ticks
                if (Rand.Chance(0.25f))
                    TendUtility.DoTend(base.pawn, base.pawn, null);

                // get list of injuries
                List<Hediff_Injury> pawnsInjuries = base.pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList();
                foreach (var injury in pawnsInjuries)
                {
                    // give 1 more health to each injury
                    injury.Heal(1f);
                }
            }
        }
    }
}
