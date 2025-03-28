using System;
using System.Collections.Generic;

public static class ListExtensions
{
    public static void AddOrReplace<T>(this List<T> list, T newItem)
    {
        int index = list.IndexOf(newItem);
        if (index != -1)
        {
            list[index] = newItem;
        }
        else
        {
            list.Add(newItem);
        }
    }
}

