using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator
{
    interface Criteria
    {
        public double getResult(Schedule s);
    }
}
