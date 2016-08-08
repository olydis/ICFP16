using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICFP16
{
    public static class ConstantsInf
    {
        public static readonly int MaxTimePerWorldMS = 60 * 1000 * 60 * 24;
        public static readonly int MaxTimePerGraphMS = 60 * 1000 * 60 * 24;
        public static readonly int MaxReOptimizationTimeMS = 60 * 1000 * 1;
        public static readonly bool ReOptimization = false;
    }
    public static class ConstantsFast
    {
        public static readonly int MaxTimePerWorldMS = 60 * 1000 * 1;
        public static readonly int MaxTimePerGraphMS = 60 * 1000 * 1;
        public static readonly int MaxReOptimizationTimeMS = 60 * 1000 * 1;
        public static readonly bool ReOptimization = false;
    }
    public static class ConstantsVeryFast
    {
        public static readonly int MaxTimePerWorldMS = 10 * 1000 * 1;
        public static readonly int MaxTimePerGraphMS = 20 * 1000 * 1;
        public static readonly int MaxReOptimizationTimeMS = 60 * 1000 * 1;
        public static readonly bool ReOptimization = false;
    }
    public class Constants
    {
        public static readonly int MaxTimePerWorldMS = 1 * 1000 * 1;
        public static readonly int MaxTimePerGraphMS = 3 * 1000 * 1;
        public static readonly int MaxReOptimizationTimeMS = 60 * 1000 * 1;
        public static readonly bool ReOptimization = false;

        public static readonly int MaxComplexityBonus = 1;
        public static readonly bool EnableCache = true;
        public static int MaxFacetsCount = 200;
        public static int MaxFacetsPerTypeCount = 16;
        public static bool TryWithoutEdges = false;
        public static readonly int SkipIfSmallerThan = 50;
    }
}
