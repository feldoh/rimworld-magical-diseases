namespace Magical_Diseases.Illness
{
	public class HediffDef_RandomTeleport: MagicalHediffDef
	{
		private const int OneHourTicks = 2500;
		public int minimumTicksBetween = OneHourTicks;
		public bool shouldLog = true;
	}
}
