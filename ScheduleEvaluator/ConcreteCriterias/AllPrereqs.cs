using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    public class AllPrereqs : Criteria
    {
        public AllPrereqs(double weight) : base(weight) {
        }

        public override double getResult(ScheduleModel s)
        {
            throw new NotImplementedException();
        }
    }
}
