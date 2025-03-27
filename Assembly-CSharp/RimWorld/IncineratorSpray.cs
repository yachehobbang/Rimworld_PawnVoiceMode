using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class IncineratorSpray : Thing
{
	private List<IncineratorProjectileMotion> projectiles = new List<IncineratorProjectileMotion>();

	private int numComplete;

	private int NumAlive => projectiles.Count;

	private bool AllComplete => numComplete >= NumAlive;

	public override void Tick()
	{
		int i = 0;
		for (int count = projectiles.Count; i < count; i++)
		{
			IncineratorProjectileMotion value = projectiles[i];
			bool num = value.Alpha == 1f;
			value.Tick(base.MapHeld);
			bool flag = value.Alpha == 1f;
			projectiles[i] = value;
			if (!num && flag)
			{
				numComplete++;
			}
		}
		if (AllComplete)
		{
			Destroy();
		}
	}

	public void Add(IncineratorProjectileMotion proj)
	{
		projectiles.Add(proj);
	}
}
