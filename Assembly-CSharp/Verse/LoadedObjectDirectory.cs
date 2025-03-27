using System;
using System.Collections.Generic;

namespace Verse;

public class LoadedObjectDirectory
{
	private Dictionary<string, ILoadReferenceable> allObjectsByLoadID = new Dictionary<string, ILoadReferenceable>();

	private Dictionary<int, ILoadReferenceable> allThingsByThingID = new Dictionary<int, ILoadReferenceable>();

	public void Clear()
	{
		allObjectsByLoadID.Clear();
		allThingsByThingID.Clear();
	}

	public void RegisterLoaded(ILoadReferenceable reffable)
	{
		if (Prefs.DevMode)
		{
			string text = "[excepted]";
			try
			{
				text = reffable.GetUniqueLoadID();
			}
			catch (Exception)
			{
			}
			string text2 = "[excepted]";
			try
			{
				text2 = reffable.ToString();
			}
			catch (Exception)
			{
			}
			if (allObjectsByLoadID.TryGetValue(text, out var value))
			{
				string text3 = "";
				Log.Error(string.Concat("Cannot register ", reffable.GetType(), " ", text2, ", (id=", text, " in loaded object directory. Id already used by ", value.GetType(), " ", value.ToStringSafe(), ".", text3));
				return;
			}
		}
		try
		{
			allObjectsByLoadID.Add(reffable.GetUniqueLoadID(), reffable);
		}
		catch (Exception ex5)
		{
			string text4 = "[excepted]";
			try
			{
				text4 = reffable.GetUniqueLoadID();
			}
			catch (Exception)
			{
			}
			string text5 = "[excepted]";
			try
			{
				text5 = reffable.ToString();
			}
			catch (Exception)
			{
			}
			Log.Error(string.Concat("Exception registering ", reffable.GetType(), " ", text5, " in loaded object directory with unique load ID ", text4, ": ", ex5));
		}
		if (!(reffable is Thing thing))
		{
			return;
		}
		try
		{
			allThingsByThingID.Add(thing.thingIDNumber, reffable);
		}
		catch (Exception ex7)
		{
			string text6 = "[excepted]";
			try
			{
				text6 = reffable.ToString();
			}
			catch (Exception)
			{
			}
			Log.Error(string.Concat("Exception registering ", reffable.GetType(), " ", text6, " in loaded object directory with unique thing ID ", thing.thingIDNumber, ": ", ex7));
		}
	}

	public T ObjectWithLoadID<T>(string loadID)
	{
		if (loadID.NullOrEmpty() || loadID == "null")
		{
			return default(T);
		}
		if (allObjectsByLoadID.TryGetValue(loadID, out var value))
		{
			if (value == null)
			{
				return default(T);
			}
			try
			{
				return (T)value;
			}
			catch (Exception ex)
			{
				Log.Error(string.Concat("Exception getting object with load id ", loadID, " of type ", typeof(T), ". What we loaded was ", value.ToStringSafe(), ". Exception:\n", ex));
				return default(T);
			}
		}
		if (typeof(Thing).IsAssignableFrom(typeof(T)) && allThingsByThingID.TryGetValue(Thing.IDNumberFromThingID(loadID), out value))
		{
			Log.Warning("Could not resolve reference to Thing with loadID " + loadID + ". Resolving reference by using thingIDNumber instead (back-compat).");
			if (value == null)
			{
				return default(T);
			}
			try
			{
				return (T)value;
			}
			catch (Exception ex2)
			{
				Log.Error(string.Concat("Exception getting object with thing id ", Thing.IDNumberFromThingID(loadID), " of type ", typeof(T), ". What we loaded was ", value.ToStringSafe(), ". Exception:\n", ex2));
				return default(T);
			}
		}
		Log.Warning(string.Concat("Could not resolve reference to object with loadID ", loadID, " of type ", typeof(T), ". Was it compressed away, destroyed, had no ID number, or not saved/loaded right? curParent=", Scribe.loader.curParent.ToStringSafe(), " curPathRelToParent=", Scribe.loader.curPathRelToParent));
		return default(T);
	}
}
