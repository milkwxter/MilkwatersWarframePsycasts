using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static UnityEngine.GraphicsBuffer;

namespace WarframePsycasts
{
    public class Ability_Fireball : VFECore.Abilities.Ability
    {
        // ability 1: fireball
        // cast a firery projectile that explodes on impact

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create our variables
            Pawn caster = base.pawn;

            // Create our projectile
            Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_Fireball"), caster.Position, caster.Map);
            projectile.Launch(caster, caster.DrawPos, targets[0].Cell, targets[0].Cell, ProjectileHitFlags.IntendedTarget);
        }
    }

    public class Ability_Immolation : VFECore.Abilities.Ability
    {
        // ability 2: immolation
        // add hediff to caster that makes him immune to fire damage, and plenty of armor
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;

            // hediff is given inside the xml file

            // Cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Immolation_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_Immolation_Sound").PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
        }
    }

    public class Ability_FireBlast : VFECore.Abilities.Ability
    {
        // ability 3: fire blast
        // switch position of caster and target, then stun target
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;
            DamageInfo burnt = new DamageInfo(DamageDefOf.Burn, 10f, 1f, -1f, base.pawn);
            Random random = new Random();

            // epic loop
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 5f, true))
            {
                // cool effect
                FleckMaker.Static(cell, caster.Map, DefDatabase<FleckDef>.GetNamed("WF_Flame_Fleck"));

                // try to spawn a fire
                int randomNumber = random.Next(1, 100);
                if (randomNumber < 30)
                    FireUtility.TryStartFireIn(cell, caster.Map, 10f, caster);

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

                    // give hediff
                    pawn.health.AddHediff(HediffDef.Named("WF_Generic_ArmorStrip"));

                    // deal two instances of damage
                    pawn.TakeDamage(burnt);
                    pawn.TakeDamage(burnt);
                }
            }

            // Cool effects
            FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_FireBlast_Sound").PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
            Find.CameraDriver.shaker.DoShake(10f);
        }
    }

    public class Ability_Inferno : VFECore.Abilities.Ability
    {
        // ability 4: inferno
        // drop tons of meteors from the top of the map to the target

        int tickCounter = 0;
        IntVec3 targetCell;
        int cometsLaunched = 0;

        public override void Tick()
        {
            // wonder what this does
            base.Tick();

            // Make a position for the comets to spawn
            IntVec3 cometOrigin = new IntVec3(0, 0, base.pawn.Map.Size.z - 1);

            // if we are currently ticking AND we passed the time needed to attack again
            if (tickCounter != 0 && Find.TickManager.TicksGame >= tickCounter && cometsLaunched < 5)
            {
                // stop ticking
                tickCounter = 0;

                // Create our projectile and launch it
                Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_Comet"), cometOrigin, base.pawn.Map);
                projectile.Launch(base.pawn, targetCell, targetCell, ProjectileHitFlags.IntendedTarget);

                // update some variables
                cometsLaunched++;
                tickCounter = Find.TickManager.TicksGame + 20;
            }

        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // set variables to move to Tick()
            tickCounter = Find.TickManager.TicksGame + 20;
            targetCell = targets[0].Cell;
            cometsLaunched = 0;
        }
    }
}
