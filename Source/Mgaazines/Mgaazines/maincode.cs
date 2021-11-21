using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatExtended;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;


namespace Mgaazines
{
	[DefOf]
	public class BipodDefsOfs : DefOf
	{
		public static JobDef setupbipodjobdef;
	}

	public class BipedProps : CompProperties
	{


		public int additionalrange = 12;

		public float recoilmulton = 0.5f;

		public float recoilmultoff = 1f;

		public float warmupmult = 0.85f;

		public float warmuppenalty = 2f;

		public int TicksToSetUp = 60;



		public BipedProps()
		{
			this.compClass = typeof(bipodcomp);
		}

		public BipedProps(Type compClass) : base(compClass)
		{
			this.compClass = compClass;
		}

	}
	public class bipodcomp : CompRangedGizmoGiver
	{
		public bool ShouldSetUp;

		public bool IsSetUpRn;

		public ThingWithComps BipodAttached;

		public override void PostExposeData()
		{
			Scribe_References.Look(ref BipodAttached, "BipodAttached");
			base.PostExposeData();
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if(this.ParentHolder is Pawn_EquipmentTracker)
			{
				Pawn dad = ((Pawn_EquipmentTracker)this.ParentHolder).pawn;
				if (dad.Drafted)
				{
					if (!ShouldSetUp)
					{
						yield return new Command_Action
						{
							action = delegate { ShouldSetUp = true; },
							defaultLabel = "Set up bipod",
							icon = ContentFinder<Texture2D>.Get("Tymon/Bipod/open_bipod")
						};
					}
					else
					{
						yield return new Command_Action
						{
							action = delegate { ShouldSetUp = false; },
							defaultLabel = "Close bipod",
							icon = ContentFinder<Texture2D>.Get("Tymon/Bipod/closed_bipod")
						};

					}
				}
				
			}
			

		}
		public VerbBipodShoot verbbipod
		{
			get
			{
				var result = (VerbBipodShoot)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
				return result;
			}
		}
		public override void Notify_Unequipped(Pawn pawn)
		{

			IsSetUpRn = false;
		}
		public override void Notify_Equipped(Pawn pawn)
		{
			IsSetUpRn = false;
			verbbipod.WereChangesApplied1 =	false;
			verbbipod.WereChangesApplied2 = false;
		}

		public BipedProps Props => (BipedProps)this.props;
		public void SetUpStart(Pawn pawn = null)
		{
			if (pawn != null)
			{
				IntVec3 pawnpos = pawn.Position;
				List<IntVec3> pawnsurs = pawn.CellsAdjacent8WayAndInside().ToList();
				pawnsurs.Remove(pawnpos);
				var surthings = new List<Thing>();
				foreach (IntVec3 cell in pawnsurs)
				{
					surthings.AddRange(cell.GetThingList(Find.CurrentMap));
				}
				pawn.jobs.StopAll();
				pawn.jobs.StartJob(new Job { def = BipodDefsOfs.setupbipodjobdef, targetA = this.parent }, JobCondition.InterruptForced);
			}

		}

	}

	public class VerbBipodShoot : Verb_ShootCE
	{
		public int ticks;

		public bool WereChangesApplied1 = false;

		public bool WereChangesApplied2 = false;

		public VerbPropertiesCE verpbrops
		{
			get
			{
				return (VerbPropertiesCE)this.EquipmentSource.def.Verbs.Find(tt22 => tt22.verbClass == typeof(VerbBipodShoot));
			}
		}

		public bipodcomp bipodcomp
		{
			get
			{
				return this.EquipmentSource.TryGetComp<bipodcomp>();
			}
		}

		public override void VerbTickCE()
		{
			if (CasterPawn.Drafted)
			{
				if (!(CasterPawn.ParentHolder is Map))
				{
					return;
				}
				if (CasterPawn != null)
				{
					if (!CasterPawn.pather.Moving && EquipmentSource.TryGetComp<bipodcomp>().IsSetUpRn)
					{
						if (!WereChangesApplied1)
						{
							VerbPropertiesCE VerbPropsClone = (VerbPropertiesCE)this.verbProps.MemberwiseClone();
							VerbPropsClone.recoilAmount = verpbrops.recoilAmount * bipodcomp.Props.recoilmulton;
							VerbPropsClone.warmupTime = verpbrops.warmupTime * bipodcomp.Props.warmupmult;
							VerbPropsClone.range += EquipmentSource.TryGetComp<bipodcomp>().Props.additionalrange;
							this.verbProps = VerbPropsClone;
							WereChangesApplied1 = true;
						}


					}
					if (!CasterPawn.pather.Moving && !EquipmentSource.TryGetComp<bipodcomp>().IsSetUpRn)
					{
						if (!WereChangesApplied2)
						{
							VerbPropertiesCE VerbPropsClone = (VerbPropertiesCE)this.verbProps.MemberwiseClone();
							VerbPropsClone.recoilAmount = verpbrops.recoilAmount * bipodcomp.Props.recoilmultoff;
							VerbPropsClone.warmupTime = verpbrops.warmupTime * bipodcomp.Props.warmuppenalty;
							VerbPropsClone.range = verpbrops.range;
							this.verbProps = VerbPropsClone;
							WereChangesApplied2 = true;
						}

					}
					if (!CasterPawn.pather.Moving && EquipmentSource.TryGetComp<bipodcomp>().ShouldSetUp && CasterPawn.CurJob.def != BipodDefsOfs.setupbipodjobdef && !bipodcomp.IsSetUpRn)
					{
						Log.Message("setting up");
						EquipmentSource.TryGetComp<bipodcomp>().SetUpStart(CasterPawn);
					}
					if (CasterPawn.pather.Moving)
					{
						WereChangesApplied1 = false;
						WereChangesApplied2 = false;
						EquipmentSource.TryGetComp<bipodcomp>().IsSetUpRn = false;
						this.verbProps = EquipmentSource.def.Verbs.Find(tt33 => tt33.verbClass == typeof(VerbBipodShoot)).MemberwiseClone();
					}
				}
			}
				


			base.VerbTickCE();
		}
	}



	public class SetUpBipod : JobDriver
	{
		private ThingWithComps weapon
		{
			get
			{
				return base.TargetThingA as ThingWithComps;
			}
		}

		private const TargetIndex guntosetup = TargetIndex.A;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			//return true;	
			return this.pawn.Reserve(this.job.GetTarget(guntosetup), this.job, 1, -1, null);
		}
		public bipodcomp Bipod
		{
			get
			{
				return this.weapon.TryGetComp<bipodcomp>();
			}
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_General.Wait(Bipod.Props.TicksToSetUp);
			yield return Toils_General.Do(delegate { Bipod.IsSetUpRn = true; });


		}
	}
	[StaticConstructorOnStartup]
	public class AddChanger
	{
		static AddChanger()
		{
			Add_and_change_all();
		}
		public static void Add_and_Change_ATRs()
		{
			List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == "Bipod_ATR") ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(VerbBipodShoot)) ?? false));
			foreach (ThingDef def in defs)
			{
				Log.Message("adding bipod (ATR) to: " + def.defName.Colorize(Color.yellow));
				if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
				{
					var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

					if (dar != null)
					{
						//Log.Message("2");
						dar.verbClass = typeof(VerbBipodShoot);
						//Log.Message("3");
						def.Verbs.Clear();
						//Log.Message("4");
						def.comps.Add(new BipedProps { additionalrange = 0, recoilmulton = 1f, recoilmultoff = 2.5f, TicksToSetUp = 480, warmupmult = 0.92f, warmuppenalty = 2.1f });
						//Log.Message("5");
						def.Verbs.Add(dar);
						//Log.Message("6");
						Log.Message("sucessfully added bipod (ATR) to: " + def.label.Colorize(Color.yellow));
					}
					else
					{
						Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					}
				}
				else
				{
					Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					foreach (VerbProperties verbp in def.Verbs)
					{
						Log.Message(verbp.verbClass.Name);
					}

				}


			}
		}
		public static void Add_and_Change_DMRs()
		{
			List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == "Bipod_DMR") ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(VerbBipodShoot)) ?? false));
			foreach (ThingDef def in defs)
			{
				Log.Message("adding bipod (DMR) to: " + def.defName.Colorize(Color.green));
				if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
				{
					var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

					if (dar != null)
					{
						//Log.Message("2");
						dar.verbClass = typeof(VerbBipodShoot);
						//Log.Message("3");
						def.Verbs.Clear();
						//Log.Message("4");
						def.comps.Add(new BipedProps { additionalrange = 15, recoilmulton = 0.75f, recoilmultoff = 1f, TicksToSetUp = 240, warmupmult = 0.76f, warmuppenalty = 1.0f });
						//Log.Message("5");
						def.Verbs.Add(dar);
						//Log.Message("6");
						Log.Message("sucessfully added bipod (DMR) to: " + def.label.Colorize(Color.green));
					}
					else
					{
						Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					}
				}
				else
				{
					Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					foreach (VerbProperties verbp in def.Verbs)
					{
						Log.Message(verbp.verbClass.Name);
					}

				}


			}
		}
		public static void Add_and_Change_LMGs()
		{
			List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == "Bipod_LMG") ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(VerbBipodShoot)) ?? false));
			foreach (ThingDef def in defs)
			{
				Log.Message("adding bipod to: " + def.label.Colorize(Color.blue));
				if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
				{
					var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

					if (dar != null)
					{
						//Log.Message("2");
						dar.verbClass = typeof(VerbBipodShoot);
						//Log.Message("3");
						def.Verbs.Clear();
						//Log.Message("4");
						def.comps.Add(new BipedProps { additionalrange = 2, recoilmulton = 0.90f, recoilmultoff = 1.5f, TicksToSetUp = 360, warmupmult = 0.85f, warmuppenalty = 1.5f });
						//Log.Message("5");
						def.Verbs.Add(dar);
						//Log.Message("6");
						Log.Message("sucessfully added bipod (LMG) to: " + def.label.Colorize(Color.blue));
					}
					else
					{
						Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					}
				}
				else
				{
					Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					foreach (VerbProperties verbp in def.Verbs)
					{
						Log.Message(verbp.verbClass.Name);
					}

				}


			}
		}
		public static void Add_and_Change_Saws()
		{
			List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == "Bipod_SAW") ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(VerbBipodShoot)) ?? false));
			foreach (ThingDef def in defs)
			{
				Log.Message("adding bipod to: " + def.label.Colorize(Color.cyan));
				if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
				{
					var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

					if (dar != null)
					{
						//Log.Message("2");
						dar.verbClass = typeof(VerbBipodShoot);
						//Log.Message("3");
						def.Verbs.Clear();
						//Log.Message("4");
						def.comps.Add(new BipedProps { additionalrange = 4, recoilmulton = 0.90f, recoilmultoff = 1.2f, TicksToSetUp = 240, warmupmult = 0.90f, warmuppenalty = 1.2f });
						//Log.Message("5");
						def.Verbs.Add(dar);
						//Log.Message("6");
						Log.Message("sucessfully added bipod (SAW) to: " + def.label.Colorize(Color.cyan));
					}
					else
					{
						Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					}
				}
				else
				{
					Log.Message("adding bipod failed in " + def.label + ". It appears to have no VerbShootCE in verbs");
					foreach (VerbProperties verbp in def.Verbs)
					{
						Log.Message(verbp.verbClass.Name);
					}

				}


			}
		}

		public static void Add_and_change_all()
		{
			foreach (BipodCategoryDef bipod_def in DefDatabase<BipodCategoryDef>.AllDefs)
			{
				List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == bipod_def.bipod_id) ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(VerbBipodShoot)) ?? false));
				foreach (ThingDef def in defs)
				{
					Log.Message("adding bipod (" + bipod_def.label + ") to: " + def.defName.Colorize(Color.cyan));
					if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
					{
						var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

						if (dar != null)
						{
							//Log.Message("2");
							dar.verbClass = typeof(VerbBipodShoot);
							//Log.Message("3");
							def.Verbs.Clear();
							//Log.Message("4");
							def.comps.Add(new BipedProps { additionalrange = bipod_def.ad_Range, recoilmulton = bipod_def.recoil_mult_setup, recoilmultoff = bipod_def.recoil_mult_NOT_setup, TicksToSetUp = bipod_def.setuptime, warmupmult = bipod_def.warmup_mult_setup, warmuppenalty = bipod_def.warmup_mult_NOT_setup });
							//Log.Message("5");
							def.Verbs.Add(dar);
							//Log.Message("6");
							Log.Message("sucessfully added bipod (" + bipod_def.label + ") to: " + def.label.Colorize(bipod_def.log_color));
						}
						else
						{
							Log.Message("adding bipod failed in " + def.label.Colorize(Color.red) + ". It appears to have no VerbShootCE in verbs");
						}
					}
					else
					{
						Log.Message("adding bipod failed in " + def.label.Colorize(Color.red) + ". It appears to have no VerbShootCE in verbs. It's verbs are following:");
						foreach (VerbProperties verbp in def.Verbs)
						{
							Log.Message(verbp.verbClass.Name.Colorize(Color.magenta));
						}

					}
				}
			}
		}

	}



	public class Bipod_Recoil_StatPart : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			var varA = req.Thing.TryGetComp<bipodcomp>();
			if (varA != null)
			{
				if (varA.IsSetUpRn == true)
				{
					val *= varA.Props.recoilmulton;
				}
				else
				{
					val *= varA.Props.recoilmultoff;
				}
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			var varA = req.Thing.TryGetComp<bipodcomp>();
			if (varA != null)
			{
				if (varA.IsSetUpRn == true)
				{
					return "Bipod is set up - " + varA.Props.recoilmulton.ToString().Colorize(Color.blue);
				}
				else
				{
					return "Bipod is NOT set up - " + varA.Props.recoilmultoff.ToString().Colorize(Color.blue);
				}
			}
			else
			{
				return "";
			}
		}
	}
}
