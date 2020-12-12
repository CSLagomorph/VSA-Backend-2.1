using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    public class MathBreaks : Criteria
    {
        const int MATH_DEPT = 54;
        public MathBreaks(double weight) : base(weight)
        {
        }

        public override double getResult(ScheduleModel s)
        {
            Quarter prevQuarter = null;
            int totalGap = 0;
            var startMath = false;
            var contMissing = 0;
            foreach (Quarter q in s.Quarters)
            {
                if (hasMathCourse(q))
                {
                    contMissing = 0;
                    if (!startMath)
                    {
                        startMath = true;
                    }

                }
                else
                {
                    contMissing++;
                    //its possible we finished all required math classes
                    if (contMissing > 4)
                    {
                        continue;
                    }
                    if (startMath)
                    {
                        totalGap++;
                    }
                }
            }
            return (totalGap > 3 ? 0.0 : 1.0) * weight;
        }

        private Boolean hasMathCourse(Quarter q)
        {
            foreach (Course c in q.Courses)
            {
                if (c.DepartmentID == MATH_DEPT)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
