using System;
using System.Collections.Generic;
using System.Text;

// Parent class of all of the Criteria classes in the 
// ConcreteCriterias folder. 

namespace ScheduleEvaluator
{
    abstract class Criteria
    {
        private readonly double weight;
        public Criteria(double weight) {
            this.weight = weight;
        }
        abstract public double getResult(ScheduleModel s);
    }
}
