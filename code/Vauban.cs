using RimWorld.Planet;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using Verse.AI;
using VFECore.Abilities;
using System.Linq;
using UnityEngine;

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

        internal Pawn flechetteOrb = null;
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

            // cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_PhotonStrike_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));
        }
    }

    public class PhotonStrike : OrbitalStrike // copied from PowerBeam, with values changed
    {
        public const float Radius = 5f;

        private const int FiresStartedPerTick = 2;

        private static readonly IntRange FlameDamageAmountRange = new IntRange(5, 15);

        private static readonly IntRange CorpseFlameDamageAmountRange = new IntRange(5, 10);

        private static List<Thing> tmpThings = new List<Thing>();

        public override void StartStrike()
        {
            base.StartStrike();
            MakePhotonStrikeMote(base.Position, base.Map);
        }

        public static void MakePhotonStrikeMote(IntVec3 cell, Map map)
        {
            Mote obj = (Mote)ThingMaker.MakeThing(ThingDef.Named("Mote_PhotonStrike"));
            obj.exactPosition = cell.ToVector3Shifted();
            obj.Scale = 10f;
            obj.rotationRate = 1.2f;
            GenSpawn.Spawn(obj, cell, map);
        }

        public override void Tick()
        {
            base.Tick();
            if (!base.Destroyed)
            {
                for (int i = 0; i < 4; i++)
                {
                    StartRandomFireAndDoFlameDamage();
                }
            }
        }

        private void StartRandomFireAndDoFlameDamage()
        {
            IntVec3 c = (from x in GenRadial.RadialCellsAround(base.Position, Radius, useCenter: true)
                         where x.InBounds(base.Map)
                         select x).RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(base.Position) / 15f, 1f) + 0.05f);
            FireUtility.TryStartFireIn(c, base.Map, Rand.Range(0.1f, 0.925f), instigator);
            tmpThings.Clear();
            tmpThings.AddRange(c.GetThingList(base.Map));
            for (int i = 0; i < tmpThings.Count; i++)
            {
                int num = ((tmpThings[i] is Corpse) ? CorpseFlameDamageAmountRange.RandomInRange : FlameDamageAmountRange.RandomInRange);
                Pawn pawn = tmpThings[i] as Pawn;
                BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = null;
                if (pawn != null)
                {
                    battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_PowerBeam, instigator as Pawn);
                    Find.BattleLog.Add(battleLogEntry_DamageTaken);
                }
                tmpThings[i].TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, instigator, null, weaponDef)).AssociateWithLog(battleLogEntry_DamageTaken);
            }
            tmpThings.Clear();
        }
    }

    public class Ability_Bastille : VFECore.Abilities.Ability
    {
        // ability 4: bastille
        // spawn a containment field that slows enemies and strips their armor
        // if a containment field is already active, turn it into a damaging vortex

        Thing thing = null;
        int tickCounter = 0;
        static int ticksToSurvive = 0;

        public override void Tick()
        {
            base.Tick();

            // see if the buildings time is up
            if (ticksToSurvive != 0 && tickCounter > ticksToSurvive)
            {
                Log.Message("Bastille should be destroyed now.");
                FleckMaker.Static(thing.Position, thing.Map, FleckDefOf.PsycastAreaEffect);
                DefDatabase<SoundDef>.GetNamed("WF_Bastille_Sound").PlayOneShot(new TargetInfo(thing.Position, thing.Map, false));
                thing.Destroy();
                tickCounter = 0;
                return;
            }

            // see if the building exists, and if so, tick
            if (thing == null) return;
            else
            {
                if (tickCounter != 0 && Find.TickManager.TicksGame >= tickCounter)
                {
                    // stop ticking
                    tickCounter = 0;

                    // get all enemies nearby
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(thing.Position, 5f, true))
                    {
                        if (!cell.InBounds(thing.Map)) continue;
                        List<Thing> things = cell.GetThingList(thing.Map);
                        foreach (Thing thing in things)
                        {
                            if (thing is Pawn pawn)
                            {
                                // Make sure only target living breathing enemies
                                if (pawn.Dead || pawn.Downed) continue;
                                if (pawn.Faction != null && !pawn.Faction.HostileTo(base.pawn.Faction)) continue;

                                // add hediffs
                                pawn.health.AddHediff(HediffDef.Named("WFP_Generic_ArmorStrip"));
                                pawn.health.AddHediff(HediffDef.Named("WFP_Generic_Stasis"));
                            }
                        }
                    }

                    // do some more ticking
                    Log.Message("Ticking works.");
                    tickCounter = Find.TickManager.TicksGame + 20;
                }
            }
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // get the building to spawn from the xml file
            AbilityExtension_Building bastille = def.GetModExtension<AbilityExtension_Building>();
            if (bastille == null)
            {
                return;
            }

            // create a bastille and spawn it
            if (targets[0].Cell.GetFirstBuilding(targets[0].Map) != null) return;
            thing = GenSpawn.Spawn(bastille.building, targets[0].Cell, targets[0].Map);
            thing.SetFactionDirect(base.pawn.Faction);

            // start ticking
            tickCounter = Find.TickManager.TicksGame + 20;
            ticksToSurvive = Find.TickManager.TicksGame + 1200;

            // cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_Bastille_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));
            Mote obj = (Mote)ThingMaker.MakeThing(ThingDef.Named("Mote_Bastille"));
            obj.exactPosition = targets[0].Cell.ToVector3Shifted();
            obj.Scale = 10f;
            obj.rotationRate = 1.2f;
            GenSpawn.Spawn(obj, targets[0].Cell, targets[0].Map);
        }
    }
}
