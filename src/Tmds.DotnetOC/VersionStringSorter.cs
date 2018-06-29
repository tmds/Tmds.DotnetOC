using System;
using System.Collections.Generic;

namespace Tmds.DotnetOC
{
    class VersionStringSorter : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            Version lhs;
            Version rhs;
            Version.TryParse(x, out lhs);
            Version.TryParse(y, out rhs);
            if (lhs != null && rhs != null)
            {
                return lhs.CompareTo(rhs);
            }
            else if (lhs != null)
            {
                return -1;
            }
            else if (rhs != null)
            {
                return 1;
            }
            else
            {
                return StringComparer.InvariantCulture.Compare(x, y);
            }
        }

        public static VersionStringSorter Sorter { get; } = new VersionStringSorter();

        public static void Sort(string[] array) => Array.Sort(array, Sorter);
    }
}