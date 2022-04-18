﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Serenity.TypeScript.TsParser
{
    public static class TsExtensions
    {
        public static string Substring(this string str, int start, int? end = null)
        {
            return end == null ? str[start..] : str[start..(int)end];
        }
        public static string[] Exec(this Regex r, string text) => r.Match(text).Captures.Cast<string>().ToArray();
        public static bool Test(this Regex r, string text) => r.IsMatch(text);
        public static void Pop<T>(this List<T> list) => list.RemoveAt(0);
        
        public static string Slice(this string str, int start, int end = int.MaxValue)
        {
            if (start < 0)
                start += str.Length;
            if (end < 0)
                end += str.Length;

            start = Math.Min(Math.Max(start, 0), str.Length);
            end = Math.Min(Math.Max(end, 0), str.Length);
            if (end <= start)
                return string.Empty;

            return str.Substring(start, end - start);
        }

    }
}
