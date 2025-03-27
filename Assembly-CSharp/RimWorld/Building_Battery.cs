using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

[StaticConstructorOnStartup]
public class Building_Battery : Building
{
	private int ticksToExplode;

	private Sustainer wickSustainer;

	private static readonly Vector2 BarSize = new Vector2(1.3f, 0.4f);

	private const float MinEnergyToExplode = 500f;

	private const float EnergyToLoseWhenExplode = 400f;

	private const float ExplodeChancePerDamage = 0.05f;

	private static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f));

	private static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToExplode, "ticksToExplode", 0);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		CompPowerBattery comp = GetComp<CompPowerBattery>();
		GenDraw.FillableBarRequest fillableBarRequest = default(GenDraw.FillableBarRequest);
		fillableBarRequest.center = drawLoc + Vector3.up * 0.1f;
		fillableBarRequest.size = BarSize;
		fillableBarRequest.fillPercent = comp.StoredEnergy / comp.Props.storedEnergyMax;
		fillableBarRequest.filledMat = BatteryBarFilledMat;
		fillableBarRequest.unfilledMat = BatteryBarUnfilledMat;
		fillableBarRequest.margin = 0.15f;
		GenDraw.FillableBarRequest r = fillableBarRequest;
		Rot4 rotation = base.Rotation;
		rotation.Rotate(RotationDirection.Clockwise);
		r.rotation = rotation;
		GenDraw.DrawFillableBar(r);
		if (ticksToExplode > 0 && base.Spawned)
		{
			base.Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
		}
	}

	public override void Tick()
	{
		base.Tick();
		if (ticksToExplode > 0)
		{
			if (wickSustainer == null)
			{
				StartWickSustainer();
			}
			else
			{
				wickSustainer.Maintain();
			}
			ticksToExplode--;
			if (ticksToExplode == 0)
			{
				GenExplosion.DoExplosion(this.OccupiedRect().RandomCell, radius: Rand.Range(0.5f, 1f) * 3f, map: base.Map, damType: DamageDefOf.Flame, instigator: null, damAmount: -1, armorPenetration: -1f, explosionSound: null, weapon: null, projectile: null, intendedTarget: null, postExplosionSpawnThingDef: null, postExplosionSpawnChance: 0f, postExplosionSpawnThingCount: 1, postExplosionGasType: null, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 0f, damageFalloff: false, direction: null, ignoredThings: null, affectedAngle: null);
				GetComp<CompPowerBattery>().DrawPower(400f);
			}
		}
	}

	public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		base.PostApplyDamage(dinfo, totalDamageDealt);
		if (!base.Destroyed && ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && GetComp<CompPowerBattery>().StoredEnergy > 500f)
		{
			ticksToExplode = Rand.Range(70, 150);
			StartWickSustainer();
		}
	}

	private void StartWickSustainer()
	{
		SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
		wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
	}
}
