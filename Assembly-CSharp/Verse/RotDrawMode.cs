using System;

namespace Verse;

[Flags]
public enum RotDrawMode : byte
{
	Fresh = 0,
	Rotting = 1,
	Dessicated = 2
}
