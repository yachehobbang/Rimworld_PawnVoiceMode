using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld;

public class Dialog_FormCaravan : Window
{
	private enum Tab
	{
		Pawns,
		Items,
		TravelSupplies
	}

	private Map map;

	private bool reform;

	private Action onClosed;

	private bool canChooseRoute;

	private bool mapAboutToBeRemoved;

	public bool choosingRoute;

	private bool thisWindowInstanceEverOpened;

	public List<TransferableOneWay> transferables;

	private TransferableOneWayWidget pawnsTransfer;

	private TransferableOneWayWidget itemsTransfer;

	private TransferableOneWayWidget travelSuppliesTransfer;

	private Tab tab;

	private float lastMassFlashTime = -9999f;

	private int startingTile = -1;

	private int destinationTile = -1;

	private IntVec3 designatedMeetingPoint = IntVec3.Invalid;

	private bool massUsageDirty = true;

	private float cachedMassUsage;

	private bool massCapacityDirty = true;

	private float cachedMassCapacity;

	private string cachedMassCapacityExplanation;

	private bool tilesPerDayDirty = true;

	private float cachedTilesPerDay;

	private string cachedTilesPerDayExplanation;

	private bool daysWorthOfFoodDirty = true;

	private Pair<float, float> cachedDaysWorthOfFood;

	private bool foragedFoodPerDayDirty = true;

	private Pair<ThingDef, float> cachedForagedFoodPerDay;

	private string cachedForagedFoodPerDayExplanation;

	private bool visibilityDirty = true;

	private float cachedVisibility;

	private string cachedVisibilityExplanation;

	private bool ticksToArriveDirty = true;

	private int cachedTicksToArrive;

	private bool autoSelectTravelSupplies;

	private const float TitleRectHeight = 35f;

	private const float BottomAreaHeight = 55f;

	private const float AutoSelectCheckBoxHeight = 35f;

	private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

	private const float MaxDaysWorthOfFoodToShowWarningDialog = 5f;

	private static readonly FloatRange ExtraFoodDaysRange = new FloatRange(1f, 5f);

	private const int AutoMedicinePerColonist = 2;

	private const float AdjustedTravelTimeFactor = 0.35f;

	private static List<TabRecord> tabsList = new List<TabRecord>();

	private static List<ThingCount> tmpFood = new List<ThingCount>();

	private static List<Pawn> tmpPawns = new List<Pawn>();

	private static List<Pawn> tmpPawnsToTransfer = new List<Pawn>();

	private static List<Thing> tmpPackingSpots = new List<Thing>();

	private static List<Pair<int, int>> tmpTicksToArrive = new List<Pair<int, int>>();

	private static Dictionary<Pawn, float> tmpPawnNutritionDays = new Dictionary<Pawn, float>();

	private static List<TransferableOneWay> tmpBeds = new List<TransferableOneWay>();

	public int CurrentTile => map.Tile;

	public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

	protected override float Margin => 0f;

	private bool AutoStripSpawnedCorpses => reform;

	private bool ListPlayerPawnsInventorySeparately => reform;

	private BiomeDef Biome => map.Biome;

	private bool MustChooseRoute
	{
		get
		{
			if (canChooseRoute)
			{
				if (reform)
				{
					return map.Parent is Settlement;
				}
				return true;
			}
			return false;
		}
	}

	private bool ShowCancelButton
	{
		get
		{
			if (!mapAboutToBeRemoved)
			{
				return true;
			}
			bool flag = false;
			for (int i = 0; i < transferables.Count; i++)
			{
				if (transferables[i].AnyThing is Pawn { IsColonist: not false, Downed: false })
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return true;
			}
			return false;
		}
	}

	private IgnorePawnsInventoryMode IgnoreInventoryMode
	{
		get
		{
			if (!ListPlayerPawnsInventorySeparately)
			{
				return IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
			}
			return IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn;
		}
	}

	public float MassUsage
	{
		get
		{
			if (massUsageDirty)
			{
				massUsageDirty = false;
				cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnoreInventoryMode, includePawnsMass: false, AutoStripSpawnedCorpses);
			}
			return cachedMassUsage;
		}
	}

	public float MassCapacity
	{
		get
		{
			if (massCapacityDirty)
			{
				massCapacityDirty = false;
				StringBuilder stringBuilder = new StringBuilder();
				cachedMassCapacity = CollectionsMassCalculator.CapacityTransferables(transferables, stringBuilder);
				cachedMassCapacityExplanation = stringBuilder.ToString();
			}
			return cachedMassCapacity;
		}
	}

	private float TilesPerDay
	{
		get
		{
			if (tilesPerDayDirty)
			{
				tilesPerDayDirty = false;
				StringBuilder stringBuilder = new StringBuilder();
				cachedTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(transferables, MassUsage, MassCapacity, CurrentTile, startingTile, stringBuilder);
				cachedTilesPerDayExplanation = stringBuilder.ToString();
			}
			return cachedTilesPerDay;
		}
	}

	private Pair<float, float> DaysWorthOfFood
	{
		get
		{
			if (daysWorthOfFoodDirty)
			{
				daysWorthOfFoodDirty = false;
				float first;
				float second;
				if (destinationTile != -1)
				{
					using WorldPath path = Find.WorldPathFinder.FindPath(CurrentTile, destinationTile, null);
					int ticksPerMove = CaravanTicksPerMoveUtility.GetTicksPerMove(new CaravanTicksPerMoveUtility.CaravanInfo(this));
					first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, CurrentTile, IgnoreInventoryMode, Faction.OfPlayer, path, 0f, ticksPerMove);
					second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, CurrentTile, IgnoreInventoryMode, path, 0f, ticksPerMove);
				}
				else
				{
					first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, CurrentTile, IgnoreInventoryMode, Faction.OfPlayer);
					second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, CurrentTile, IgnoreInventoryMode);
				}
				cachedDaysWorthOfFood = new Pair<float, float>(first, second);
			}
			return cachedDaysWorthOfFood;
		}
	}

	private Pair<ThingDef, float> ForagedFoodPerDay
	{
		get
		{
			if (foragedFoodPerDayDirty)
			{
				foragedFoodPerDayDirty = false;
				StringBuilder stringBuilder = new StringBuilder();
				cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(transferables, Biome, Faction.OfPlayer, stringBuilder);
				cachedForagedFoodPerDayExplanation = stringBuilder.ToString();
			}
			return cachedForagedFoodPerDay;
		}
	}

	private float Visibility
	{
		get
		{
			if (visibilityDirty)
			{
				visibilityDirty = false;
				StringBuilder stringBuilder = new StringBuilder();
				cachedVisibility = CaravanVisibilityCalculator.Visibility(transferables, stringBuilder);
				cachedVisibilityExplanation = stringBuilder.ToString();
			}
			return cachedVisibility;
		}
	}

	private int TicksToArrive
	{
		get
		{
			if (destinationTile == -1)
			{
				return 0;
			}
			if (ticksToArriveDirty)
			{
				ticksToArriveDirty = false;
				using WorldPath path = Find.WorldPathFinder.FindPath(CurrentTile, destinationTile, null);
				cachedTicksToArrive = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(CurrentTile, destinationTile, path, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(new CaravanTicksPerMoveUtility.CaravanInfo(this)), Find.TickManager.TicksAbs);
			}
			return cachedTicksToArrive;
		}
	}

	private bool MostFoodWillRotSoon
	{
		get
		{
			float num = 0f;
			float num2 = 0f;
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (transferableOneWay.HasAnyThing && transferableOneWay.CountToTransfer > 0 && transferableOneWay.ThingDef.IsNutritionGivingIngestible && !(transferableOneWay.AnyThing is Corpse))
				{
					float num3 = 600f;
					CompRottable compRottable = transferableOneWay.AnyThing.TryGetComp<CompRottable>();
					if (compRottable != null)
					{
						num3 = (float)DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(compRottable, CurrentTile) / 60000f;
					}
					float num4 = transferableOneWay.ThingDef.GetStatValueAbstract(StatDefOf.Nutrition) * (float)transferableOneWay.CountToTransfer;
					if (num3 < 5f)
					{
						num += num4;
					}
					else
					{
						num2 += num4;
					}
				}
			}
			if (num == 0f && num2 == 0f)
			{
				return false;
			}
			return num / (num + num2) >= 0.75f;
		}
	}

	public Dialog_FormCaravan(Map map, bool reform = false, Action onClosed = null, bool mapAboutToBeRemoved = false, IntVec3? designatedMeetingPoint = null)
	{
		this.map = map;
		this.reform = reform;
		this.onClosed = onClosed;
		this.mapAboutToBeRemoved = mapAboutToBeRemoved;
		this.designatedMeetingPoint = designatedMeetingPoint ?? IntVec3.Invalid;
		canChooseRoute = !reform || !map.retainedCaravanData.HasDestinationTile;
		closeOnAccept = true;
		closeOnCancel = !reform;
		autoSelectTravelSupplies = !reform;
		forcePause = true;
		absorbInputAroundWindow = true;
	}

	public override void PostOpen()
	{
		base.PostOpen();
		choosingRoute = false;
		if (!thisWindowInstanceEverOpened)
		{
			thisWindowInstanceEverOpened = true;
			CalculateAndRecacheTransferables();
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.FormCaravan, KnowledgeAmount.Total);
			Find.WorldRoutePlanner.Start(this);
		}
	}

	public override void PostClose()
	{
		base.PostClose();
		if (onClosed != null && !choosingRoute)
		{
			onClosed();
		}
	}

	public void Notify_NoLongerChoosingRoute()
	{
		choosingRoute = false;
		if (!Find.WindowStack.IsOpen(this) && onClosed != null)
		{
			onClosed();
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect rect = new Rect(0f, 0f, inRect.width, 35f);
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect, (reform ? "ReformCaravan" : "FormCaravan").Translate());
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, cachedMassCapacityExplanation, TilesPerDay, cachedTilesPerDayExplanation, DaysWorthOfFood, ForagedFoodPerDay, cachedForagedFoodPerDayExplanation, Visibility, cachedVisibilityExplanation), null, CurrentTile, (destinationTile == -1) ? ((int?)null) : new int?(TicksToArrive), lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), lerpMassColor: true, (destinationTile == -1) ? ((TaggedString)null) : ("\n" + "DaysWorthOfFoodTooltip_OnlyFirstWaypoint".Translate()));
		tabsList.Clear();
		tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
		{
			tab = Tab.Pawns;
		}, tab == Tab.Pawns));
		tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
		{
			tab = Tab.Items;
		}, tab == Tab.Items));
		tabsList.Add(new TabRecord("TravelSupplies".Translate(), delegate
		{
			tab = Tab.TravelSupplies;
		}, tab == Tab.TravelSupplies));
		inRect.yMin += 119f;
		Widgets.DrawMenuSection(inRect);
		TabDrawer.DrawTabs(inRect, tabsList);
		tabsList.Clear();
		inRect = inRect.ContractedBy(17f);
		inRect.height += 17f;
		Widgets.BeginGroup(inRect);
		Rect rect2 = inRect.AtZero();
		DoBottomButtons(rect2);
		Rect rect3 = rect2;
		rect3.yMax -= 76f;
		bool anythingChanged = false;
		switch (tab)
		{
		case Tab.Pawns:
			pawnsTransfer.OnGUI(rect3, out anythingChanged);
			break;
		case Tab.Items:
			itemsTransfer.OnGUI(rect3, out anythingChanged);
			break;
		case Tab.TravelSupplies:
			travelSuppliesTransfer.extraHeaderSpace = 35f;
			travelSuppliesTransfer.OnGUI(rect3, out anythingChanged);
			DrawAutoSelectCheckbox(rect3, ref anythingChanged);
			break;
		}
		if (anythingChanged)
		{
			Notify_TransferablesChanged();
		}
		Widgets.EndGroup();
	}

	public void DrawAutoSelectCheckbox(Rect rect, ref bool anythingChanged)
	{
		rect.yMin += 37f;
		rect.height = 35f;
		bool num = autoSelectTravelSupplies;
		Widgets.CheckboxLabeled(rect, "AutomaticallySelectTravelSupplies".Translate(), ref autoSelectTravelSupplies, disabled: false, null, null, placeCheckboxNearText: true);
		travelSuppliesTransfer.readOnly = autoSelectTravelSupplies;
		if (num != autoSelectTravelSupplies)
		{
			anythingChanged = true;
		}
	}

	public override bool CausesMessageBackground()
	{
		return true;
	}

	public void Notify_ChoseRoute(int destinationTile)
	{
		this.destinationTile = destinationTile;
		startingTile = CaravanExitMapUtility.BestExitTileToGoTo(destinationTile, map);
		ticksToArriveDirty = true;
		daysWorthOfFoodDirty = true;
		soundAppear.PlayOneShotOnCamera();
		if (autoSelectTravelSupplies)
		{
			SelectApproximateBestTravelSupplies();
		}
	}

	private void AddToTransferables(Thing t, bool setToTransferMax = false)
	{
		TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
		if (transferableOneWay == null)
		{
			transferableOneWay = new TransferableOneWay();
			transferables.Add(transferableOneWay);
		}
		if (transferableOneWay.things.Contains(t))
		{
			Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
			return;
		}
		transferableOneWay.things.Add(t);
		if (setToTransferMax)
		{
			transferableOneWay.AdjustTo(transferableOneWay.CountToTransfer + t.stackCount);
		}
	}

	private void DoBottomButtons(Rect rect)
	{
		Rect rect2 = new Rect(rect.width - BottomButtonSize.x, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y);
		if (Widgets.ButtonText(rect2, "Send".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			TrySend();
		}
		if (ShowCancelButton && Widgets.ButtonText(new Rect(0f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Close();
		}
		if (Widgets.ButtonText(new Rect(rect.width / 2f - BottomButtonSize.x - 8.5f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
			CalculateAndRecacheTransferables();
		}
		if (canChooseRoute)
		{
			if (Widgets.ButtonText(new Rect(rect.width / 2f + 8.5f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ChangeRouteButton".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
			{
				soundClose.PlayOneShotOnCamera();
				Find.WorldRoutePlanner.Start(this);
			}
			if (destinationTile != -1)
			{
				Rect rect3 = rect2;
				rect3.y += rect2.height + 4f;
				rect3.height = 200f;
				rect3.xMin -= 200f;
				Text.Anchor = TextAnchor.UpperRight;
				Widgets.Label(rect3, "CaravanEstimatedDaysToDestination".Translate(((float)TicksToArrive / 60000f).ToString("0.#")));
				Text.Anchor = TextAnchor.UpperLeft;
			}
		}
		if (Prefs.DevMode)
		{
			float width = 200f;
			float height = BottomButtonSize.y / 2f;
			if (Widgets.ButtonText(new Rect(0f, rect2.yMax + 4f, width, height), "DEV: Send instantly", drawBackground: true, doMouseoverSound: true, active: true, null) && DebugTryFormCaravanInstantly())
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				Close(doCloseSound: false);
			}
			if (Widgets.ButtonText(new Rect(204f, rect2.yMax + 4f, width, height), "DEV: Select everything", drawBackground: true, doMouseoverSound: true, active: true, null))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				SetToSendEverything();
			}
		}
	}

	private void TrySend()
	{
		if (reform)
		{
			if (TryReformCaravan())
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				Close(doCloseSound: false);
			}
			return;
		}
		List<string> list = new List<string>();
		List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(transferables);
		Pair<float, float> daysWorthOfFood = DaysWorthOfFood;
		if (daysWorthOfFood.First < 5f)
		{
			list.Add((daysWorthOfFood.First < 0.1f) ? "DaysWorthOfFoodWarningDialog_NoFood".Translate().ToString() : "DaysWorthOfFoodWarningDialog".Translate(daysWorthOfFood.First.ToString("0.#")).Resolve());
		}
		else if (MostFoodWillRotSoon)
		{
			list.Add("CaravanFoodWillRotSoonWarningDialog".Translate());
		}
		if (!pawns.Any((Pawn pawn) => CaravanUtility.IsOwner(pawn, Faction.OfPlayer) && !pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled))
		{
			list.Add("CaravanIncapableOfSocial".Translate());
		}
		if (ShouldShowWarningForUndesirableFood())
		{
			list.Add("DaysWorthOfFoodDietWarningDialog".Translate());
		}
		if (ShouldShowWarningForMechWithoutMechanitor())
		{
			list.Add("CaravanLacksMechMechanitorWarning".Translate());
		}
		if (ModsConfig.BiotechActive)
		{
			IEnumerable<Pawn> source = pawns.Where((Pawn x) => (x.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) is Hediff_PsychicBond hediff_PsychicBond && ThoughtWorker_PsychicBondProximity.NearPsychicBondedPerson(x, hediff_PsychicBond) && !pawns.Contains(hediff_PsychicBond.target)) ? true : false);
			if (source.Any())
			{
				list.Add("PsychicBondDistanceWillBeActive_Caravan".Translate().ToString() + ":\n" + source.Select((Pawn x) => x.NameFullColored.Resolve() + " (" + "Partner".Translate((x.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicBond) as Hediff_PsychicBond).target).Resolve() + ")").ToLineList("  - ", capitalizeItems: true));
			}
		}
		if (list.Count > 0)
		{
			if (!CheckForErrors(pawns))
			{
				return;
			}
			string text = string.Concat(string.Concat(list.Select((string str) => str + "\n\n").ToArray()), "CaravanAreYouSure".Translate().ToString());
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate
			{
				if (TryFormAndSendCaravan())
				{
					Close(doCloseSound: false);
				}
			}));
		}
		else if (TryFormAndSendCaravan())
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
			Close(doCloseSound: false);
		}
	}

	private void CalculateAndRecacheTransferables()
	{
		transferables = new List<TransferableOneWay>();
		AddPawnsToTransferables();
		AddItemsToTransferables();
		CaravanUIUtility.CreateCaravanTransferableWidgets(transferables, out pawnsTransfer, out itemsTransfer, out travelSuppliesTransfer, "FormCaravanColonyThingCountTip".Translate(), IgnoreInventoryMode, () => MassCapacity - MassUsage, AutoStripSpawnedCorpses, CurrentTile, mapAboutToBeRemoved);
		Notify_TransferablesChanged();
	}

	private bool DebugTryFormCaravanInstantly()
	{
		List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
		if (!pawnsFromTransferables.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer)))
		{
			if (ModsConfig.IdeologyActive)
			{
				Messages.Message("CaravanMustHaveAtLeastOneNonSlaveColonist".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			else
			{
				Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		AddItemsFromTransferablesToRandomInventories(pawnsFromTransferables);
		int num = startingTile;
		if (num < 0)
		{
			num = CaravanExitMapUtility.RandomBestExitTileFrom(map);
		}
		if (num < 0)
		{
			num = CurrentTile;
		}
		CaravanFormingUtility.FormAndCreateCaravan(pawnsFromTransferables, Faction.OfPlayer, CurrentTile, num, destinationTile);
		return true;
	}

	private bool TryFormAndSendCaravan()
	{
		List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(transferables);
		if (!CheckForErrors(pawns))
		{
			return false;
		}
		Direction8Way direction8WayFromTo = Find.WorldGrid.GetDirection8WayFromTo(CurrentTile, startingTile);
		if (!TryFindExitSpot(pawns, reachableForEveryColonist: true, out var exitSpot))
		{
			if (!TryFindExitSpot(pawns, reachableForEveryColonist: false, out exitSpot))
			{
				Messages.Message("CaravanCouldNotFindExitSpot".Translate(direction8WayFromTo.LabelShort()), MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			Messages.Message("CaravanCouldNotFindReachableExitSpot".Translate(direction8WayFromTo.LabelShort()), new GlobalTargetInfo(exitSpot, map), MessageTypeDefOf.CautionInput, historical: false);
		}
		IntVec3 meetingPoint = designatedMeetingPoint;
		if (meetingPoint == IntVec3.Invalid && !TryFindRandomPackingSpot(exitSpot, out meetingPoint))
		{
			Messages.Message("CaravanCouldNotFindPackingSpot".Translate(direction8WayFromTo.LabelShort()), new GlobalTargetInfo(exitSpot, map), MessageTypeDefOf.RejectInput, historical: false);
			return false;
		}
		Pawn pawn = pawns.Find((Pawn ropee) => AnimalPenUtility.NeedsToBeManagedByRope(ropee) && !pawns.Any((Pawn roper) => roper.IsColonist && GatherAnimalsAndSlavesForCaravanUtility.CanRoperTakeAnimalToDest(roper, ropee, exitSpot) && GatherAnimalsAndSlavesForCaravanUtility.CanRoperTakeAnimalToDest(roper, ropee, meetingPoint)));
		if (pawn != null)
		{
			Messages.Message("CaravanRoamerCannotReachSpots".Translate(pawn.LabelShort, pawn), pawn, MessageTypeDefOf.CautionInput, historical: false);
			return false;
		}
		CaravanFormingUtility.StartFormingCaravan(pawns.Where((Pawn x) => !x.Downed).ToList(), pawns.Where((Pawn x) => x.Downed).ToList(), Faction.OfPlayer, transferables, meetingPoint, exitSpot, startingTile, destinationTile);
		Messages.Message("CaravanFormationProcessStarted".Translate(), pawns[0], MessageTypeDefOf.PositiveEvent, historical: false);
		if (ModsConfig.BiotechActive && pawns.Any((Pawn p) => p.RaceProps.IsMechanoid))
		{
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.MechsInCaravans, OpportunityType.GoodToKnow);
		}
		return true;
	}

	private bool TryReformCaravan()
	{
		List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
		if (!CheckForErrors(pawnsFromTransferables))
		{
			return false;
		}
		AddItemsFromTransferablesToRandomInventories(pawnsFromTransferables);
		Caravan caravan = CaravanExitMapUtility.ExitMapAndCreateCaravan(pawnsFromTransferables, Faction.OfPlayer, CurrentTile, CurrentTile, destinationTile, sendMessage: false);
		map.Parent.CheckRemoveMapNow();
		TaggedString taggedString = "MessageReformedCaravan".Translate();
		if (caravan.pather.Moving && caravan.pather.ArrivalAction != null)
		{
			taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.pather.ArrivalAction.Label + ".";
		}
		Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, historical: false);
		return true;
	}

	private void AddItemsFromTransferablesToRandomInventories(List<Pawn> pawns)
	{
		transferables.RemoveAll((TransferableOneWay x) => x.AnyThing is Pawn);
		if (ListPlayerPawnsInventorySeparately)
		{
			for (int i = 0; i < pawns.Count; i++)
			{
				if (CanListInventorySeparately(pawns[i]))
				{
					ThingOwner<Thing> innerContainer = pawns[i].inventory.innerContainer;
					for (int num = innerContainer.Count - 1; num >= 0; num--)
					{
						RemoveCarriedItemFromTransferablesOrDrop(innerContainer[num], pawns[i], transferables);
					}
				}
			}
			for (int j = 0; j < transferables.Count; j++)
			{
				if (transferables[j].things.Any((Thing x) => !x.Spawned))
				{
					transferables[j].things.SortBy((Thing x) => x.Spawned);
				}
			}
		}
		for (int k = 0; k < transferables.Count; k++)
		{
			if (!(transferables[k].AnyThing is Corpse))
			{
				TransferableUtility.Transfer(transferables[k].things, transferables[k].CountToTransfer, delegate(Thing splitPiece, IThingHolder originalHolder)
				{
					Thing item = splitPiece.TryMakeMinified();
					CaravanInventoryUtility.FindPawnToMoveInventoryTo(item, pawns, null).inventory.TryAddAndUnforbid(item);
				});
			}
		}
		for (int l = 0; l < transferables.Count; l++)
		{
			if (!(transferables[l].AnyThing is Corpse))
			{
				continue;
			}
			TransferableUtility.TransferNoSplit(transferables[l].things, transferables[l].CountToTransfer, delegate(Thing originalThing, int numToTake)
			{
				if (AutoStripSpawnedCorpses && originalThing is Corpse { Spawned: not false } corpse)
				{
					corpse.Strip();
				}
				Thing item2 = originalThing.SplitOff(numToTake);
				CaravanInventoryUtility.FindPawnToMoveInventoryTo(item2, pawns, null).inventory.TryAddAndUnforbid(item2);
			});
		}
	}

	private bool CheckForErrors(List<Pawn> pawns)
	{
		if (MustChooseRoute && destinationTile < 0)
		{
			Messages.Message("MessageMustChooseRouteFirst".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return false;
		}
		if (!reform && startingTile < 0)
		{
			Messages.Message("MessageNoValidExitTile".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return false;
		}
		if (!pawns.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
		{
			if (ModsConfig.IdeologyActive)
			{
				Messages.Message("CaravanMustHaveAtLeastOneNonSlaveColonist".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			else
			{
				Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (!reform && MassUsage > MassCapacity)
		{
			FlashMass();
			Messages.Message("TooBigCaravanMassUsage".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return false;
		}
		for (int i = 0; i < transferables.Count; i++)
		{
			if (transferables[i].ThingDef.category != ThingCategory.Item)
			{
				continue;
			}
			int countToTransfer = transferables[i].CountToTransfer;
			int num = 0;
			if (countToTransfer <= 0)
			{
				continue;
			}
			for (int j = 0; j < transferables[i].things.Count; j++)
			{
				Thing t = transferables[i].things[j];
				if (!t.Spawned || pawns.Any((Pawn x) => x.IsColonist && x.CanReach(t, PathEndMode.Touch, Danger.Deadly)))
				{
					num += t.stackCount;
					if (num >= countToTransfer)
					{
						break;
					}
				}
			}
			if (num < countToTransfer)
			{
				if (countToTransfer == 1)
				{
					Messages.Message("CaravanItemIsUnreachableSingle".Translate(transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					Messages.Message("CaravanItemIsUnreachableMulti".Translate(countToTransfer, transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
		}
		return true;
	}

	private bool ShouldShowWarningForUndesirableFood()
	{
		tmpFood.Clear();
		tmpPawns.Clear();
		int num = 0;
		foreach (TransferableOneWay transferable in transferables)
		{
			if (!transferable.HasAnyThing)
			{
				continue;
			}
			if (transferable.AnyThing is Pawn)
			{
				for (int i = 0; i < transferable.CountToTransfer; i++)
				{
					Pawn pawn = (Pawn)transferable.things[i];
					if (!InventoryCalculatorsUtility.ShouldIgnoreInventoryOf(pawn, IgnoreInventoryMode))
					{
						ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;
						int j = 0;
						for (int count = innerContainer.Count; j < count; j++)
						{
							Thing thing = innerContainer[j];
							if (thing.def.IsNutritionGivingIngestible && ThingDefOf.Human.race.CanEverEat(thing))
							{
								tmpFood.Add(new ThingCount(thing, thing.stackCount));
								num += thing.stackCount;
							}
						}
					}
					if (pawn.RaceProps.Humanlike && pawn.needs?.food != null)
					{
						tmpPawns.Add(pawn);
					}
				}
			}
			else if (transferable.ThingDef.IsNutritionGivingIngestible && ThingDefOf.Human.race.CanEverEat(transferable.AnyThing) && transferable.CountToTransfer > 0)
			{
				tmpFood.Add(new ThingCount(transferable.AnyThing, transferable.CountToTransfer, ignoreStackLimit: true));
				num += transferable.CountToTransfer;
			}
		}
		int num2 = 0;
		foreach (ThingCount item in tmpFood)
		{
			foreach (Pawn tmpPawn in tmpPawns)
			{
				if (FoodUtility.WillGiveNegativeThoughts(item.Thing, tmpPawn))
				{
					num2 += item.Count;
					break;
				}
			}
		}
		return (double)((float)num2 / (float)num) >= 0.5;
	}

	private bool ShouldShowWarningForMechWithoutMechanitor()
	{
		if (!ModsConfig.BiotechActive)
		{
			return false;
		}
		tmpPawnsToTransfer.Clear();
		foreach (TransferableOneWay transferable in transferables)
		{
			if (transferable.HasAnyThing && transferable.AnyThing is Pawn)
			{
				for (int i = 0; i < transferable.CountToTransfer; i++)
				{
					tmpPawnsToTransfer.Add((Pawn)transferable.things[i]);
				}
			}
		}
		for (int j = 0; j < tmpPawnsToTransfer.Count; j++)
		{
			Pawn pawn = tmpPawnsToTransfer[j];
			if (pawn.IsColonyMech && MechanitorUtility.EverControllable(pawn))
			{
				Pawn overseer = pawn.GetOverseer();
				if (overseer != null && !tmpPawnsToTransfer.Contains(overseer))
				{
					tmpPawnsToTransfer.Clear();
					return true;
				}
			}
		}
		tmpPawnsToTransfer.Clear();
		return false;
	}

	private bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, out IntVec3 spot)
	{
		CaravanExitMapUtility.GetExitMapEdges(Find.WorldGrid, CurrentTile, startingTile, out var primary, out var secondary);
		if ((!(primary != Rot4.Invalid) || !TryFindExitSpot(pawns, reachableForEveryColonist, primary, out spot)) && (!(secondary != Rot4.Invalid) || !TryFindExitSpot(pawns, reachableForEveryColonist, secondary, out spot)) && !TryFindExitSpot(pawns, reachableForEveryColonist, primary.Rotated(RotationDirection.Clockwise), out spot))
		{
			return TryFindExitSpot(pawns, reachableForEveryColonist, primary.Rotated(RotationDirection.Counterclockwise), out spot);
		}
		return true;
	}

	private bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot)
	{
		if (startingTile < 0)
		{
			Log.Error("Can't find exit spot because startingTile is not set.");
			spot = IntVec3.Invalid;
			return false;
		}
		Predicate<IntVec3> validator = (IntVec3 x) => !x.Fogged(map) && x.Standable(map);
		if (reachableForEveryColonist)
		{
			return CellFinder.TryFindRandomEdgeCellWith(delegate(IntVec3 x)
			{
				if (!validator(x))
				{
					return false;
				}
				for (int i = 0; i < pawns.Count; i++)
				{
					if (pawns[i].IsColonist && !pawns[i].Downed && !pawns[i].CanReach(x, PathEndMode.Touch, Danger.Deadly))
					{
						return false;
					}
				}
				return true;
			}, map, exitDirection, CellFinder.EdgeRoadChance_Always, out spot);
		}
		IntVec3 intVec = IntVec3.Invalid;
		int num = -1;
		foreach (IntVec3 item in CellRect.WholeMap(map).GetEdgeCells(exitDirection).InRandomOrder())
		{
			if (!validator(item))
			{
				continue;
			}
			int num2 = 0;
			for (int j = 0; j < pawns.Count; j++)
			{
				if (pawns[j].IsColonist && !pawns[j].Downed && pawns[j].CanReach(item, PathEndMode.Touch, Danger.Deadly))
				{
					num2++;
				}
			}
			if (num2 > num)
			{
				num = num2;
				intVec = item;
			}
		}
		spot = intVec;
		return intVec.IsValid;
	}

	private bool TryFindRandomPackingSpot(IntVec3 exitSpot, out IntVec3 packingSpot)
	{
		tmpPackingSpots.Clear();
		List<Thing> list = map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
		TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassDoors);
		for (int i = 0; i < list.Count; i++)
		{
			if (map.reachability.CanReach(exitSpot, list[i], PathEndMode.OnCell, traverseParams))
			{
				tmpPackingSpots.Add(list[i]);
			}
		}
		if (tmpPackingSpots.Any())
		{
			Thing thing = tmpPackingSpots.RandomElement();
			tmpPackingSpots.Clear();
			packingSpot = thing.Position;
			return true;
		}
		return RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, map, out packingSpot);
	}

	private void AddPawnsToTransferables()
	{
		List<Pawn> list = AllSendablePawns(map, reform);
		for (int i = 0; i < list.Count; i++)
		{
			bool setToTransferMax = (reform || mapAboutToBeRemoved) && !CaravanUtility.ShouldAutoCapture(list[i], Faction.OfPlayer);
			AddToTransferables(list[i], setToTransferMax);
			Thing t;
			if ((t = list[i].carryTracker?.CarriedThing) != null)
			{
				AddToTransferables(t, setToTransferMax);
			}
		}
	}

	private void AddItemsToTransferables()
	{
		List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(map, reform, reform, reform);
		for (int i = 0; i < list.Count; i++)
		{
			AddToTransferables(list[i]);
		}
		if (AutoStripSpawnedCorpses)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].Spawned)
				{
					TryAddCorpseInventoryAndGearToTransferables(list[j]);
				}
			}
		}
		if (!ListPlayerPawnsInventorySeparately)
		{
			return;
		}
		List<Pawn> list2 = AllSendablePawns(map, reform);
		for (int k = 0; k < list2.Count; k++)
		{
			if (!CanListInventorySeparately(list2[k]))
			{
				continue;
			}
			ThingOwner<Thing> innerContainer = list2[k].inventory.innerContainer;
			for (int l = 0; l < innerContainer.Count; l++)
			{
				AddToTransferables(innerContainer[l], setToTransferMax: true);
				if (AutoStripSpawnedCorpses && innerContainer[l].Spawned)
				{
					TryAddCorpseInventoryAndGearToTransferables(innerContainer[l]);
				}
			}
		}
	}

	private void TryAddCorpseInventoryAndGearToTransferables(Thing potentiallyCorpse)
	{
		if (potentiallyCorpse is Corpse corpse)
		{
			AddCorpseInventoryAndGearToTransferables(corpse);
		}
	}

	private void AddCorpseInventoryAndGearToTransferables(Corpse corpse)
	{
		Pawn_InventoryTracker inventory = corpse.InnerPawn.inventory;
		Pawn_ApparelTracker apparel = corpse.InnerPawn.apparel;
		Pawn_EquipmentTracker equipment = corpse.InnerPawn.equipment;
		for (int i = 0; i < inventory.innerContainer.Count; i++)
		{
			if (CanAdd(inventory.innerContainer[i]))
			{
				AddToTransferables(inventory.innerContainer[i]);
			}
		}
		if (apparel != null)
		{
			List<Apparel> wornApparel = apparel.WornApparel;
			for (int j = 0; j < wornApparel.Count; j++)
			{
				if (CanAdd(wornApparel[j]))
				{
					AddToTransferables(wornApparel[j]);
				}
			}
		}
		if (equipment == null)
		{
			return;
		}
		List<ThingWithComps> allEquipmentListForReading = equipment.AllEquipmentListForReading;
		for (int k = 0; k < allEquipmentListForReading.Count; k++)
		{
			if (CanAdd(allEquipmentListForReading[k]))
			{
				AddToTransferables(allEquipmentListForReading[k]);
			}
		}
		static bool CanAdd(Thing thing)
		{
			if (!thing.def.destroyOnDrop)
			{
				return thing.GetInnerIfMinified().def.canLoadIntoCaravan;
			}
			return false;
		}
	}

	private void RemoveCarriedItemFromTransferablesOrDrop(Thing carried, Pawn carrier, List<TransferableOneWay> transferables)
	{
		TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(carried, transferables, TransferAsOneMode.PodsOrCaravanPacking);
		int num;
		if (transferableOneWay == null)
		{
			num = carried.stackCount;
		}
		else if (transferableOneWay.CountToTransfer >= carried.stackCount)
		{
			transferableOneWay.AdjustBy(-carried.stackCount);
			transferableOneWay.things.Remove(carried);
			num = 0;
		}
		else
		{
			num = carried.stackCount - transferableOneWay.CountToTransfer;
			transferableOneWay.AdjustTo(0);
		}
		if (num > 0)
		{
			Thing thing = carried.SplitOff(num);
			if (carrier.SpawnedOrAnyParentSpawned)
			{
				GenPlace.TryPlaceThing(thing, carrier.PositionHeld, carrier.MapHeld, ThingPlaceMode.Near);
			}
			else
			{
				thing.Destroy();
			}
		}
	}

	private void FlashMass()
	{
		lastMassFlashTime = Time.time;
	}

	public static bool CanListInventorySeparately(Pawn p)
	{
		if (p.Faction != Faction.OfPlayer)
		{
			return p.HostFaction == Faction.OfPlayer;
		}
		return true;
	}

	private void SetToSendEverything()
	{
		for (int i = 0; i < transferables.Count; i++)
		{
			transferables[i].AdjustTo(transferables[i].GetMaximumToTransfer());
		}
		Notify_TransferablesChanged();
	}

	private void Notify_TransferablesChanged()
	{
		if (autoSelectTravelSupplies)
		{
			SelectApproximateBestTravelSupplies();
		}
		if (ModsConfig.BiotechActive)
		{
			foreach (TransferableOneWay transferable in transferables)
			{
				for (int i = 0; i < transferable.things.Count; i++)
				{
					if (!transferable.IsThing || !(transferable.things[i] is Pawn { IsColonyMech: not false } pawn))
					{
						continue;
					}
					Pawn overseer = pawn.GetOverseer();
					if (overseer == null || pawn.GetMechControlGroup().WorkMode != MechWorkModeDefOf.Escort)
					{
						continue;
					}
					bool flag = false;
					foreach (TransferableOneWay transferable2 in transferables)
					{
						if (transferable2.IsThing && transferable2.things.Contains(overseer) && transferable2.CountToTransferToDestination > 0)
						{
							flag = true;
							break;
						}
					}
					if (flag && transferable.CountToTransferToDestination <= 0)
					{
						Messages.Message("MessageCaravanAddingEscortingMech".Translate(pawn.Named("MECH"), overseer.Named("OVERSEER")), pawn, MessageTypeDefOf.RejectInput, historical: false);
						transferable.ForceToDestination(1);
					}
					if (!flag && transferable.CountToTransferToDestination > 0)
					{
						Messages.Message("MessageCaravanRemovingEscortingMech".Translate(pawn.Named("MECH"), overseer.Named("OVERSEER")), pawn, MessageTypeDefOf.RejectInput, historical: false);
						transferable.ForceToDestination(0);
					}
				}
			}
		}
		massUsageDirty = true;
		massCapacityDirty = true;
		tilesPerDayDirty = true;
		daysWorthOfFoodDirty = true;
		foragedFoodPerDayDirty = true;
		visibilityDirty = true;
		ticksToArriveDirty = true;
	}

	private void SelectApproximateBestTravelSupplies()
	{
		daysWorthOfFoodDirty = true;
		massUsageDirty = true;
		massCapacityDirty = true;
		IEnumerable<TransferableOneWay> enumerable = transferables.Where((TransferableOneWay x) => x.HasAnyThing && x.ThingDef.category != ThingCategory.Pawn && !x.ThingDef.thingCategories.NullOrEmpty() && x.ThingDef.thingCategories.Contains(ThingCategoryDefOf.Medicine));
		IEnumerable<TransferableOneWay> enumerable2 = transferables.Where((TransferableOneWay x) => x.HasAnyThing && x.ThingDef.IsIngestible && !x.ThingDef.IsDrug && !x.ThingDef.IsCorpse);
		IEnumerable<TransferableOneWay> enumerable3 = Enumerable.Empty<TransferableOneWay>();
		if (ModsConfig.BiotechActive)
		{
			enumerable3 = transferables.Where((TransferableOneWay x) => x.ThingDef == ThingDefOf.HemogenPack);
		}
		tmpBeds.Clear();
		for (int i = 0; i < transferables.Count; i++)
		{
			for (int j = 0; j < transferables[i].things.Count; j++)
			{
				Thing thing = transferables[i].things[j];
				for (int k = 0; k < thing.stackCount; k++)
				{
					if (thing.GetInnerIfMinified() is Building_Bed building_Bed && building_Bed.def.building.bed_caravansCanUse)
					{
						for (int l = 0; l < building_Bed.SleepingSlotsCount; l++)
						{
							tmpBeds.Add(transferables[i]);
						}
					}
				}
			}
		}
		tmpBeds.SortByDescending((TransferableOneWay x) => x.AnyThing.GetInnerIfMinified().GetStatValue(StatDefOf.BedRestEffectiveness));
		foreach (TransferableOneWay item in enumerable)
		{
			item.AdjustTo(0);
		}
		foreach (TransferableOneWay item2 in enumerable2)
		{
			item2.AdjustTo(0);
		}
		foreach (TransferableOneWay tmpBed in tmpBeds)
		{
			tmpBed.AdjustTo(0);
		}
		foreach (TransferableOneWay item3 in enumerable3)
		{
			item3.AdjustTo(0);
		}
		List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
		if (!pawnsFromTransferables.Any())
		{
			return;
		}
		pawnsFromTransferables.SortByDescending((Pawn x) => x.RaceProps.Humanlike);
		foreach (Pawn item4 in pawnsFromTransferables)
		{
			TransferableOneWay transferableOneWay = BestBedFor(item4);
			if (transferableOneWay != null)
			{
				tmpBeds.Remove(transferableOneWay);
				if (transferableOneWay.CanAdjustBy(1).Accepted)
				{
					AddOneIfMassAllows(transferableOneWay);
				}
			}
			if (item4.AnimalOrWildMan() || (item4.guest != null && item4.guest.IsPrisoner) || (ModsConfig.BiotechActive && item4.IsColonyMech))
			{
				continue;
			}
			for (int m = 0; m < 2; m++)
			{
				Transferable transferable = BestMedicineItemFor(item4, enumerable);
				if (transferable != null)
				{
					AddOneIfMassAllows(transferable);
				}
			}
		}
		if (destinationTile == -1 || !DaysWorthOfFoodCalculator.AnyFoodEatingPawn(pawnsFromTransferables) || !enumerable2.Any())
		{
			return;
		}
		try
		{
			WorldPath path = Find.WorldPathFinder.FindPath(CurrentTile, destinationTile, null);
			try
			{
				int ticksPerMove = CaravanTicksPerMoveUtility.GetTicksPerMove(new CaravanTicksPerMoveUtility.CaravanInfo(this));
				CaravanArrivalTimeEstimator.EstimatedTicksToArriveToEvery(CurrentTile, destinationTile, path, 0f, ticksPerMove, Find.TickManager.TicksAbs, tmpTicksToArrive);
				float num = (float)tmpTicksToArrive.Last().Second / 60000f;
				float num2 = num + ExtraFoodDaysRange.ClampToRange(num * 0.35f);
				foreach (Pawn item5 in pawnsFromTransferables)
				{
					if (ModsConfig.BiotechActive && !item5.AnimalOrWildMan() && !item5.IsPrisoner && item5.genes != null && num >= 0.5f && enumerable3.Any() && item5.genes.GetFirstGeneOfType<Gene_Hemogen>() != null)
					{
						int num3 = Mathf.CeilToInt(num / 2f);
						for (int n = 0; n < num3; n++)
						{
							Transferable hemogenPack = GetHemogenPack(enumerable3);
							if (hemogenPack != null)
							{
								AddOneIfMassAllows(hemogenPack);
							}
						}
					}
					if (!VirtualPlantsUtility.CanEverEatVirtualPlants(item5))
					{
						continue;
					}
					for (float num4 = 0f; num4 < num; num4 += 0.25f)
					{
						int ticksAbs = Find.TickManager.TicksGame + (int)(60000f * num4);
						if (VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsAt(CaravanArrivalTimeEstimator.TileIllBeInAt(ticksAbs, tmpTicksToArrive, Find.TickManager.TicksGame), ticksAbs))
						{
							tmpPawnNutritionDays.SetOrAdd(item5, 0.25f);
						}
					}
				}
				float num5 = DaysOfFood();
				while (num5 < num2 && MassUsage < MassCapacity)
				{
					bool flag = false;
					foreach (Pawn item6 in pawnsFromTransferables)
					{
						if (item6.RaceProps.EatsFood && item6.needs.food != null && !(item6.needs.food.MaxLevel <= 0f) && item6.needs.food.FoodFallPerTick != 0f && !(tmpPawnNutritionDays.TryGetValue(item6, 0f) >= num2))
						{
							Transferable transferable2 = BestFoodItemFor(item6, enumerable2, tmpTicksToArrive);
							if (transferable2 != null && AddFoodItem(item6, transferable2, num2))
							{
								flag = true;
							}
						}
					}
					if (!flag)
					{
						break;
					}
					num5 = DaysOfFood();
				}
				float DaysOfFood()
				{
					return DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, CurrentTile, IgnoreInventoryMode, Faction.OfPlayer, path, 0f, ticksPerMove);
				}
			}
			finally
			{
				if (path != null)
				{
					((IDisposable)path).Dispose();
				}
			}
		}
		finally
		{
			tmpTicksToArrive.Clear();
			tmpPawnNutritionDays.Clear();
			daysWorthOfFoodDirty = true;
			massUsageDirty = true;
			massCapacityDirty = true;
		}
	}

	private bool AddFoodItem(Pawn pawn, Transferable transferable, float tripDays)
	{
		int num = transferable.GetMaximumToTransfer() - transferable.CountToTransfer;
		if (num <= 0)
		{
			return false;
		}
		float num2 = tmpPawnNutritionDays.TryGetValue(pawn, 0f);
		float num3 = pawn.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed, ignoreMalnutrition: true) * 60000f;
		float statValueAbstract = transferable.ThingDef.GetStatValueAbstract(StatDefOf.Nutrition);
		int num4 = Mathf.Min(num, Mathf.CeilToInt((tripDays - num2) * num3 / statValueAbstract));
		if (num4 <= 0 || !transferable.CanAdjustBy(num4).Accepted)
		{
			return false;
		}
		int destination = transferable.CountToTransfer + Mathf.Min(num4, Mathf.FloorToInt((MassCapacity - MassUsage) / transferable.ThingDef.BaseMass));
		transferable.AdjustTo(destination);
		tmpPawnNutritionDays.SetOrAdd(pawn, num2 + num3 / (statValueAbstract * (float)num4));
		daysWorthOfFoodDirty = true;
		massUsageDirty = true;
		return true;
	}

	private bool AddOneIfMassAllows(Transferable transferable)
	{
		if (transferable.CanAdjustBy(1).Accepted && MassUsage + transferable.ThingDef.BaseMass < MassCapacity)
		{
			transferable.AdjustBy(1);
			massUsageDirty = true;
			return true;
		}
		return false;
	}

	private TransferableOneWay BestBedFor(Pawn pawn)
	{
		if (ModsConfig.BiotechActive && pawn.IsColonyMech)
		{
			return null;
		}
		if (pawn.needs?.rest == null)
		{
			return null;
		}
		for (int i = 0; i < tmpBeds.Count; i++)
		{
			Thing innerIfMinified = tmpBeds[i].AnyThing.GetInnerIfMinified();
			if (RestUtility.CanUseBedEver(pawn, innerIfMinified.def))
			{
				return tmpBeds[i];
			}
		}
		return null;
	}

	private Transferable BestFoodItemFor(Pawn pawn, IEnumerable<TransferableOneWay> food, List<Pair<int, int>> ticksToArrive)
	{
		Transferable result = null;
		float num = 0f;
		foreach (TransferableOneWay item in food)
		{
			if (item.CanAdjustBy(1).Accepted)
			{
				float foodScore = GetFoodScore(pawn, item.AnyThing, ticksToArrive);
				if (foodScore > num)
				{
					result = item;
					num = foodScore;
				}
			}
		}
		return result;
	}

	private float GetFoodScore(Pawn pawn, Thing food, List<Pair<int, int>> ticksToArrive)
	{
		if (!CaravanPawnsNeedsUtility.CanEatForNutritionEver(food.def, pawn))
		{
			return 0f;
		}
		float num = CaravanPawnsNeedsUtility.GetFoodScore(food.def, pawn, food.GetStatValue(StatDefOf.Nutrition));
		if (FoodUtility.WillGiveNegativeThoughts(food, pawn))
		{
			num *= 0.4f;
		}
		if (ModsConfig.BiotechActive && food.def == ThingDefOf.BabyFood && !pawn.DevelopmentalStage.Baby())
		{
			num *= 0.1f;
		}
		CompRottable compRottable = food.TryGetComp<CompRottable>();
		if (compRottable != null && compRottable.Active && DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(compRottable, CurrentTile, ticksToArrive) < ticksToArrive.Last().Second)
		{
			num *= 0.1f;
		}
		return num;
	}

	private Transferable BestMedicineItemFor(Pawn pawn, IEnumerable<TransferableOneWay> medicine)
	{
		Transferable transferable = null;
		float num = 0f;
		foreach (TransferableOneWay item in medicine)
		{
			Thing anyThing = item.AnyThing;
			if (item.CanAdjustBy(1).Accepted && pawn.playerSettings.medCare.AllowsMedicine(anyThing.def))
			{
				float statValue = anyThing.GetStatValue(StatDefOf.MedicalPotency);
				if (transferable == null || statValue > num)
				{
					transferable = item;
					num = statValue;
				}
			}
		}
		return transferable;
	}

	private Transferable GetHemogenPack(IEnumerable<TransferableOneWay> hemogen)
	{
		foreach (TransferableOneWay item in hemogen)
		{
			if (item.CanAdjustBy(1).Accepted)
			{
				return item;
			}
		}
		return null;
	}

	public static List<Pawn> AllSendablePawns(Map map, bool reform)
	{
		return CaravanFormingUtility.AllSendablePawns(map, allowEvenIfDowned: true, reform, reform, reform);
	}

	public override void OnAcceptKeyPressed()
	{
		TrySend();
	}
}
