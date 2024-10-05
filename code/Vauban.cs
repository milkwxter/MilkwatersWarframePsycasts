using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.Noise;
using UnityEngine.SocialPlatforms;
using UnityEngine;
using WarframePsycasts.PhotonStrikeCode;

namespace WarframePsycasts
{
    public class Ability_TeslaNervos : VFECore.Abilities.Ability
    {
        // ability 1: tesla nervos
        // spawn a roller drone that drives up to an enemy, and explodes in a electric fashion

        internal Pawn teslaNervos = null;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;

            // create and spawn our decoy
            teslaNervos = PawnGenerator.GeneratePawn(PawnKindDef.Named("WF_Vauban_TeslaNervos"), caster.Faction);
            GenSpawn.Spawn(teslaNervos, caster.Position, caster.Map);

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_TeslaNervos_Sound").PlayOneShot(new TargetInfo(targets[0].Cell, targets[0].Map, false));

            // tell the tesla nervos to move to an enemy
            Job job = JobMaker.MakeJob(JobDefOf.Goto, targets[0].Pawn);
            teslaNervos.jobs.StartJob(job, JobCondition.InterruptForced);
        }
    }

    public class Ability_Minelayer : VFECore.Abilities.Ability
    {
        // ability 2: minelayer
        // throw a flechette orb down, which will then activate and shoot nearby enemies for a bit

        Pawn flechetteOrb = null;
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(targets[0].Cell, targets[0].Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_Minelayer_Sound").PlayOneShot(new TargetInfo(targets[0].Cell, targets[0].Map, false));

            // create and spawn the mine
            flechetteOrb = PawnGenerator.GeneratePawn(PawnKindDef.Named("WF_Vauban_FlechetteOrb"), caster.Faction);
            GenSpawn.Spawn(flechetteOrb, targets[0].Cell, targets[0].Map);
        }

        public override void Tick()
        {
            base.Tick();

            // Check every 120 ticks (2 seconds)
            if (!base.pawn.IsHashIntervalTick(120)) return;

            // make sure the orb exists and is alive
            if (flechetteOrb != null && !flechetteOrb.DeadOrDowned)
            {
                // save list of pawns to hit
                List<Pawn> pawnsToNail = new List<Pawn>();

                // find new targets if they exist
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(flechetteOrb.Position, 15f, true))
                {
                    if (!cell.InBounds(flechetteOrb.Map)) continue;
                    List<Thing> things = cell.GetThingList(flechetteOrb.Map);
                    foreach (Thing thing in things)
                    {
                        if (thing is Pawn pawn)
                        {
                            // Make sure only target pawns other than ourselves, dead pawns, and downed pawns
                            if (pawn == flechetteOrb || pawn.Dead || pawn.Downed) continue;
                            if (pawn.Faction != null && !pawn.Faction.HostileTo(base.pawn.Faction)) continue;
                            if (AttackTargetFinder.CanSee(flechetteOrb, pawn) == false) continue;

                            if (pawnsToNail.Count < 8)
                                pawnsToNail.Add(pawn);
                        }
                    }
                }

                // cool sound
                if (pawnsToNail.Count > 0)
                    DefDatabase<SoundDef>.GetNamed("WF_Minelayer_Shot_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));

                // Launch the nails
                foreach (Pawn pawn in pawnsToNail)
                {
                    Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_Flechette_Nail"), pawn.Position, base.pawn.Map);
                    FleckMaker.Static(pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Target_Fleck"));
                    projectile.Launch(flechetteOrb, flechetteOrb.DrawPos, pawn, pawn, ProjectileHitFlags.IntendedTarget);
                }
            }
        }
    }

    public class Ability_PhotonStrike : VFECore.Abilities.Ability
    {
        // ability 3: photon strike
        // throw a flare at a location, after 1.5 seconds it will be orbital beamed

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // make our photon strike
            PhotonStrike photonStrike = (PhotonStrike)GenSpawn.Spawn(ThingDef.Named("WF_PhotonStrike"), targets[0].Cell, targets[0].Map);
            photonStrike.duration = 120; // Duration in ticks (2 seconds)
            photonStrike.instigator = base.pawn;

            // spawn it
            photonStrike.StartStrike();
        }
    }
}
