using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using Verse;

namespace RimWorld;

public class StockGenerator_Animals : StockGenerator
{
	[NoTranslate]
	private List<string> tradeTagsSell = new List<string>();

	[NoTranslate]
	private List<string> tradeTagsBuy = new List<string>();

	private IntRange kindCountRange = new IntRange(1, 1);

	private float minWildness;

	private float maxWildness = 1f;

	private bool checkTemperature = true;

	[NoTranslate]
	private List<string> createMatingPair = new List<string>();

	private static readonly SimpleCurve SelectionChanceFromWildnessCurve = new SimpleCurve
	{
		new CurvePoint(0f, 100f),
		new CurvePoint(0.25f, 60f),
		new CurvePoint(0.5f, 30f),
		new CurvePoint(0.75f, 12f),
		new CurvePoint(1f, 2f)
	};

	private const float SelectionChanceFactorIfExistingMatingPair = 0.5f;

	public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
	{
		int numKinds = kindCountRange.RandomInRange;
		int count = countRange.RandomInRange;
		if (count > 1 && !createMatingPair.NullOrEmpty())
		{
			Func<PawnKindDef, bool> CanCreateMatingPair = delegate(PawnKindDef k)
			{
				if (k.race.tradeTags == null || createMatingPair.NullOrEmpty())
				{
					return false;
				}
				for (int i = 0; i < k.race.tradeTags.Count; i++)
				{
					if (createMatingPair.Contains(k.race.tradeTags[i]))
					{
						return true;
					}
				}
				return false;
			};
			DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef k) => PawnKindAllowed(k, forTile) && CanCreateMatingPair(k)).TryRandomElementByWeight((PawnKindDef k) => (PawnUtility.PlayerHasReproductivePair(k) ? 0.5f : 1f) * SelectionChance(k), out var matingKind);
			if (matingKind != null)
			{
				PawnGenerationRequest request = new PawnGenerationRequest(matingKind, null, PawnGenerationContext.NonPlayer, forTile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, Gender.Female, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
				yield return PawnGenerator.GeneratePawn(request);
				PawnGenerationRequest request2 = new PawnGenerationRequest(matingKind, null, PawnGenerationContext.NonPlayer, forTile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, Gender.Male, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
				yield return PawnGenerator.GeneratePawn(request2);
				count -= 2;
			}
		}
		if (count <= 0)
		{
			yield break;
		}
		List<PawnKindDef> kinds = new List<PawnKindDef>();
		for (int j = 0; j < numKinds; j++)
		{
			if (!DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef k) => !kinds.Contains(k) && PawnKindAllowed(k, forTile)).TryRandomElementByWeight((PawnKindDef k) => SelectionChance(k), out var result))
			{
				break;
			}
			kinds.Add(result);
		}
		for (int l = 0; l < count; l++)
		{
			if (!kinds.TryRandomElement(out var result2))
			{
				break;
			}
			PawnGenerationRequest request3 = new PawnGenerationRequest(result2, null, PawnGenerationContext.NonPlayer, forTile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
			yield return PawnGenerator.GeneratePawn(request3);
		}
	}

	private float SelectionChance(PawnKindDef k)
	{
		return SelectionChanceFromWildnessCurve.Evaluate(k.RaceProps.wildness);
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		if (thingDef.category == ThingCategory.Pawn && thingDef.race.Animal && thingDef.tradeability != 0)
		{
			if (!tradeTagsSell.Any((string tag) => thingDef.tradeTags != null && thingDef.tradeTags.Contains(tag)))
			{
				return tradeTagsBuy.Any((string tag) => thingDef.tradeTags != null && thingDef.tradeTags.Contains(tag));
			}
			return true;
		}
		return false;
	}

	public override Tradeability TradeabilityFor(ThingDef thingDef)
	{
		if (!HandlesThingDef(thingDef))
		{
			return Tradeability.None;
		}
		bool flag = false;
		bool flag2 = false;
		if ((thingDef.tradeability == Tradeability.All || thingDef.tradeability == Tradeability.Buyable) && tradeTagsSell.Any((string tag) => thingDef.tradeTags != null && thingDef.tradeTags.Contains(tag)))
		{
			flag = true;
		}
		if ((thingDef.tradeability == Tradeability.All || thingDef.tradeability == Tradeability.Sellable) && tradeTagsBuy.Any((string tag) => thingDef.tradeTags != null && thingDef.tradeTags.Contains(tag)))
		{
			flag2 = true;
		}
		if (flag2 && flag)
		{
			return Tradeability.All;
		}
		if (flag2)
		{
			return Tradeability.Sellable;
		}
		if (flag)
		{
			return Tradeability.Buyable;
		}
		return Tradeability.None;
	}

	private bool PawnKindAllowed(PawnKindDef kind, int forTile)
	{
		if (!kind.RaceProps.Animal || kind.RaceProps.wildness < minWildness || kind.RaceProps.wildness > maxWildness || kind.RaceProps.wildness > 1f)
		{
			return false;
		}
		if (checkTemperature)
		{
			int num = forTile;
			if (num == -1 && Find.AnyPlayerHomeMap != null)
			{
				num = Find.AnyPlayerHomeMap.Tile;
			}
			if (num != -1 && !Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(num, kind.race))
			{
				return false;
			}
		}
		if (kind.race.tradeTags == null)
		{
			return false;
		}
		if (!tradeTagsSell.Any((string x) => kind.race.tradeTags.Contains(x)))
		{
			return false;
		}
		if (!kind.race.tradeability.TraderCanSell())
		{
			return false;
		}
		return true;
	}

	public void LogAnimalChances()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PawnKindDef allDef in DefDatabase<PawnKindDef>.AllDefs)
		{
			stringBuilder.AppendLine(allDef.defName + ": " + SelectionChance(allDef).ToString("F2"));
		}
		Log.Message(stringBuilder.ToString());
	}

	[DebugOutput]
	private static void StockGenerationAnimals()
	{
		StockGenerator_Animals stockGenerator_Animals = new StockGenerator_Animals();
		stockGenerator_Animals.tradeTagsSell = new List<string>();
		stockGenerator_Animals.tradeTagsSell.Add("AnimalCommon");
		stockGenerator_Animals.tradeTagsSell.Add("AnimalUncommon");
		stockGenerator_Animals.LogAnimalChances();
	}
}
