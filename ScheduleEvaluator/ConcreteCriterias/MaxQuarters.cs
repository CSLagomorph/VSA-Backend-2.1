using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    class MaxQuarters : Criteria
    {
        public MaxQuarters(double weight) : base(weight)
        {

        }

        // Validates that the number of quarters scheduled do not exceed 
        // the preferred number of quarters scheduled.
        // Returns the difference between preferred number of quarters and
        // scheduled number of quarters. 
        public override double getResult(ScheduleModel s, Preferences p)
        {
            return Math.Abs(s.Quarters.size() - p.getMaxQuarters());
        }
    }
}
