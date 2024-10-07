using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;

namespace WarframePsycasts
{
    public class Ability_Decoy : VFECore.Abilities.Ability
    {
        // ability 1: decoy
        // spawn a friendly decoy that can shoot at enemies

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;

            // create and spawn our decoy
            GenSpawn.Spawn(PawnGenerator.GeneratePawn(PawnKindDef.Named("WF_Loki_Decoy"), caster.Faction), targets[0].Cell, caster.Map);

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(targets[0].Cell, targets[0].Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_Decoy_Sound").PlayOneShot(new TargetInfo(targets[0].Cell, targets[0].Map, false));
        }
    }

    public class Ability_Invisibility : VFECore.Abilities.Ability
    {
        // ability 2: invisibility
        // add hediff to caster that makes him invisible
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variable
            Pawn target = targets[0].Pawn;

            // giving the hediff is done in the xml file

            // cool effects
            FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(target.Position, target.Map, DefDatabase<FleckDef>.GetNamed("WF_Invisibility_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_Invisibility_Sound").PlayOneShot(new TargetInfo(target.Position, target.Map, false));
        }
    }

    public class Ability_SwitchTeleport : VFECore.Abilities.Ability
    {
        // ability 3: switch teleport
        // switch position of caster and target, then stun target
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // create variables
            Pawn caster = base.pawn;
            Pawn target = targets[0].Pawn;
            IntVec3 tempPos = target.Position;

            // swap the two guys
            target.Position = caster.Position;
            target.Notify_Teleported();
            caster.Position = tempPos;
            caster.Notify_Teleported();

            // stun the bad one
            target.stances.stunner.StunFor(GenTicks.SecondsToTicks(8f), caster, addBattleLog: false);

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_SwitchTeleport_Sound").PlayOneShot(new TargetInfo(target.Position, target.Map, false));
        }
    }

    public class Ability_RadialDisarm : VFECore.Abilities.Ability
    {
        // ability 4: radial disarm
        // make every enemy drop their weapon around loki

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // what does this do
            base.Cast(targets);

            // epic loop
            Pawn caster = base.pawn;
            Map map = caster.Map;

            // find new target if it exists
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, 5f, true))
            {
                if (!cell.InBounds(map)) continue;
                Thing[] array = cell.GetThingList(caster.Map).ToArray();
                foreach (Thing thing in array)
                {
                    if (thing is Pawn pawn)
                    {
                        // Make sure only target pawns other than ourselves, dead pawns, and downed pawns
                        if (pawn == base.pawn || pawn.Dead || pawn.Downed || pawn.IsColonist) continue;
                        if (pawn.Faction != null && !pawn.Faction.HostileTo(caster.Faction)) continue;

                        // does the weapon exist
                        if (pawn.equipment.Primary != null)
                        {
                            // cool effect
                            FleckMaker.Static(pawn.Position, pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Disarm_Fleck"));

                            // drop it
                            ThingWithComps weapon = pawn.equipment.Primary;
                            pawn.equipment.TryDropEquipment(weapon, out ThingWithComps droppedWeapon, pawn.Position, false);
                            pawn.equipment.Notify_EquipmentRemoved(droppedWeapon);
                        }
                    }
                }
            }

            // cool effects
            FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.PsycastAreaEffect);
            FleckMaker.Static(caster.Position, caster.Map, DefDatabase<FleckDef>.GetNamed("WF_RadialDisarmBig_Fleck"));
            DefDatabase<SoundDef>.GetNamed("WF_RadialDisarm_Sound").PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
            Find.CameraDriver.shaker.DoShake(10f);
        }
    }
}
