using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler
{
    public static class SchedulerExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> originSet)
        {
            Random seed = new Random();
            return originSet == null ? null : originSet.ToList().OrderBy(x => seed.Next()).ToList();
        }
    }
}
