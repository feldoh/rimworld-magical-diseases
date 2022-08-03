using RimWorld;
using Verse;

namespace Magical_Diseases.Illness
{
	public class CompProperties_ChromaticFood : CompProperties
	{
		public ColorDef forcedColorDef = null;
		public CompProperties_ChromaticFood() => compClass = typeof (CompChromaticFood);
	}
}
