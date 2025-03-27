using Verse;

namespace RimWorld;

public class CompRitualTargetEffecterSpawner : CompRitualEffectSpawner
{
	private Effecter effecter;

	private CompProperties_RitualTargetEffecterSpawner Props => (CompProperties_RitualTargetEffecterSpawner)props;

	public override void PostDeSpawn(Map map)
	{
		base.PostDeSpawn(map);
		effecter?.Cleanup();
		effecter = null;
	}

	protected override void Tick_InRitual(LordJob_Ritual ritual)
	{
		if (!(Props.minRitualProgress > 0f) || !(ritual.Progress < Props.minRitualProgress))
		{
			if (effecter == null)
			{
				effecter = Props.effecter.Spawn();
				effecter.Trigger(parent, parent);
			}
			effecter.EffectTick(parent, parent);
		}
	}

	protected override void Tick_OutOfRitual()
	{
		effecter?.Cleanup();
		effecter = null;
	}
}
