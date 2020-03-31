using System;
using System.Collections.Generic;
using System.Text;
using ScheduleEvaluator.ConcreteCriterias;

namespace ScheduleEvaluator
{
    class CriteriaFactory
    {
        public Criteria[] Criterias { get; }

        public CriteriaFactory(CritTyp[] types, double[] weights) {
            // Check to make sure the paramaters are legal.
            if (types.Length != weights.Length) 
                throw new ArgumentException("Must have equal number of weights and types");
            
            if (types.Length == 0 || types.Length == 0)
                throw new ArgumentException("Criteria Types and Weights must have elements");

            double sum = 0;
            foreach (double val in weights)
                sum += val;

            if (sum != 1.0)
                throw new ArgumentException("Weights must sum to 1.0");

            Criterias = new Criteria[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                CritTyp ct = types[i];
                double w = weights[i];
                if (ct == CritTyp.AllPrereqs)
                    Criterias[i] = new AllPrereqs(w);
                else if (ct == CritTyp.CoreClassesLastYear)
                    Criterias[i] = new CoreClassesLastYear(w);
                else if (ct == CritTyp.CoreCreditsAQuarter)
                    Criterias[i] = new CoreCreditsAQuarter(w);
                else if (ct == CritTyp.ElectiveRelevancy)
                    Criterias[i] = new ElectiveRelevancy(w);
                else if (ct == CritTyp.EnglishTime)
                    Criterias[i] = new EnglishTime(w);
                else if (ct == CritTyp.MajorSpecificBreaks)
                    Criterias[i] = new MajorSpecificBreaks(w);
                else if (ct == CritTyp.MathBreaks)
                    Criterias[i] = new MathBreaks(w);
                else if (ct == CritTyp.MaxQuarters)
                    Criterias[i] = new MaxQuarters(w);
                else if (ct == CritTyp.PreRequisiteOrder)
                    Criterias[i] = new PreRequisiteOrder(w);
                else if (ct == CritTyp.TimeOfDay)
                    Criterias[i] = new TimeOfDay(w);
                else
                    throw new ArgumentException("Illegal Criteria Type");
            }
        }
    }

}
