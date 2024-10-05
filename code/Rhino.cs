using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WarframePsycasts
{
    public class Ability_RhinoCharge : VFECore.Abilities.Ability
    {
        // ability 1: rhino charge
        // pawn dashes to a target, goring them. anyone who stands in their way will be clobbered.

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;
            Pawn target = targets[0].Pawn;

            // make new damage info
            DamageInfo clobbered = new DamageInfo(DamageDefOf.Blunt, 5f, 1f, - 1f, base.pawn);
            DamageInfo gored = new DamageInfo(DamageDefOf.Stab, 35f, 1f, -1f, base.pawn);

            // cool effects
            FleckMaker.Static(target.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_RhinoCharge_Sound").PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
            Find.CameraDriver.shaker.DoShake(1f);

            // get path to traverse
            List<IntVec3> tiles = GetTilesBetweenPoints(caster.Position, target.Position);

            // iterate over traversal path
            foreach (IntVec3 tile in tiles)
            {
                // stay in bounds
                if (!tile.InBounds(caster.Map)) continue;

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 2f, true))
                {
                    // stay in bounds
                    if (!cell.InBounds(caster.Map)) continue;

                    // cool effect
                    FleckMaker.Static(cell, caster.Map, DefDatabase<FleckDef>.GetNamed("WF_Dust_Fleck"));

                    // epic loop
                    Thing[] array = cell.GetThingList(caster.Map).ToArray();
                    foreach (Thing thing in array)
                    {
                        // make sure we do stuff to only pawns
                        if (thing.GetType() != typeof(Pawn)) continue;

                        // make sure we only do stuff to people that we dont like
                        Pawn pawn = thing as Pawn;
                        if (pawn == caster || pawn.IsColonist) continue;
                        if (pawn.Faction != null && !pawn.Faction.HostileTo(caster.Faction)) continue;

                        // clobber them
                        pawn.TakeDamage(clobbered);
                    }
                }
                // teleport rhino to the spot
                caster.Position = tile;
            }

            // gore the target
            target.TakeDamage(gored);
        }

        public List<IntVec3> GetTilesBetweenPoints(IntVec3 start, IntVec3 end)
        {
            List<IntVec3> tiles = new List<IntVec3>();

            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.z - start.z);

            int sx = start.x < end.x ? 1 : -1;
            int sy = start.z < end.z ? 1 : -1;

            int err = dx - dy;

            int x = start.x;
            int z = start.z;

            while (true)
            {
                tiles.Add(new IntVec3(x, 0, z));

                if (x == end.x && z == end.z)
                    break;

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    z += sy;
                }
            }

            return tiles;
        }
    }

    public class Ability_IronSkin : VFECore.Abilities.Ability
    {
        // ability 2: iron skin
        // add hediff to caster that reduces damage
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variable
            Pawn target = targets[0].Pawn;

            // create the hediff
            HediffDef hediff = HediffDef.Named("WF_Rhino_IronSkin");

            // add the hediff
            target.health.AddHediff(hediff);

            // cool effects
            FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(target.Position, target.Map, DefDatabase<FleckDef>.GetNamed("WF_IronSkin_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_IronSkin_Sound").PlayOneShot(new TargetInfo(target.Position, target.Map, false));
        }
    }

    public class Ability_Roar : VFECore.Abilities.Ability
    {
        // ability 3: roar
        // add hediff to caster and those in range that increases damage
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create the hediff
            HediffDef hediff = HediffDef.Named("WF_Rhino_Roar");

            // get the pawns
            Pawn caster = base.pawn;
            Pawn target = targets[0].Pawn;

            // add the hediff
            caster.health.AddHediff(hediff);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 10f, true))
            {
                // epic loop
                Thing[] array = cell.GetThingList(caster.Map).ToArray();
                foreach (Thing thing in array)
                {
                    // make sure we do stuff to only pawns
                    if (thing.GetType() != typeof(Pawn)) continue;

                    // make sure we only do stuff to people that we like
                    Pawn pawn = thing as Pawn;
                    if (pawn == caster || !pawn.Faction.HostileTo(caster.Faction))
                    {
                        // add hediff
                        pawn.health.AddHediff(hediff);

                        // cool effect
                        FleckMaker.Static(pawn.Position, pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Roar_Fleck"));
                    }
                }
            }

            // cool effects
            FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(target.Position, target.Map, DefDatabase<FleckDef>.GetNamed("WF_RoarBig_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_Roar_Sound").PlayOneShot(new TargetInfo(target.Position, target.Map, false));
            Find.CameraDriver.shaker.DoShake(3f);
        }
    }

    public class Ability_RhinoStomp : VFECore.Abilities.Ability
    {
        // ability 4: rhino stomp
        // stomp and damage enemies in range

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // epic loop
            Pawn caster = base.pawn;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 5f, true))
            {
                // stay in bounds
                if (!cell.InBounds(caster.Map)) continue;

                // cool effect
                FleckMaker.Static(cell, caster.Map, DefDatabase<FleckDef>.GetNamed("WF_Dust_Fleck"));

                // epic loop
                Thing[] array = cell.GetThingList(caster.Map).ToArray();
                foreach (Thing thing in array)
                {
                    // make sure we do stuff to only pawns
                    if (thing.GetType() != typeof(Pawn)) continue;

                    // make sure we only do stuff to people that we hate
                    Pawn pawn = thing as Pawn;
                    if (pawn == caster || pawn.IsColonist) continue;
                    if (pawn.Faction != null && !pawn.Faction.HostileTo(caster.Faction)) continue;

                    // deal damage
                    DamageInfo clobbered = new DamageInfo(DamageDefOf.Blunt, 20f, 1f, -1f, base.pawn);
                    pawn.TakeDamage(clobbered);
                }
            }

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_RhinoStomp_Sound").PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
            Find.CameraDriver.shaker.DoShake(10f);
        }
    }
}
