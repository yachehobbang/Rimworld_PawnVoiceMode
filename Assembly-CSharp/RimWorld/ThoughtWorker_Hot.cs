using Verse;

namespace RimWorld;

public class ThoughtWorker_Hot : ThoughtWorker
{
	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		float statValue = p.GetStatValue(StatDefOf.ComfyTemperatureMax);
		float num = p.AmbientTemperature - statValue;
		if (num <= 0f)
		{
			return ThoughtState.Inactive;
		}
		int num2 = ((!(num < 10f)) ? ((num < 20f) ? 1 : ((!(num < 30f)) ? 3 : 2)) : 0);
		if (ModsConfig.IdeologyActive)
		{
			Ideo ideo = p.Ideo;
			if (ideo != null && ideo.HasPrecept(PreceptDefOf.Temperature_Tough))
			{
				num2 -= 2;
			}
		}
		if (num2 >= 0)
		{
			return ThoughtState.ActiveAtStage(num2);
		}
		return ThoughtState.Inactive;
	}
}
