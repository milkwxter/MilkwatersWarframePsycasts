using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using VFECore.Abilities;
using Verse.Noise;
using Verse.Sound;
using static UnityEngine.GraphicsBuffer;

namespace WarframePsycasts
{
    public class Ability_DreadMirror : VFECore.Abilities.Ability
    {
        internal const float maxDuration = 100f;
        internal float currentDuration = 0f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // do standard psycast stuff
            base.Cast(targets);

            // if there is no shield, do this
            Hediff hediffShield = base.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("WFP_Garuda_DreadMirror"));
            if (hediffShield == null)
            {
                // skip to target
                pawn.Position = targets[0].Cell;
                pawn.Notify_Teleported(false, true);
                pawn.stances.SetStance(new Stance_Mobile());

                // damage enemy
                DamageInfo ripDamageInfo = new DamageInfo(DamageDefOf.Cut, 25f, 1f, -1f, pawn);
                targets[0].Pawn.TakeDamage(ripDamageInfo);

                // give caster a shield
                pawn.health.GetOrAddHediff(HediffDef.Named("WFP_Garuda_DreadMirror"));
                DefDatabase<SoundDef>.GetNamed("WF_DreadMirrorShield_Sound").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
            }
            else
            {
                // cancel shield if it exists
                HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("WFP_Garuda_DreadMirror");
                Hediff hediff = base.pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                    base.pawn.health.RemoveHediff(hediff);

                // launch projectile
                Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_DreadMirror"), pawn.Position, pawn.Map);
                FleckMaker.Static(pawn.Position, pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Target_Fleck"));
                projectile.Launch(pawn, pawn.DrawPos, targets[0].Pawn, targets[0].Pawn, ProjectileHitFlags.IntendedTarget);
            }

            // cool effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_DreadMirror_Sound").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
        }
    }

    public class Ability_BloodAltar : VFECore.Abilities.Ability
    {
        public Building_BloodAltar altar = null;
        public const float strikeRadius = 7f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            // do standard psycast stuff
            base.Cast(targets);

            // teleport to target
            pawn.Position = targets[0].Cell;
            pawn.Notify_Teleported(false, true);
            pawn.stances.SetStance(new Stance_Mobile());
            
            // spike him
            targets[0].Pawn.health.GetOrAddHediff(HediffDef.Named("WFP_Garuda_BloodAltarSpiked"));

            // spawn Building_BloodAltar 
            Thing thingAltar = GenSpawn.Spawn(WF_ThingDefOf.Building_BloodAltar, targets[0].Cell, targets[0].Map, WipeMode.Vanish);
            Building_BloodAltar altar = (Building_BloodAltar) thingAltar;
            altar.owner = pawn;
            altar.sourceAbility = this;
            altar.remainingDuration = Building_BloodAltar.maxDuration;

            // cool effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_BloodAltar_Sound").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
        }
    }

    public class Building_BloodAltar : Building
    {

        public Pawn owner;
        public VFECore.Abilities.Ability sourceAbility;
        public List<Pawn> linkedPawns = new List<Pawn>();
        public const float effectRadius = 5f;
        public const int maxDuration = 3000;
        public int remainingDuration = 0;
        public bool GetSurroundingPawns()
        {
            bool changeFlag = false;
            foreach (Pawn item in from x in GenRadial.RadialDistinctThingsAround(this.Position, this.Map, effectRadius, true).OfType<Pawn>()
                                  where x.RaceProps.Humanlike && !x.Faction.HostileTo(Faction.OfPlayer)
                                  select x)
            {
                bool flag = !this.linkedPawns.Contains(item);
                if (flag)
                {
                    this.linkedPawns.Add(item);
                    changeFlag = true;
                }
            }
            return changeFlag;
        }

        public void SpreadHediffToAll()
        {
            foreach (Pawn p in linkedPawns)
            {
                p.health.GetOrAddHediff(HediffDef.Named("WFP_Garuda_BloodAltarHealing"));
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (this.IsHashIntervalTick(60))
            {
                this.ShowRadius();
            }
            if (--remainingDuration <= 0)
            {
                linkedPawns.Clear();
                this.Destroy();
                return;
            }
            bool changedFlag = false;
            for(int i = 0; i < linkedPawns.Count; i++)
            {
                Pawn p = linkedPawns[i];
                bool removeFlag = p.Map != this.Map ||
                    p.Position.DistanceTo(this.Position) > effectRadius;
                if (removeFlag)
                {
                    linkedPawns.RemoveAt(i);
                    i--;
                    changedFlag = true;
                }
            }
            if (GetSurroundingPawns())
            {
                changedFlag = true;
            }
            if (changedFlag)
            {
                SpreadHediffToAll();
            }            
        }

        public void ShowRadius()
        {
            FleckMaker.Static(this.Position, this.Map, FleckDefOf.PsycastAreaEffect);
            Mote obj = (Mote)ThingMaker.MakeThing(ThingDef.Named("Mote_BloodAltarRadius"));
            obj.exactPosition = this.Position.ToVector3Shifted();
            obj.Scale = 7f;
            GenSpawn.Spawn(obj, this.Position, this.Map);
        }
    }

    public class Ability_Bloodletting : VFECore.Abilities.Ability
    {
        public const float selfDamage = 10f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            // sacrifice health.  Shouldn't be lethal.
            DamageInfo selfDamageInfo = new DamageInfo(DamageDefOf.Cut, selfDamage, 1f, -1f, pawn);
            selfDamageInfo.SetBodyRegion(BodyPartHeight.Bottom);
            pawn.TakeDamage(selfDamageInfo);

            // gain psyfocus
            pawn.psychicEntropy.OffsetPsyfocusDirectly(.2f);

            // get list of bad hediffs
            HediffSet set = pawn.health.hediffSet;
            List<Hediff> hList = new List<Hediff>();
            set.GetHediffs<Hediff>(ref hList);
            List<Hediff> removeList = hList.Where(h => h.def.isBad).ToList();

            // remove injuries from list
            removeList = removeList.Where(h => h.GetType() != typeof(Hediff_Injury)).ToList();

            // remove bad hediffs from pawn
            for(int i = removeList.Count - 1; i >= 0; i--)
            {
                pawn.health.RemoveHediff(removeList[i]);
            }

            // cool effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
            DefDatabase<SoundDef>.GetNamed("WF_Bloodletting_Sound").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
        }

    }

    public class Ability_SeekingTalons : VFECore.Abilities.Ability
    {
        public float range = 7f;
        public float talonDamage = 33f;

        public override void Cast(params GlobalTargetInfo[] targets) 
        { 
            base.Cast(targets);

            // create a damage info
            DamageInfo clawsDamageInfo = new DamageInfo(DamageDefOf.Cut, talonDamage, 1f, -1f, base.pawn);

            // for each cell around garuda
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targets[0].Cell, range, true))
            {
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
                        pawn.TakeDamage(clawsDamageInfo);
                        if (pawn.DeadOrDowned) continue;

                        // do some effects
                        FleckMaker.Static(pawn.Position, base.pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_GarudaSlash_Fleck"));

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

            // cool effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
            Mote obj = (Mote)ThingMaker.MakeThing(ThingDef.Named("Mote_SeekingTalonsRadius"));
            obj.exactPosition = pawn.Position.ToVector3Shifted();
            obj.Scale = 16f;
            GenSpawn.Spawn(obj, pawn.Position, pawn.Map);
            DefDatabase<SoundDef>.GetNamed("WF_SeekingTalons_Sound").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
        }
    }
}
