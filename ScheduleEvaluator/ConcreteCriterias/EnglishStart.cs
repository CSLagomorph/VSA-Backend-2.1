using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    public class EnglishStart : Criteria
    {
        const int ENGLISH_DEPARTMENT = 28;
        public EnglishStart(double weight) : base(weight)
        {
        }

        public override double getResult(ScheduleModel s)
        {
            // This algorithm works for all of the 'preferred english starts' in
            // the database right now. They are all apart of Department ID 28, my
            // guess is that we can evaluate if a course is an english course by
            // checking against Department ID. From my understanding this D-ID would
            // change based on college.
            foreach (Quarter q in s.Quarters) {
                foreach (Course c in q.Courses) {
                    if (c.DepartmentID == ENGLISH_DEPARTMENT) {
                        return (Int32.Parse(c.Description) == s.PreferenceSet.PreferredEnglishStart ?
                            1.0 : 0.0) * weight;
                    }
                }
            }
            // No english courses or school with dept ID that is not in the consts
            return 0.0;
        }
    }
}
