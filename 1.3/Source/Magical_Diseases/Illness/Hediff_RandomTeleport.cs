using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Magical_Diseases.Illness
{
	public class Hediff_RandomTeleport : HediffWithComps
	{
		private int teleportInterval;
		private int teleportRange;
		private float teleportFactor;
		private const int OneDayTicks = 60000;
		private const int TeleportBaseRange = 100;

		#region Properties

		public HediffDef_RandomTeleport Def => this.def as HediffDef_RandomTeleport;

		#endregion Properties

		#region Overrides

		public override void PostMake()
		{
			base.PostMake();
			teleportFactor = Rand.Range(0.8f, 2.0f);
			UpdateTeleportSeverity();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref teleportFactor, "teleportFactor");
		}

		public override void Tick()
		{
			base.Tick();
			if (!ShouldTeleport())
				return;
			UpdateTarget();
			UpdateTeleportSeverity();
		}

		#endregion Overrides

		private void UpdateTeleportSeverity()
		{
			teleportInterval = Mathf.Clamp((int)((1.0f - Severity) * teleportFactor) * OneDayTicks,
				this.Def.minimumTicksBetween,
				(int)(OneDayTicks * teleportFactor));
			teleportRange = Mathf.Clamp((int)(Severity * teleportFactor * TeleportBaseRange), 10, pawn.Map.Size.x);
		}

		#region Helpers

		private bool ShouldTeleport()
		{
			return pawn.IsHashIntervalTick(teleportInterval)
			       // Don't teleport while sedated
			       && (pawn?.health?.capacities?.CanBeAwake ?? true)
			       && pawn.GetCaravan() == null
			       // Reduce teleport chance when downed to allow potential sedation 
			       && (!pawn.Downed || Rand.Value < 0.2);
		}

		private void UpdateTarget()
		{
			if (!pawn.Spawned) return;
			var pawnMap = pawn.Map;
			IntVec3 approxDest = pawn.OccupiedRect().ExpandedBy(teleportRange).RandomCell.ClampInsideMap(pawnMap);
			if (!this.FindFreeCell(approxDest, pawnMap, out var result)) return;
			bool moteBefore = Rand.Bool;
			pawn.teleporting = true;
			if (moteBefore && pawn.Awake())
				MoteMaker.ThrowText(pawn.DrawPos, pawnMap,
					"TextMote_TeleportingSicknessEpisode".Translate((NamedArgument)pawn.Ideo.KeyDeityName), 6.5f);
			pawn.Position = result;
			FleckMaker.ThrowDustPuffThick(result.ToVector3(), pawnMap, Rand.Range(1.5f, 3f),
				CompAbilityEffect_Chunkskip.DustColor);
			SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(approxDest, pawnMap));
			pawn.teleporting = false;
			pawn.Notify_Teleported();

			// Post teleport reaction
			if (Rand.Bool)
			{
				pawn.stances.stunner.StunFor(new IntRange(50, 150).RandomInRange, pawn, false);
			}
			else
			{
				pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced,
					resumeCurJobAfterwards: false);
			}

			if (this.Def.shouldLog)
			{
				BattleLogEntry_Event teleportationSicknessEpisodeEvent =
					new BattleLogEntry_Event(pawn, RulePackDef.Named("Event_TeleportingSicknessEpisode"), null);
				Find.PlayLog.Add(teleportationSicknessEpisodeEvent);
			}

			if (!moteBefore && pawn.Awake())
				MoteMaker.ThrowText(pawn.DrawPos, pawnMap,
					"TextMote_TeleportingSicknessEpisodeExit".Translate(), 6.5f);
		}

		private bool FindFreeCell(IntVec3 target, Map map, out IntVec3 result) =>
			CellFinder.TryFindRandomSpawnCellForPawnNear(target, map, out result, 10,
				cell => CompAbilityEffect_WithDest.CanTeleportThingTo(cell, map));

		#endregion Helpers
	}
}