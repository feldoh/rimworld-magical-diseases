using RimWorld;
using UnityEngine;
using Verse;

namespace Magical_Diseases.Illness
{
	public class Hediff_RandomHair : HediffWithComps
	{
		private float changeFactor;
		private const int OneDayTicks = 60000;

		#region Properties

		public HediffDef_RandomHair Def => def as HediffDef_RandomHair;

		#endregion Properties

		#region Overrides

		public override void PostMake()
		{
			base.PostMake();
			changeFactor = Rand.Range(0.1f, 2.0f);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref changeFactor, "changeFactor");
		}

		public override void Tick()
		{
			base.Tick();
			if (!pawn.IsHashIntervalTick((int)(changeFactor * OneDayTicks)))
				return;
			UpdateTarget();
		}

		#endregion Overrides

		#region Helpers

		private void UpdateTarget()
		{
			if (!pawn.Spawned || pawn?.story?.hairColor == null) return;
			pawn.story.hairColor = new Color(Rand.Value, Rand.Value, Rand.Value);
			pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawn);

			if (pawn.Awake())
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
					"TextMote_RandomHairSicknessEpisode".Translate(), 6.5f);
		}

		#endregion Helpers
	}
}