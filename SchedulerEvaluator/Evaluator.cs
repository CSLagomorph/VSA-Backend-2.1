using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator
{
    class Evaluator
    {
        public static readonly CritTyp[] criteriaTypes = { CritTyp.MathBreak };
        public static readonly double[] weights = { 1.0 };

        private Criteria[] criterias;

        public Evaluator() {
            CriteriaFactory fact = new CriteriaFactory(criteriaTypes, weights);
            criterias = fact.getCriterias();
        }

        public double evalaute(Schedule s) {
            double result = 0;
            foreach (Criteria c in criterias) 
                result += c.getResult(s);
            return result;
        }
    
    }
}
