using Verse;

namespace Magical_Diseases.Illness
{
	public class CompChromaticFood : ThingComp
	{
		public CompProperties_ChromaticFood Props => (CompProperties_ChromaticFood) props;

		public override void PostIngested(Pawn ingester)
		{
			base.PostIngested(ingester);
			Log.Warning("Post ingest");
			(ingester.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Feldoh_ChromaticSensitivity")) as
				Hediff_ChromaticSensitivity)?.FoodIngested(parent, Props.forcedColorDef?.color);
		}
	}
}