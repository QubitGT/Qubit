/*
 * Qubit Menu  Extensions/Dictionaries.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
 * https://github.com/QubitGT/Qubit
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Qubit.Extensions
{
    public static class Dictionaries
    {

        public static bool TryGetKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TKey actualKey)
        {
            return dictionary == null
                ? throw new ArgumentNullException(nameof(dictionary))
                : TryGetKeyInternal(dictionary, key, out actualKey, null);
        }

        public static bool TryGetKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TKey actualKey, IEqualityComparer<TKey> comparer)
        {
            return dictionary == null
                ? throw new ArgumentNullException(nameof(dictionary))
                : TryGetKeyInternal(dictionary, key, out actualKey, comparer);
        }

        public static bool TryGetKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs, TKey key, out TKey actualKey, IEqualityComparer<TKey> comparer = null)
        {
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));
            comparer ??= EqualityComparer<TKey>.Default;

            foreach (var kv in pairs)
            {
                if (!comparer.Equals(kv.Key, key)) continue;
                actualKey = kv.Key;
                return true;
            }

            actualKey = default!;
            return false;
        }

        public static bool TryGetKeyByValue(
            this Dictionary<string, string> dict,
            string value,
            out string key)
        {
            foreach (var kv in dict.Where(kv => string.Equals(kv.Value, value, StringComparison.OrdinalIgnoreCase)))
            {
                key = kv.Key;
                return true;
            }
            key = null!;
            return false;
        }


        private static bool TryGetKeyInternal<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, out TKey actualKey, IEqualityComparer<TKey> comparer)
        {
            comparer ??= TryGetComparerFromDictionary(dictionary) ?? EqualityComparer<TKey>.Default;
            foreach (var k in dictionary.Keys)
            {
                if (!comparer.Equals(k, key)) continue;
                actualKey = k;
                return true;
            }

            actualKey = default!;
            return false;
        }

        private static IEqualityComparer<TKey> TryGetComparerFromDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary is Dictionary<TKey, TValue> dict)
                return dict.Comparer;

            var dictType = dictionary.GetType();
            var prop = dictType.GetProperty("Comparer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop == null || !typeof(IEqualityComparer<TKey>).IsAssignableFrom(prop.PropertyType)) return null;
            IEqualityComparer<TKey> comp = prop.GetValue(dictionary) as IEqualityComparer<TKey>;
            return comp;
        }
    
    }
}
