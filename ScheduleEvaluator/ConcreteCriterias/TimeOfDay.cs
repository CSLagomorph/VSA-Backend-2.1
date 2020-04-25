using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    class TimeOfDay : Criteria
    {
        public TimeOfDay(double weight) : base(weight)
        {

        }

        public override double getResult(ScheduleModel s)
        {
            string timePreference = s.PreferenceSet.TimePreference;

            // If time preference is set to "Any", this criterion passes
            if (string.Equals(timePreference, "Any")) return 1.0;

            int numOutsideTimePref = 0;
            foreach (Quarter q in s.Quarters)
            {
                foreach (Course c in q.Courses)
                {
                    // Compares time of each course in the ScheduleModel to the
                    // time preference of its PreferenceSet; increments count of
                    // how many courses don't align with time preference.
                    if (!string.Equals(c.TimeOfDay, timePreference))
                    {
                        numOutsideTimePref++;
                    }
                }
            }
            // May we want to change this binary return.
            return (numOutsideTimePref > 0 ? 0.0 : 1.0) * weight;
        }
    }
}
