using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine; 

namespace Mgaazines
{
    public class BipodSettings : ModSettings
    {
       
        public bool NerfBuffBool;
        public bool Testingmode;



        public override void ExposeData()
        {
            Scribe_Values.Look(ref NerfBuffBool, "NerfBuffBool");
            Scribe_Values.Look(ref Testingmode, "testmode");

            base.ExposeData();
        }
    }

    public class BipodMod : Mod
    {
       
        BipodSettings settings;

        
        public BipodMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<BipodSettings>();
        }

        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            if (settings.NerfBuffBool)
            {
                listingStandard.CheckboxLabeled("Make bipods a nerf (guns without bipod setup have 2 of normal warmup time, with they have 1)", ref settings.NerfBuffBool, "Make bipods a nerf");
            }
            if (!settings.NerfBuffBool)
            {
                listingStandard.CheckboxLabeled("Make bipods a buff (guns without bipod set up have normal warmup time, with they have half of it) ", ref settings.NerfBuffBool, "Make bipods a buff");
            }
            if (settings.Testingmode)
            {
                listingStandard.CheckboxLabeled("Turn off testing mode (enable Log.Error and Log.Message parst of code)", ref settings.Testingmode, "Turn off testing mode");
            }
            if (!settings.Testingmode)
            {
                listingStandard.CheckboxLabeled("Turn on testing mode (enable Log.Error and Log.Message parst of code)", ref settings.Testingmode, "Turn on testing mode");
            }


            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

       
        public override string SettingsCategory()
        {
            return "Bipod mod".Translate();
        }
    }
}
