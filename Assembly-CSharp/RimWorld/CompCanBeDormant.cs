using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public class CompCanBeDormant : ThingComp
{
	private int makeTick;

	public int wokeUpTick = int.MinValue;

	public int wakeUpOnTick = int.MinValue;

	private int repeatWakeUpSignalOnTick = int.MaxValue;

	private Effecter wakeUpEffect;

	public string wakeUpSignalTag;

	public List<string> wakeUpSignalTags;

	public const string DefaultWakeUpSignal = "CompCanBeDormant.WakeUp";

	private CompProperties_CanBeDormant Props => (CompProperties_CanBeDormant)props;

	private bool WaitingToWakeUp => wakeUpOnTick != int.MinValue;

	public bool Awake
	{
		get
		{
			if (wokeUpTick == int.MinValue || wokeUpTick > Find.TickManager.TicksGame)
			{
				if (Props.jobDormancy && parent is Pawn pawn)
				{
					return pawn.CurJobDef != JobDefOf.Wait_AsleepDormancy;
				}
				return false;
			}
			return true;
		}
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
		makeTick = GenTicks.TicksGame;
		if (!Props.startsDormant)
		{
			WakeUp();
		}
	}

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		wakeUpSignalTag = Props.wakeUpSignalTag;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Prefs.DevMode && DebugSettings.godMode && !Props.dontShowDevGizmos)
		{
			if (!Awake)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: Wake Up",
					action = WakeUp
				};
			}
			else if (Props.jobDormancy)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: To Sleep",
					action = ToSleep
				};
			}
		}
	}

	public override string CompInspectStringExtra()
	{
		if (parent is Pawn p && (p.IsSelfShutdown() || p.Awake()))
		{
			return null;
		}
		if (Awake)
		{
			if (parent.Faction != Faction.OfPlayer && makeTick != wokeUpTick && !(parent is Pawn { Downed: not false }))
			{
				return Props.awakeStateLabelKey.Translate((GenTicks.TicksGame - wokeUpTick).TicksToDays().ToString("0.#"));
			}
			return null;
		}
		if (!Props.wakeUpDelayStateLabelKey.NullOrEmpty() && wakeUpOnTick != int.MinValue)
		{
			return Props.wakeUpDelayStateLabelKey.Translate();
		}
		return Props.dormantStateLabelKey.Translate();
	}

	public void WakeUpWithDelay()
	{
		if (!Awake)
		{
			wakeUpOnTick = Find.TickManager.TicksGame + Props.wakeUpDelayRange.RandomInRange;
			repeatWakeUpSignalOnTick = (Props.wakeUpRepeatSignalDelayRange.HasValue ? (Find.TickManager.TicksGame + Props.wakeUpRepeatSignalDelayRange.Value.RandomInRange) : int.MinValue);
		}
	}

	public void WakeUp()
	{
		if (Awake)
		{
			return;
		}
		wokeUpTick = GenTicks.TicksGame;
		wakeUpOnTick = int.MinValue;
		Pawn obj = parent as Pawn;
		Building building = parent as Building;
		(obj?.GetLord() ?? building?.GetLord())?.Notify_DormancyWakeup();
		if (parent.Spawned)
		{
			if (parent is IAttackTarget t)
			{
				parent.Map.attackTargetsCache.UpdateTarget(t);
			}
			if (Props.jobDormancy && parent is Pawn pawn && pawn.CurJobDef == JobDefOf.Wait_AsleepDormancy)
			{
				pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}
	}

	public void ToSleep()
	{
		if (!Awake)
		{
			return;
		}
		wokeUpTick = int.MinValue;
		if (parent.Spawned)
		{
			if (parent is IAttackTarget t)
			{
				parent.Map.attackTargetsCache.UpdateTarget(t);
			}
			if (Props.jobDormancy && parent is Pawn pawn)
			{
				Job job = JobMaker.MakeJob(JobDefOf.Wait_AsleepDormancy, pawn.Position);
				job.forceSleep = true;
				pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
			}
		}
	}

	public override void CompTickRare()
	{
		base.CompTickRare();
		if (wakeUpOnTick != int.MinValue && Find.TickManager.TicksGame >= wakeUpOnTick)
		{
			WakeUp();
		}
		TickRareWorker();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (wakeUpOnTick != int.MinValue)
		{
			if (Find.TickManager.TicksGame >= wakeUpOnTick)
			{
				WakeUp();
				wakeUpEffect?.Cleanup();
				wakeUpEffect = null;
			}
			else
			{
				if (Props.wakeUpEffect != null && wakeUpEffect == null)
				{
					wakeUpEffect = Props.wakeUpEffect.SpawnAttached(parent, parent.Map);
				}
				wakeUpEffect?.EffectTick(parent, parent);
				if (repeatWakeUpSignalOnTick != int.MinValue && Find.TickManager.TicksGame == repeatWakeUpSignalOnTick)
				{
					Find.SignalManager.SendSignal(new Signal(wakeUpSignalTag, parent.Named("SUBJECT")));
				}
			}
		}
		if (parent.IsHashIntervalTick(250))
		{
			TickRareWorker();
		}
	}

	public void TickRareWorker()
	{
		if (parent.Spawned && !Awake && !(parent is Pawn) && Props.showSleepingZs && (Props.delayedWakeUpDoesZs || wakeUpOnTick == int.MinValue) && !parent.Position.Fogged(parent.Map))
		{
			FleckMaker.ThrowMetaIcon(parent.Position, parent.Map, FleckDefOf.SleepZ);
		}
	}

	public override void Notify_SignalReceived(Signal signal)
	{
		if (!string.IsNullOrEmpty(wakeUpSignalTag) && !Awake && (signal.tag == wakeUpSignalTag || (wakeUpSignalTags != null && wakeUpSignalTags.Contains(signal.tag))) && signal.args.TryGetArg("SUBJECT", out Thing arg) && arg != parent && arg != null && arg.Map == parent.Map && parent.Position.DistanceTo(arg.Position) <= Props.maxDistAwakenByOther && (!signal.args.TryGetArg("FACTION", out Faction arg2) || arg2 == null || arg2 == parent.Faction) && (Props.canWakeUpFogged || !parent.Fogged()) && !WaitingToWakeUp)
		{
			WakeUpWithDelay();
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref wokeUpTick, "wokeUpTick", int.MinValue);
		Scribe_Values.Look(ref wakeUpOnTick, "wakeUpOnTick", int.MinValue);
		Scribe_Values.Look(ref wakeUpSignalTag, "wakeUpSignalTag");
		Scribe_Collections.Look(ref wakeUpSignalTags, "wakeUpSignalTags", LookMode.Value);
		Scribe_Values.Look(ref makeTick, "makeTick", 0);
		Scribe_Values.Look(ref repeatWakeUpSignalOnTick, "repeatWakeUpSignalOnTick", int.MinValue);
	}
}
