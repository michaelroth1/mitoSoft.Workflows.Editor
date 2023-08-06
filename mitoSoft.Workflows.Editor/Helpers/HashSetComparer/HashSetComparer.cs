using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mitoSoft.Workflows.Editor.Helpers.HashSetComparer
{
    public class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
    {
        public bool Equals(HashSet<T> x, HashSet<T> y)
        {
            return x.SetEquals(y);
        }

        public int GetHashCode(HashSet<T> obj)
        {
            int hashcode = 0;
            foreach (T t in obj)
            {
                hashcode = t.GetHashCode();
            }
            return hashcode;
        }
    }

    public static class DictExtensions
    { 
        public static void ChangeKey<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey fromKey, TKey toKey)
        {
            TValue value = dic[fromKey];
            dic.Remove(fromKey);
            dic[toKey] = value;
        }
    }

}
