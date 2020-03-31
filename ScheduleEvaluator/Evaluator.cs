using System;
using System.Collections.Generic;
using System.Text;

// This class is the definition for the object that does the evaluation of schedules. A user
// must instantiate this class, and then call the evaluate method by passing in a Schedule object.
// The evaluate function lets each criteria object look at the schedule and determine whether or not
// it fulfills its requirement.

namespace ScheduleEvaluator
{
    class Evaluator
    {
        // These fields are hardcoded before program runtime. They define the preferences and the weights associated with
        // each preference. For now these two structures are going to stay as fields, but in the future they may migrate to passed
        // in paramaters.
        public static readonly CritTyp[] criteriaTypes = { CritTyp.AllPrereqs };
        public static readonly double[] weights = { 1.0 };

        // This field holds all of the criteria objects that are created by the Criteria factory.
        private Criteria[] criterias;

        // Constructor for the evaluator. Creates all of the criteria objects and stores in criterias. 
        // For now I dont believe that the criterias should be mutable. 
        public Evaluator() {
            CriteriaFactory fact;
            try
            {
                fact = new CriteriaFactory(criteriaTypes, weights);
            }
            catch(ArgumentException ag) 
            {
                Console.WriteLine("Error in creating Criterias: {0}", ag);
                throw;
            }

            criterias = fact.Criterias;
        }

        // The bread and butter of the class. At first this method doesn't look like much, but
        // by iterating over all of the criterias and having them evaluate the scheudle on their own
        // criteria this function is able to assign a score to the schedule.
        public double evalaute(Schedule s) {
            double result = 0;
            foreach (Criteria c in criterias) 
                result += c.getResult(s);
            return result;
        }
    
    }
}
