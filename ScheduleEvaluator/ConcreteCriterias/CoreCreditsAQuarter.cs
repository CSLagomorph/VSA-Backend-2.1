using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    class CoreCreditsAQuarter : Criteria
    {
        public CoreCreditsAQuarter(double weight) : base(weight)
        {
            throw new NotImplementedException();
        }

        public override double getResult(ScheduleModel s)
        {
            int numQuartersOver = 0;
            foreach (Quarter q in s.Quarters) {
                int coreCredits = 0;
                foreach (Course c in q.Courses) {
                    // In Preference Object grab the major ID and figure out
                    // which department the major is in. IF the department for
                    // the major is the same as the department for the class is 
                    // the same... Then we will assume that this is a core credit.
                    if (c.DepartmentID == 1) { //replace 1 with assumed major from preferences object
                        coreCredits++;
                    }
                }
                if (coreCredits > 1) { // replace with student preference here. 
                    numQuartersOver++;
                }
            }
            return numQuartersOver > 0 ? 0.0 : 1.0;
        }
    }
}
