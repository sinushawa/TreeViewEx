using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Windows.Controls
{
    internal class SizesCache
    {
        Dictionary<int, double> cache;
        int highestIndex = -1;
        double average;
        double max;

        public SizesCache()
        {
            cache = new Dictionary<int, double>();
            Update();
        }

        public void AddOrChange(int index, double size)
        {
            if (cache.ContainsKey(index)) { cache[index] = size; return; }

            cache.Add(index, size);

            highestIndex = Math.Max(highestIndex, index);
        }

        public double this[int index]
        {
            get
            {
                return cache[index];
            }
        }

        public bool ContainsSize(int index)
        {
            return cache.ContainsKey(index);
        }

        public void CleanUp(int lastUsedIndex)
        {
            for (int i = lastUsedIndex + 1; i <= highestIndex; i++)
            {
                //if (cache.ContainsKey(i)) 
                cache.Remove(i);
            }

            highestIndex = lastUsedIndex;
        }

        public double GetAverage()
        {
            return average;
        }

        public double GetMax()
        {
            if(max <= 0.0 && cache.Count >0)Update();
            return max;
        }

        internal void Update()
        {
            if (cache.Values.Count < 1) { average = 0.0; return; }

            max = cache.Values.Max();
            average = cache.Values.Average();
        }
    }
}
