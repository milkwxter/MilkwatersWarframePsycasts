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
}
