/*
 * This class outlines a student's basic schedule preferences.
 * 
 * Each field corresponds to one of the 'ConcreteCriterias' that references
 * students' schedule preferences. Each instance of this class belongs to one
 * student, and can be used many times by the schedule evaluator to evaluate the
 * strength of a ScheduleModel with respect to the student's preferences.
 */

namespace ScheduleEvaluator
{
    class Preferences
    {

        // Fields correspond to preference-related 'ConcreteCriterias'
        private int maxQuarters;

        public Preferences(int maxQtr)
        {
            maxQuarters = maxQtr;
        }

        // Get and Set for MaxQuarters criterion
        public int getMaxQuarters()
        {
            return maxQuarters;
        }
        public void set(int max)
        {
            if (max < 0)
            {
                throw new ArgumentException();
            }
            maxQuarters = max;
        }
    }
}
