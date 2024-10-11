using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace WarframePsycasts
{
    public class Ability_SlashDash : VFECore.Abilities.Ability
    {
        // ability 1: slash dash
        // pawn teleports to enemy and does a heavy slash

        private int tickCounter = 0;
        private List<Pawn> alreadyHitPawns = new List<Pawn>();

        public override void Tick()
        {
            // wonder what this does
            base.Tick();

            // if we are currently ticking AND we passed the time needed to attack again
            if (tickCounter != 0 && Find.TickManager.TicksGame >= tickCounter)
            {
                // stop ticking
                tickCounter = 0;

                // find a new target
                Pawn pawn = FindNewTarget(alreadyHitPawns);

                // if target exists
                if (pawn != null)
                {
                    // attack him and add to list so we dont target him again
                    AttackTarget(pawn);
                    alreadyHitPawns.Add(pawn);

                    // if caster is still alive, then start ticking again
                    if (base.pawn.DeadOrDowned == false)
                        tickCounter = Find.TickManager.TicksGame + 20;
                    else
                        tickCounter = 0;
                }
                // if target does not exist, stop ticking
                else
                {
                    tickCounter = 0;

                    // remove the dodge bonus
                    HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("WF_Generic_Dodge");
                    Hediff hediff = base.pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    if (hediff != null)
                        base.pawn.health.RemoveHediff(hediff);
                }
            }
                
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variable
            Pawn target = targets[0].Pawn;

            // create and add a hediff
            HediffDef hediff = HediffDef.Named("WF_Generic_Dodge");
            base.pawn.health.AddHediff(hediff);

            // clear the list so we can get new pawns to attack
            alreadyHitPawns.Clear();

            // attack the target
            AttackTarget(target);

            // add them to the list
            alreadyHitPawns.Add(target);

            // find a new target
            target = FindNewTarget(alreadyHitPawns);

            // if a target was found, move to Tick()
            if (target != null)
                tickCounter = Find.TickManager.TicksGame + 20;
            else
                tickCounter = 0;
        }

        public void AttackTarget(LocalTargetInfo target)
        {
            // make new damage info
            DamageInfo damageInfo = new DamageInfo(DamageDefOf.Cut, 25f, 1f, -1f, base.pawn);

            // teleport excalibur to the target
            base.pawn.Position = target.Pawn.Position;

            // try to stop the teleportation glitch
            base.pawn.Notify_Teleported();

            // deal damage to target pawn
            target.Pawn.TakeDamage(damageInfo);

            // cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_SlashBig_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_SlashDash_Sound").PlayOneShot(new TargetInfo(target.Cell, target.Pawn.Map, false));
        }

        public Pawn FindNewTarget(List<Pawn> alreadyHitPawns)
        {
            // Get current map
            Map map = base.pawn.Map;

            // find new target if it exists
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(base.pawn.Position, 10f, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> things = cell.GetThingList(map);
                foreach (Thing thing in things)
                {
                    if (thing is Pawn pawn)
                    {
                        // Make sure only target only living enemies
                        if (pawn == base.pawn || pawn.Downed || pawn.Dead || pawn.IsColonist) continue;
                        if (pawn.Faction != null && !pawn.Faction.HostileTo(base.pawn.Faction)) continue;
                        if (alreadyHitPawns.Contains(pawn)) continue;

                        return pawn;
                    }
                }
            }

            return null;
        }
    }

    public class Ability_RadialBlind : VFECore.Abilities.Ability
    {
        // ability 2: radial blind
        // pawn causes a blindness explosion around themselves
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // save current map
            Map map = base.pawn.Map;

            // cool effects
            FleckMaker.Static(base.pawn.Position, map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_RadialBlind_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, map, false));

            // find new target if it exists
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(base.pawn.Position, 5f, true))
            {
                // make sure cell is within bounds
                if (!cell.InBounds(map)) continue;

                // cool effect
                FleckMaker.Static(cell, map, DefDatabase<FleckDef>.GetNamed("WF_RadialBlind_Fleck"));

                List<Thing> things = cell.GetThingList(map);
                foreach (Thing thing in things)
                {
                    if (thing is Pawn pawn)
                    {
                        // Make sure only target pawns other than ourselves, dead pawns, and downed pawns
                        if (pawn == base.pawn || pawn.Dead || pawn.Downed || pawn.IsColonist) continue;
                        if (pawn.Faction != null && !pawn.Faction.HostileTo(base.pawn.Faction)) continue;

                        // Find distance between target
                        float distance = (pawn.Position - base.pawn.Position).LengthHorizontal;

                        // If distance is inside of radius
                        if (distance <= 5f)
                        {
                            pawn.stances.stunner.StunFor(GenTicks.SecondsToTicks(8f), base.pawn, addBattleLog: false);
                        }
                    }
                }
            }
        }
    }

    public class Ability_RadialJavelin : VFECore.Abilities.Ability
    {
        // ability 3: radial javelin
        // pawn throws 3 javelins at nearby enemies
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // save current map
            Map map = base.pawn.Map;

            // save list of pawns to hit later
            List<Pawn> pawnsToJavelin = new List<Pawn>();

            // cool effects
            FleckMaker.Static(base.pawn.Position, map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_RadialJavelin_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, map, false));

            // find new target if it exists
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(base.pawn.Position, 15f, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> things = cell.GetThingList(map);
                foreach (Thing thing in things)
                {
                    if (thing is Pawn pawn)
                    {
                        // Make sure only target pawns other than ourselves, dead pawns, and downed pawns
                        if (pawn == base.pawn || pawn.Dead || pawn.Downed || pawn.IsColonist) continue;
                        if (pawn.Faction != null && !pawn.Faction.HostileTo(base.pawn.Faction)) continue;

                        if (pawnsToJavelin.Count < 3)
                            pawnsToJavelin.Add(pawn);
                    }
                }
            }

            foreach (Pawn pawn in pawnsToJavelin)
            {
                Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_Radial_Javelin"), pawn.Position, map);
                FleckMaker.Static(pawn.Position, map, DefDatabase<FleckDef>.GetNamed("WF_Target_Fleck"));
                projectile.Launch(base.pawn, base.pawn.DrawPos, pawn, pawn, ProjectileHitFlags.IntendedTarget);
            }
        }
    }

    public class Ability_ExaltedBlade : VFECore.Abilities.Ability
    {
        // ability 4: exalted blade
        // pawn drops his weapon and equips a new exalted blade
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_ExaltedBlade_Sound").PlayOneShot(new TargetInfo(base.pawn.Position, base.pawn.Map, false));

            // drop current weapon if it exists
            if (base.pawn.equipment.Primary != null)
                base.pawn.equipment.TryDropEquipment(base.pawn.equipment.Primary, out ThingWithComps resultingWep, base.pawn.Position, true);

            // equip a cool new sword
            ThingWithComps exaltedSword = (ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Milkwater_Exalted_Blade"), null);
            base.pawn.equipment.AddEquipment(exaltedSword);
        }
    }
}
