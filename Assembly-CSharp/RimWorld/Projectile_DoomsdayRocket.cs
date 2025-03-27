using Verse;

namespace RimWorld;

public class Projectile_DoomsdayRocket : Projectile
{
	private const int ExtraExplosionCount = 3;

	private const int ExtraExplosionRadius = 5;

	public override bool AnimalsFleeImpact => true;

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing, blockedByShield);
		GenExplosion.DoExplosion(base.Position, map, def.projectile.explosionRadius, DamageDefOf.Bomb, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, postExplosionSpawnThingDef: ThingDefOf.Filth_Fuel, intendedTarget: intendedTarget.Thing, postExplosionSpawnChance: 0.2f, postExplosionSpawnThingCount: 1, postExplosionGasType: null, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 0.4f, damageFalloff: false, direction: null, ignoredThings: null, affectedAngle: null);
		CellRect cellRect = CellRect.CenteredOn(base.Position, 5);
		cellRect.ClipInsideMap(map);
		for (int i = 0; i < 3; i++)
		{
			IntVec3 randomCell = cellRect.RandomCell;
			DoFireExplosion(randomCell, map, 3.9f);
		}
	}

	protected void DoFireExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Flame, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing, null, 0f, 1, null, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, null);
	}
}
