using System;
using System.Collections.Generic;

namespace Verse;

public static class GenList
{
	public static int CountAllowNull<T>(this IList<T> list)
	{
		return list?.Count ?? 0;
	}

	public static bool NullOrEmpty<T>(this IList<T> list)
	{
		if (list != null)
		{
			return list.Count == 0;
		}
		return true;
	}

	public static bool HasData<T>(this IList<T> list)
	{
		if (list != null)
		{
			return list.Count >= 1;
		}
		return false;
	}

	public static List<T> ListFullCopy<T>(this List<T> source)
	{
		List<T> list = new List<T>(source.Count);
		for (int i = 0; i < source.Count; i++)
		{
			list.Add(source[i]);
		}
		return list;
	}

	public static List<T> ListFullCopyOrNull<T>(this List<T> source)
	{
		return source?.ListFullCopy();
	}

	public static void ClearValueLists<T, J>(this Dictionary<T, List<J>> source)
	{
		foreach (KeyValuePair<T, List<J>> item in source)
		{
			item.Value.Clear();
		}
	}

	public static void ClearAndPoolValueLists<T, V>(this Dictionary<T, List<V>> source)
	{
		foreach (List<V> value in source.Values)
		{
			value.Clear();
			SimplePool<List<V>>.Return(value);
		}
		source.Clear();
	}

	public static void RemoveDuplicates<T>(this List<T> list, Func<T, T, bool> comparer = null) where T : class
	{
		if (list.Count <= 1)
		{
			return;
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < num; i++)
			{
				if ((comparer == null && list[num] == list[i]) || (comparer != null && comparer(list[num], list[i])))
				{
					list.RemoveAt(num);
					break;
				}
			}
		}
	}

	public static void TruncateToLength<T>(this List<T> list, int maxLength)
	{
		if (list.Count != 0 && list.Count > maxLength)
		{
			list.RemoveRange(maxLength, list.Count - maxLength);
		}
	}

	public static void Shuffle<T>(this IList<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = Rand.RangeInclusive(0, num);
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}

	public static void InsertionSort<T>(this IList<T> list, Comparison<T> comparison)
	{
		int count = list.Count;
		for (int i = 1; i < count; i++)
		{
			T val = list[i];
			int num = i - 1;
			while (num >= 0 && comparison(list[num], val) > 0)
			{
				list[num + 1] = list[num];
				num--;
			}
			list[num + 1] = val;
		}
	}

	public static bool NotNullAndContains<T>(this IList<T> list, T element)
	{
		if (list.NullOrEmpty())
		{
			return false;
		}
		return list.Contains(element);
	}

	public static bool RemoveAll_IfNotAll<T, ContextType>(this List<T> list, ContextType context, Predicate<ContextType, T> predicate)
	{
		int num = list.Count - 1;
		while (num >= 0 && !predicate(context, list[num]))
		{
			num--;
		}
		if (num < 0)
		{
			return false;
		}
		T item = list[num];
		list.RemoveRange(num, list.Count - num);
		list.RemoveAll(context, predicate, negatePredicate: true);
		list.Add(item);
		return true;
	}

	public static int RemoveAll<T, ContextType>(this List<T> list, ContextType context, Predicate<ContextType, T> predicate, bool negatePredicate = false)
	{
		bool flag = !negatePredicate;
		int i;
		for (i = 0; i < list.Count && predicate(context, list[i]) != flag; i++)
		{
		}
		if (i == list.Count)
		{
			return 0;
		}
		for (int j = i + 1; j < list.Count; j++)
		{
			if (predicate(context, list[j]) != flag)
			{
				list[i++] = list[j];
			}
		}
		int num = list.Count - i;
		list.RemoveRange(i, num);
		return num;
	}

	public static void Swap<T>(this IList<T> list, int a, int b)
	{
		T val = list[b];
		T val2 = list[a];
		T val4 = (list[a] = val);
		val4 = (list[b] = val2);
	}

	public static T GetLast<T>(this IList<T> source)
	{
		if (source.Count == 0)
		{
			throw new IndexOutOfRangeException("Attempted to get the last element of a list with zero length.");
		}
		return source[source.Count - 1];
	}
}
