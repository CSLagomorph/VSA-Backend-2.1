using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    // For the criterias that evaluate whether or not there are breaks
    // ask if there is a threshold for number of quarters that are a break?
    public class MajorSpecificBreaks : Criteria
    {
        public MajorSpecificBreaks(double weight) : base(weight)
        {
        }

        public override double getResult(ScheduleModel s)
        {
            Quarter prevQuarter = null;
            int totalGap = 0;
            foreach (Quarter q in s.Quarters) {
                if (hasMajorCourse(q, s.PreferenceSet.DepartmentID)) {
                    if (prevQuarter == null)
                    {
                        prevQuarter = q;
                        continue;
                    }
                    
                    Quarter nextQuarter = q;

                   
                    int prevQID = Int32.Parse(prevQuarter.Id);
                    int nextQID = Int32.Parse(nextQuarter.Id);
                    if (prevQID + 1 != nextQID) 
                        totalGap += nextQID - prevQID;
                    prevQuarter = nextQuarter;
                
                }
            }
            return (totalGap > 0 ? 0.0 : 1.0) * weight;
        }

        private Boolean hasMajorCourse(Quarter q, int deptID) {
            foreach (Course c in q.Courses)
            {
                if (c.DepartmentID == deptID)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
