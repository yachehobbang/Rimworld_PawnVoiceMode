using Verse;

namespace RimWorld;

public class QuestPart_Filter_AnyPawnAlive : QuestPart_Filter_AnyPawn
{
	protected override int PawnsCount
	{
		get
		{
			if (pawns.NullOrEmpty())
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < pawns.Count; i++)
			{
				if (!pawns[i].Destroyed)
				{
					num++;
				}
			}
			return num;
		}
	}
}
