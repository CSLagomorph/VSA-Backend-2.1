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
            // Construct a preference set.

            // Evaluate against ONE criteria and make sure
            // Everything checks out

            // Include an Assert to signify the test passsing/failing.
            Assert.AreEqual(1, 1);
        }
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
                "PreferredEnglishStart as PES, Quarter as Q, TimePeriod as T " +
                "FROM ParameterSet as ps JOIN Quarter as q on ps.QuarterPreferenceID = q.QuarterID " +
                "JOIN TimePreference as tp on tp.TimePreferenceID = ps.TimePreferenceID " +
                $"WHERE ps.parameterSetID = {preferenceSetID}";
            DataTable table = conn.ExecuteToDT(query);
            
            if (table.Rows.Count == 0) 
                throw ArgumentException("PreferenceSetID not Found in Database");
            if (table.Rows.Count != 1)
                throw ArgumentException("PreferenceSetID returned more than one Preference Set");

            // FIXME: This is guaranteed to iterate once, but how to access just
            // first row with iteration?
            DataRow row = table.Rows[0];
            
            result = new Preferences()
            {
                MaxQuarters = (int)row["MNQ"],
                MajorID = (int)row["MID"],
                CoreCoursesPerQuarter = (int)row["CPQ"],
                // This and the Prefererred Math start, have no idea what
                // type they are of
                PreferredEnglishStart = (int)row["PES"],
                QuarterPreference = (string)row["Q"],
                TimePreference = (string)row["T"],
                CreditsPerQuarter = (int)row["CreditsPQ"],
                SummerPreference = ((string)row["SP"]).Equals("Yes"),
                PreferredMathStart = (int)row["PMS"]

            };

            return result;
        }

        private Exception ArgumentException(string v)
        {
            throw new NotImplementedException();
        }
    }
}
