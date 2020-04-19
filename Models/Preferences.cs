/*
 * This class outlines a student's basic schedule preferences.
 * 
 * Each field corresponds to one of the 'ConcreteCriterias' that references
 * students' schedule preferences. Each instance of this class belongs to one
 * student, and can be used many times by the schedule evaluator to evaluate the
 * strength of a ScheduleModel with respect to the student's preferences.
 */

namespace Models
{
    using System;

    public class Preferences
    {

        // Fields correspond to preference-related 'ConcreteCriterias'
        public int MajorID { get; set; }
        public int CoreCoursesPerQuarter { get; set; }
        public int PreferredEnglishStart { get; set; }
        public int QuarterPreference { get; set; }
        public int TimePreference { get; set; }
        public int CreditsPerQuarter { get; set; }
        public Boolean SummerPreference { get; set; }
        public int PreferredMathStart { get; set; }
        public int DepartmentID { get; set; }
        public int CoreCountLastYear { get; set; }
        public int MaxQuarters
        {
            get
            {
                return this.MaxQuarters;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Max Quarters Must be Greater than 0");
                this.MaxQuarters = value;
            }
        }

    }
}
