using System;
using System.Collections.Generic;
using System.Linq;

namespace Tmds.DotnetOC
{
    static class LinqExtensions
    {
        public static bool IsSubsetOf(this IEnumerable<string> toCheck, IEnumerable<string> set)
        {
            bool rv = !toCheck.Except(set, StringComparer.InvariantCulture).Any();
            return rv;
        }
    }
}