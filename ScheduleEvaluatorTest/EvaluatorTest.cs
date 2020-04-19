using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using ScheduleEvaluator;
using System.Data;
using System.Linq;

namespace ScheduleEvaluatorTest
{
    using Models;

    [TestClass]
    public class EvaluatorTest
    {
        private DBConnection conn;
        [TestInitialize]
        public void Initialize() {
            // Do any 'Constructor' type stuff here.
            conn = new DBConnection();
        }

        [TestMethod]
        public void ExampleTest() {
            // Construct a few Schedule Models
            ScheduleModel sm = getScheduleFromDB(12); // Replace the int here with the actual schedule ID
            Evaluator eval = new Evaluator();

            // Construct a preference set.
            Preferences pref = GetPreferencesFromDB(10); // Replace the int here with an actual preference set ID

            // Associate the schedule with a given preference set:
            sm.PreferenceSet = pref;

            // Get the score for the schedule associated with the preference set. NOTE: The preference set does not
            // dictate the criteria the schedule is evaluated against. To change which criterias to evaluate against 
            // change the array of CritTyp and Weights in `Evaluator.cs`
            double result = eval.evalaute(sm);

            // Include an Assert to signify the test passsing/failing.
            Assert.AreEqual(1, 1);
        }

        // These DB methods ARE NOT TESTED.
        private ScheduleModel getScheduleFromDB(int generatedPlanID) {
            ScheduleModel result = new ScheduleModel
            {
                Quarters = new List<Quarter>(),
                Id = generatedPlanID
            };

            string query = "SELECT CourseNumber as CNUM, QuarterID as QID, YearID as YID, " +
                "Course.CourseId as CID, DepartmentID as DID " +
                "FROM StudyPlan as sp JOIN course as c ON c.CourseID = sp.CourseID " +
                $" WHERE GeneratedPlanID = {generatedPlanID}";
            DataTable table = conn.ExecuteToDT(query);
            foreach (DataRow row in table.Rows) {
                string courseName = (string)row["CNUM"];
                int quarter = (int)row["QID"];
                int year = (int)row["YID"];
                int courseId = (int)row["CID"];
                int deptId = (int)row["DID"];
                Quarter quarterItem = result.Quarters.FirstOrDefault(s => s.Id == $"{year}{quarter}" && s.Year == year);
                if (quarterItem == null) {
                    result.Quarters.Add(new Quarter() { Id = $"{year}{quarter}", Title = $"{year}-{quarter}", Year = year });
                    quarterItem = result.Quarters.First(s => s.Id == $"{year}{quarter}" && s.Year == year);
                }
                if (quarterItem.Courses == null)
                    quarterItem.Courses = new List<Course>();
               
                quarterItem.Courses.Add(new Course { 
                    Description = courseName + $"({courseId})", 
                    Id = courseName, 
                    Title = courseName + $"({courseId})",
                    DepartmentID = deptId
                });
            }
            return result;
        }

        private Preferences GetPreferencesFromDB(int preferenceSetID) {
            Preferences result;
            string query = "SELECT MajorID as MID, NumberCoreCoursesPerQuarter as CPQ, MaxNumberofQuarters as MNQ, " +
                "CreditsPerQuarter as CreditsPQ, SummerPreference as SP, PreferredMathStart as PMS, " +
                "PreferredEnglishStart as PES, QuarterPreferenceID as Q, TimePreferenceID as T, DepartmentID as DID " +
                "FROM ParameterSet as ps " +
                "JOIN Major as m on m.MajorID = sp.MajorID" +
                $"WHERE ps.parameterSetID = {preferenceSetID}";
            DataTable table = conn.ExecuteToDT(query);
            
            if (table.Rows.Count == 0) 
                throw ArgumentException("PreferenceSetID not Found in Database");
            if (table.Rows.Count != 1)
                throw ArgumentException("PreferenceSetID returned more than one Preference Set");


            DataRow row = table.Rows[0];
            
            result = new Preferences()
            {
                MaxQuarters = (int)row["MNQ"],
                MajorID = (int)row["MID"],
                CoreCoursesPerQuarter = (int)row["CPQ"],
                // This and the Prefererred Math start, have no idea what
                // type they are of
                PreferredEnglishStart = (int)row["PES"],
                QuarterPreference = (int)row["Q"],
                TimePreference = (int)row["T"],
                CreditsPerQuarter = (int)row["CreditsPQ"],
                SummerPreference = ((string)row["SP"]).Equals("Yes"),
                PreferredMathStart = (int)row["PMS"],
                DepartmentID = (int)row["DID"]

            };

            return result;
        }

        private Exception ArgumentException(string v)
        {
            throw new NotImplementedException();
        }
    }
}
