using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI.Group;
using Verse;

namespace WarframePsycasts
{
    public class DeathActionWorker_RemoveBody : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            // cool effect
            FleckMaker.Static(corpse.Position, corpse.Map, FleckDefOf.PsycastAreaEffect);
            
            // bye bye corpse
            corpse.Destroy();
        }
    }

    [DefOf]
    public static class WF_ThingDefOf 
    {
        static WF_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WF_ThingDefOf));
        }

        public static ThingDef BloodAltar;
    }
}
