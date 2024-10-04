using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using VanillaPsycastsExpanded;

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

        internal bool shieldIsOn = false;
        internal const float maxDuration = 100f;
        internal float currentDuration = 0f;
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            if (!shieldIsOn)
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
                //Maybe Hediff_Overshield from VPE - Protector Tree - Overshield

            }

            else
            {
                //Cancel shield

                //Launch projectile
            }
        }

        public override void Tick()
        {
            base.Tick();

            if(shieldIsOn)
            {
                if(--currentDuration <= 0)
                {

                }
            }
        }
    }

    public class Ability_BloodAltar : VFECore.Abilities.Ability
    {
        /*
         * Spikes an enemy and creates a healing alter at their location.  
         * See:
         *  Ability_GroupLink
         *      Hediff_GroupLink
         *          LinkAllPawnsAround()
         * */
        //public override Hediff ApplyHediff(Pawn targetPawn, HediffDef hediffDef, BodyPartRecord bodyPart, int duration, float severity)
        //{
        //    //Hediff_GroupLink hediff_GroupLink = base.ApplyHediff(targetPawn, hediffDef, bodyPart, duration, severity) as Hediff_GroupLink;
        //    //hediff_GroupLink.LinkAllPawnsAround();
        //    //return hediff_GroupLink;
        //}

        public Building_BloodAltar altar = null;
        public const float strikeRadius = 7f;
        //public const float healRate 

        //public void 


    }

    public class Building_BloodAltar : Building
    {

        public Pawn owner;
        public VFECore.Abilities.Ability sourceAbility;
        public List<Pawn> linkedPawns = new List<Pawn>();
        public const float effectRadius = 5f;
        public void GetSurroundingPawns()
        {
            //Borrowing from: Hediff_GroupLink.LinkAllPawnsAround()
            foreach (Pawn item in from x in GenRadial.RadialDistinctThingsAround(this.Position, this.Map, effectRadius, true).OfType<Pawn>()
                                  where x.RaceProps.Humanlike && !x.Faction.HostileTo(Faction.OfPlayer)
                                  select x)
            {
                bool flag = !this.linkedPawns.Contains(item);
                if (flag)
                {
                    this.linkedPawns.Add(item);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            for(int i = 0; i < linkedPawns.Count; i++)
            {
                Pawn p = linkedPawns[i];
                bool flag = p.Map != this.Map ||
                    p.Position.DistanceTo(this.Position) > effectRadius;
                if (flag)
                {
                    linkedPawns.RemoveAt(i);
                    i--;
                }

//TODO: Need WF_Garuda_BloodAltarHealing based on VPE_DefOf.VPE_GainedVitality
                HediffDef hediffDef = HediffDef.Named("WF_Garuda_BloodAltarHealing");
                p.health.GetOrAddHediff(hediffDef);
            }

            
        }
    }

    public class Ability_Bloodletting : VFECore.Abilities.Ability
    {
        /*
         * Pawn sacrifices health for psyfocus.  Sounds simple enough; doubt it actually will be.  
         * Per convo: "For bloodletting I was thinking the pawn would slash himself for like 10 damage, and gain x% psyfocus and removes bad hediffs"
         * */
        public const float selfDamage = 10f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            DamageInfo selfDamageInfo = new DamageInfo(DamageDefOf.Cut, selfDamage, 1f, -1f, pawn);

            targets[0].Pawn.TakeDamage(selfDamageInfo);

            //Two possible mods that could contain hints on methods for regaining psyfocus: Hemosage and Anima Obelisk

        }

    }

    public class Ability_SeekingTalons : VFECore.Abilities.Ability
    {
        /*
         * Radial slash.  Strong, but expensive.
         * Probably won't be all that different from Radial Javelin, except all pawns in range, and range is centered on Garuda
         * Maybe a semi-long chargeup, too
         * */
    }
}
