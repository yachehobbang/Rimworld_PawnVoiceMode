using Verse;

namespace RimWorld;

public class CompProperties_SpawnFilthOnTakeDamage : CompProperties
{
	public ThingDef filthDef;

	public IntRange filthCountRange = IntRange.one;

	public float chance = 1f;

	public CompProperties_SpawnFilthOnTakeDamage()
	{
		compClass = typeof(CompSpawnerFilthOnTakeDamage);
	}
}
