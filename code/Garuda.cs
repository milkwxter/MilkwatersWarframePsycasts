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

namespace WarframePsycasts
{
    public class Ability_DreadMirror : VFECore.Abilities.Ability
    {
        /*
         * Effectively, pawn teleports to target and "rips" lifeforce from them to create a shield.  Wiki describes damage as 40% (I think)
         * If cast again, sends out damage absorbed as damage to target.
         * Steps:
         *  1. Skip to enemy
         *  2. Damage enemy
         *  3. Give caster a shield
         *  4. Wait for shield to drain, time to run out, or second cast
         *  
        */

        //internal bool shieldIsOn = false;
        internal bool ShieldIsOn { get
            {
                return pawn.health.hediffSet.HasHediff(HediffDef.Named("WF_Garuda_DreadMirror"));
            } }
        internal const float maxDuration = 100f;
        internal float currentDuration = 0f;
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            if (!ShieldIsOn)
            {
                //Skip to target
                //Borrowing from VPE Killskip, "AttackTarget"
                pawn.Position = targets[0].Cell;
                pawn.Notify_Teleported(false, true);
                pawn.stances.SetStance(new Stance_Mobile());

                //Damage enemy
                DamageInfo ripDamageInfo = new DamageInfo(DamageDefOf.Psychic, 25f, 1f, -1f, pawn);
                targets[0].Pawn.TakeDamage(ripDamageInfo);

                //Give caster a shield
                pawn.health.GetOrAddHediff(HediffDef.Named("WF_Garuda_DreadMirror"));
                //currentDuration = maxDuration;
                //Note: Right now there's nothing to make the shield ever go away.
            }

            else
            {
                //Cancel shield
                Hediff shieldHediff = pawn.health.GetOrAddHediff(HediffDef.Named("WF_Garuda_DreadMirror"));
                pawn.health.RemoveHediff(shieldHediff);

                //Launch projectile
                Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Milkwater_Projectile_DreadMirror"), pawn.Position, pawn.Map);
                FleckMaker.Static(pawn.Position, pawn.Map, DefDatabase<FleckDef>.GetNamed("WF_Target_Fleck"));
                projectile.Launch(pawn, pawn.DrawPos, targets[0].Pawn, targets[0].Pawn, ProjectileHitFlags.IntendedTarget);
                //DamageInfo pdi = new DamageInfo(DamageDefOf.Psychic, 25f, 1f, -1f, pawn);
                

            }
        }
    }
    #region BloodAltar
    public class Ability_BloodAltar : VFECore.Abilities.Ability
    {
        /*
         * Spikes an enemy and creates a healing alter at their location.  
         * See:Ability_GroupLink->Hediff_GroupLink->LinkAllPawnsAround()
         * */

        public Building_BloodAltar altar = null;
        public const float strikeRadius = 7f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            //Check if there's already a blood altar present
            //List<Building_BloodAltar> BAList = pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_BloodAltar>().ToList();
            //if (BAList.Count > 0)
            //{
            //    for (int i = 0; i < BAList.Count; i++)
            //    {
            //        if (BAList[i].owner == pawn)
            //        {
            //            return;
            //        }
            //    }
            //}
            //I don't like this.  Let's see if we can grey out the gizmo instead.  
            //Second guessing: WF's Garuda can have up to 3.  

            pawn.Position = targets[0].Cell;
            pawn.Notify_Teleported(false, true);
            pawn.stances.SetStance(new Stance_Mobile());

            targets[0].Pawn.health.GetOrAddHediff(HediffDef.Named("WF_Garuda_BloodAltarSpiked"));
            //Spiked should add Movement max 0 and possibly manipulation max 0.  If that second one causes trouble, then manipulation max 5%

            //Spawn Building_BloodAltar
            //Building should be fully walkable, to prevent it from moving the host pawn out of the way.  
            Thing thingAltar = GenSpawn.Spawn(WF_ThingDefOf.Building_BloodAltar, targets[0].Cell, targets[0].Map, WipeMode.Vanish);
            Building_BloodAltar altar = (Building_BloodAltar) thingAltar;
            altar.owner = pawn;
            altar.sourceAbility = this;
            altar.remainingDuration = Building_BloodAltar.maxDuration;

            altar.ShowRadius();
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
            //Borrowing from: Hediff_GroupLink.LinkAllPawnsAround()
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
                p.health.GetOrAddHediff(HediffDef.Named("WF_Garuda_BloodAltarHealing"));
            }
        }
        public override void Tick()
        {
            base.Tick();
            //Log.Message("BATick, " + remainingDuration.ToString());
            if(--remainingDuration <= 0)
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
            obj.exactPosition = owner.Position.ToVector3Shifted();
            obj.Scale = 7f;
            GenSpawn.Spawn(obj, this.Position, this.Map);
            //FleckMaker.Static(owner.Position, owner.Map, DefDatabase<FleckDef>.GetNamed("WF_BloodAltarRadius_Fleck"));
        }
    }
    #endregion

    /*
     * Testing successful.  
     */
    public class Ability_Bloodletting : VFECore.Abilities.Ability
    {
        /*
         * Pawn sacrifices health for psyfocus and status (bad, non-injury hediffs) clearing
         * */
        public const float selfDamage = 10f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            //Sacrifice health.  Shouldn't be lethal.
            //NOTE: One way to balance could be through blood loss not being healed.  
            DamageInfo selfDamageInfo = new DamageInfo(DamageDefOf.Cut, selfDamage, 1f, -1f, pawn);
            selfDamageInfo.SetBodyRegion(BodyPartHeight.Bottom);
            pawn.TakeDamage(selfDamageInfo);

            //Gain psyfocus
            pawn.psychicEntropy.OffsetPsyfocusDirectly(.2f);


            //Remove bad hediffs
            HediffSet set = pawn.health.hediffSet;
            List<Hediff> hList = new List<Hediff>();
            set.GetHediffs<Hediff>(ref hList);
            List<Hediff> removeList = hList.Where(h => h.def.isBad).ToList();
            //Pare down list
            removeList = removeList.Where(h => h.GetType() != typeof(Hediff_Injury)).ToList();
            //Actual removal
            for(int i = removeList.Count-1; i >= 0; i--)
            {
                pawn.health.RemoveHediff(removeList[i]);
            }
        }

    }

    public class Ability_SeekingTalons : VFECore.Abilities.Ability
    {
        public float range = 7f;
        public float talonDamage = 20f;

        /*
         * Radial slash.  Strong, but expensive.
         * Maybe a semi-long chargeup, too
         * */
        public override void Cast(params GlobalTargetInfo[] targets) 
        { 
            base.Cast(targets);

            //Borrowing from Oberon's Reckoning
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targets[0].Cell, range, true))
            {
                if (!cell.InBounds(targets[0].Map)) continue;

                FleckMaker.Static(base.pawn.Position, base.pawn.Map, FleckDefOf.PsycastAreaEffect);
                Mote obj = (Mote)ThingMaker.MakeThing(ThingDef.Named("Mote_SeekingTalons"));
                obj.exactPosition = pawn.Position.ToVector3Shifted();
                obj.Scale = 7f;
                GenSpawn.Spawn(obj, pawn.Position, pawn.Map);


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
                        DamageInfo clawsDamageInfo = new DamageInfo(DamageDefOf.Cut, talonDamage, 1f, -1f, base.pawn);
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
        }
    }
}
