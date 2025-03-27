using RimWorld.Planet;

namespace Verse;

public sealed class MapInfo : IExposable
{
	private IntVec3 sizeInt;

	public MapParent parent;

	public bool isPocketMap;

	public bool disableSunShadows;

	public int Tile => parent?.Tile ?? (-1);

	public int NumCells => Size.x * Size.y * Size.z;

	public IntVec3 Size
	{
		get
		{
			return sizeInt;
		}
		set
		{
			sizeInt = value;
		}
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref sizeInt, "size");
		Scribe_Values.Look(ref isPocketMap, "isPocketMap", defaultValue: false);
		Scribe_Values.Look(ref disableSunShadows, "disableSunShadows", defaultValue: false);
		Scribe_References.Look(ref parent, "parent");
	}
}
