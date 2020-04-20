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
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

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
        public int MaxQuarters { get; set; }
        public int SchoolId { get; set; }

        public static Preferences ConvertFromDatabase(DataTable results, int id)
        {
            var pref = new Preferences();
            foreach (DataRow row in results.Rows)
            {
                var major = (int)row["MajorId"];
                var school = (int)row["SchoolId"];
                var maxNumQuarter = (int)row["MaxNumberOfQuarters"];
                int corePerQuarter = 0;
                if (row["NumberCoreCoursesPerQuarter"] != DBNull.Value)
                {
                    corePerQuarter = (int)row["NumberCoreCoursesPerQuarter"];
                }
                int credits = 0;
                if (row["CreditsPerQuarter"] != DBNull.Value)
                {
                    credits = (int)row["CreditsPerQuarter"];
                }
                
                var summer =(string)row["SummerPreference"];
                int department = 0;
                if (row["DepartmentId"] != DBNull.Value)
                {
                    department = (int)row["DepartmentId"];
                }
                pref.MaxQuarters = maxNumQuarter;
                pref.MajorID = major;
                pref.SchoolId = school;
                pref.CreditsPerQuarter = credits;
                pref.SummerPreference = summer == "Y";
                pref.DepartmentID = department;
                pref.CoreCoursesPerQuarter = corePerQuarter;
            }

            return pref;
        }
    }
}
