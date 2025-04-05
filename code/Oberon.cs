using RimWorld.Planet;
using RimWorld;
using Verse;
using Verse.Sound;
using System.Collections.Generic;

namespace WarframePsycasts
{
    public class Ability_Smite : VFECore.Abilities.Ability
    {
        // ability 1: smite
        // hit a target with a ranged attack, inflicting radiation. then orbs will come from the target to get other enemies

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;
            HediffDef hediff = HediffDef.Named("WFP_Generic_Radiation");
            DamageInfo smote = new DamageInfo(DamageDefOf.Burn, 40f, 1f, -1f, base.pawn);
            List<Pawn> pawnsToOrb = new List<Pawn>();

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(targets[0].Cell, caster.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(targets[0].Cell, targets[0].Map, DefDatabase<FleckDef>.GetNamed("WF_FlameHoly_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_Smite_Sound").PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));

            // add the hediff and damage
            targets[0].Pawn.health.AddHediff(hediff);
            targets[0].Pawn.TakeDamage(smote);

            // get nearby enemies
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targets[0].Cell, 5f, true))
            {
                // stay in bounds
                if (!cell.InBounds(targets[0].Map)) continue;

                // epic loop
                Thing[] array = cell.GetThingList(targets[0].Map).ToArray();
                foreach (Thing thing in array)
                {
                    // make sure we do stuff to only pawns
                    if (thing.GetType() != typeof(Pawn)) continue;
                    Pawn pawn = thing as Pawn;

                    // skip pawns that are friendly to casters faction and the target
                    if (pawn == targets[0].Pawn) continue;
                    if (pawn.Faction != null && !pawn.Faction.HostileTo(caster.Faction)) continue;

                    // Add guy to get orbed
                    if (pawnsToOrb.Count < 6 && !pawn.DeadOrDowned)
                        pawnsToOrb.Add(pawn);
                }
            }

            // send out the holy orbs
            foreach (Pawn pawn in pawnsToOrb)
            {
                Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_Holy_Orb"), targets[0].Cell, targets[0].Map);
                FleckMaker.Static(pawn.Position, pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Target_Fleck"));
                projectile.Launch(targets[0].Pawn, targets[0].Pawn.DrawPos, pawn, pawn, ProjectileHitFlags.IntendedTarget);
            }
        }
    }

    public class Ability_HallowedGround : VFECore.Abilities.Ability
    {
        // ability 2: hallowed ground
        // sanctify the ground around oberon, enemies get burnt and irradiated

        List<IntVec3> sanctifiedGround = new List<IntVec3>();
        Sustainer sustainer;
        int ticksToEnd = 0;

        public override void Tick()
        {
            base.Tick();

            if (ticksToEnd != 0 && Find.TickManager.TicksGame > ticksToEnd)
            {
                sustainer.End();
                ticksToEnd = 0;
            }

            if (pawn.IsHashIntervalTick(60) && ticksToEnd != 0 && Find.TickManager.TicksGame < ticksToEnd) // Check every 60 ticks
            {
                if (sanctifiedGround.Count == 0) return;

                foreach (IntVec3 cell in sanctifiedGround)
                {
                    if (cell == null) continue;

                    // do fleck randomly
                    if (Rand.Chance(0.5f))
                        FleckMaker.Static(cell, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_FlameHoly_Fleck"));

                    // get list of things inside cell
                    Thing[] array = cell.GetThingList(base.pawn.Map).ToArray();
                    foreach (Thing thing in array)
                    {
                        // make sure we do stuff to only pawns
                        if (thing.GetType() != typeof(Pawn)) continue;
                        Pawn pawn = thing as Pawn;

                        // Check the faction of the pawn
                        if (pawn.Faction != null && pawn.Faction.HostileTo(base.pawn.Faction))
                        {
                            // For enemies
                            pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, 8f, 1f, -1f, base.pawn));
                            pawn.health.AddHediff(HediffDef.Named("WFP_Generic_Radiation"));
                        }
                    }
                }
            }
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // damage info
            DamageInfo smote = new DamageInfo(DamageDefOf.Burn, 10f, 1f, -1f, base.pawn);

            // clear the sanctified ground list
            sanctifiedGround.Clear();

            // save the area around oberon so we can tick it
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targets[0].Cell, 5f, true))
            {
                // stay in bounds
                if (!cell.InBounds(targets[0].Map)) continue;

                // add it to the list
                sanctifiedGround.Add(cell);
            }

            // Play cool sounds
            DefDatabase<SoundDef>.GetNamed("WF_HallowedGround_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));
            SoundDef soundDef = SoundDef.Named("WF_HallowedGround_Sustainer");
            sustainer = soundDef.TrySpawnSustainer(SoundInfo.InMap(base.pawn));

            // Make our cool mote
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            Mote obj = (Mote)ThingMaker.MakeThing(ThingDef.Named("Mote_HallowedGround"));
            obj.exactPosition = targets[0].Cell.ToVector3Shifted();
            obj.Scale = 12f;
            GenSpawn.Spawn(obj, targets[0].Cell, targets[0].Map);

            // start ticking
            ticksToEnd = Find.TickManager.TicksGame + 600;
        }
    }

    public class Ability_Renewal : VFECore.Abilities.Ability
    {
        // ability 3: renewal
        // cast a area wide effect that jumpstarts healing in allies bodies
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // iterate through nearby cells
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targets[0].Cell, 5f, true))
            {
                // stay in bounds
                if (!cell.InBounds(targets[0].Map)) continue;

                Thing[] array = cell.GetThingList(base.pawn.Map).ToArray();
                foreach (Thing thing in array)
                {
                    // make sure we do stuff to only pawns
                    if (thing.GetType() != typeof(Pawn)) continue;
                    Pawn pawn = thing as Pawn;

                    // Check the faction of the pawn
                    if (pawn.Faction != null && !pawn.Faction.HostileTo(base.pawn.Faction))
                    {
                        // For friends
                        pawn.health.AddHediff(HediffDef.Named("WFP_Generic_Healing"));
                    }
                }
            }

            // Cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_Renewal_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Renewal_Fleck"));
        }
    }

    public class Ability_Reckoning : VFECore.Abilities.Ability
    {
        // ability 4: reckoning
        // cast a area wide effect that throws enemies up in the air, and then hurts them when they land
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // iterate through nearby cells
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targets[0].Cell, 5f, true))
            {
                // stay in bounds
                if (!cell.InBounds(targets[0].Map)) continue;

                Thing[] array = cell.GetThingList(base.pawn.Map).ToArray();
                foreach (Thing thing in array)
                {
                    // make sure we do stuff to only pawns
                    if (thing.GetType() != typeof(Pawn)) continue;
                    Pawn pawn = thing as Pawn;

                    // Check the faction of the pawn
                    if (pawn.Faction != null && pawn.Faction.HostileTo(base.pawn.Faction))
                    {
                        // Deal some damage
                        DamageInfo impact = new DamageInfo(DamageDefOf.Blunt, 30f, 1f, -1f, base.pawn);
                        pawn.TakeDamage(impact);
                        if (pawn.DeadOrDowned) continue;

                        // do some effects
                        FleckMaker.Static(pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_FlameHoly_Fleck"));

                        // Make him jump
                        PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, pawn, pawn.Position, null, DefDatabase<SoundDef>.GetNamed("WF_RhinoStomp_Sound"), false, null, null, pawn);
                        if (pawnFlyer != null)
                        {
                            FleckMaker.ThrowDustPuff(pawn.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f), base.pawn.Map, 2f);
                            GenSpawn.Spawn(pawnFlyer, pawn.Position, base.pawn.Map);
                        }
                    }
                }
            }

            // Cool effects
            FleckMaker.Static(targets[0].Cell, targets[0].Map, DefDatabase<FleckDef>.GetNamed("WF_Reckoning_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_Reckoning_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
        }
    }
}
