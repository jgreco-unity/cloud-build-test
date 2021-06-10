using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.AutomatedQA
{

    public static class AutomatedQaTools
    {

        public static bool SequenceEqual<T>(this List<T> list, List<T> otherList)
        {
            if (list.Count != otherList.Count) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].Equals(otherList[i])) return false;
            }
            return true;
        }

        public static List<X> Select<X, T>(this T[] list, Func<T, X> predicate)
        {
            List<X> values = new List<X>();
            foreach (T item in list)
            {
                values.Add(predicate.Invoke(item));
            }
            return values;
        }

        public static List<T> Prepend<T>(this List<T> list, T item)
        {
            List<T> result = new List<T>() { item };
            result.AddRange(list);
            return result;
        }

        public static List<T> PrependRange<T>(this List<T> list, List<T> items)
        {
            List<T> result = items;
            result.AddRange(list);
            return result;
        }

        public static List<T> AddAtAndReturnNewList<T>(this List<T> list, int index, T item)
        {
            List<T> results = new List<T>();
            if (index >= list.Count)
            {
                results.Add(item);
            }
            else if (index < 0)
            {
                results = new List<T>() { item };
                results.AddRange(list);
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == index)
                        results.Add(item);
                    results.Add(list[i]);
                }
            }
            return results;
        }

        public static bool AnyMatch<T>(this T[] array, Func<T, bool> predicate)
        {
            foreach (T item in array)
            {
                if (predicate.Invoke(item))
                    return true;
            }
            return false;
        }

        public static bool AnyMatch<T>(this List<T> list, Func<T, bool> predicate)
        {
            foreach (T item in list)
            {
                if (predicate.Invoke(item))
                    return true;
            }
            return false;
        }

        public static bool Any<T>(this List<T> list)
        {
            return list.Count > 0;
        }

        public static bool Any<T>(this T[] array)
        {
            return array.Length > 0;
        }

        public static bool Contains<T>(this T[] array, T item)
        {
            return array.ToList().Contains(item);
        }

        public static List<X> Select<X, T>(this List<T> list, Func<T, X> predicate)
        {
            return Select(list.ToArray(), predicate);
        }

        public static List<T> ToList<T>(this T[] array)
        {
            return new List<T>(array);
        }

        public static List<T> ToList<T>(this IEnumerable<T> enumerable)
        {
            return new List<T>(enumerable);
        }

        public static T First<T>(this List<T> list)
        {
            if (!list.Any()) 
            {
                throw new UnityException("List provided to AutomatedQATools.First() was empty. Cannot invoke First() on an empty list. Check for list being empty before invoking First().");
            }
            return list[0];
        }

        public static T First<T>(this T[] array)
        {
            if (!array.Any())
            {
                throw new UnityException("Array provided to AutomatedQATools.First() was empty. Cannot invoke First() on an empty array. Check for array being empty before invoking First().");
            }
            return array[0];
        }

        public static T Last<T>(this List<T> list)
        {
            if (!list.Any())
            {
                throw new UnityException("List provided to AutomatedQATools.Last() was empty. Cannot invoke Last() on an empty list. Check for list being empty before invoking Last().");
            }
            else
            {
                return list[list.Count - 1];
            }
        }

        public static T Last<T>(this T[] array)
        {
            if (!array.Any())
            {
                throw new UnityException("Array provided to AutomatedQATools.Last() was empty. Cannot invoke Last() on an empty array. Check for array being empty before invoking Last().");
            }
            else
            {
                return array[array.Length - 1];
            }
        }

    }

}