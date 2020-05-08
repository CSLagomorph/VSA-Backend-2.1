﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using ScheduleEvaluator;
using System.Linq;
using Models;
using System.Data;
using ScheduleEvaluator.ConcreteCriterias;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace ScheduleEvaluatorTestFramework
{

    [TestClass]
    public class EvaluatorTest
    {
        private DBConnection conn;
        [TestInitialize]
        public void Initialize()
        {
            // Do any 'Constructor' type stuff here.
            conn = new DBConnection();
        }

        [TestMethod, TestCategory("Example")]
        public void ExampleTest()
        {
            const int GENERATED_PLAN_ID = 933; // 
            const int PARAMETER_SET_ID = 213; // Mechanical Engineering
            // Construct a few Schedule Models
            ScheduleModel sm = getScheduleFromDB(GENERATED_PLAN_ID); // Replace the int here with the actual schedule ID
            Evaluator eval = new Evaluator();

            // Construct a preference set.
            Preferences pref = GetPreferencesFromDB(PARAMETER_SET_ID); // Replace the int here with an actual preference set ID

            // Associate the schedule with a given preference set:
            sm.PreferenceSet = pref;

            // Get the score for the schedule associated with the preference set. NOTE: The preference set does not
            // dictate the criteria the schedule is evaluated against. To change which criterias to evaluate against 
            // change the array of CritTyp and Weights in `Evaluator.cs`
            double result = eval.evalaute(sm);

            // Include an Assert to signify the test passsing/failing.
            Assert.AreEqual(1, 1);
        }

        [TestMethod, TestCategory("Constructor")]
        public void TestConstructorValidSchema() {
            JObject criteria = JObject.Parse(File.ReadAllText("./../../../ScheduleEvaluator/JSONCriteriaWeights/TestCriteriaWeightsValid.json"));
            Evaluator eval = new Evaluator(criteria.ToString(), true);
        }

        [TestMethod, TestCategory("HTTP")]
        public void TestHTTPRequest()
        {
            PreRequisiteOrder eval = new PreRequisiteOrder(1.0);
            List<CourseNode> result;
            Task.Run(async () =>
            {
                result = await eval.getCourseNetwork(42.ToString());
                Assert.IsNotNull(result);
            }).GetAwaiter().GetResult();
            // Probably some better way to do this

        }
        
        [TestMethod, TestCategory("MathBreaks")]
        public void TestMathBreaksFromDataBase() {
            const int GENERATED_PLAN_ID = 933; // Contains breaks in the math sequence
            const int PARAMETER_SET_ID = 213;
            ScheduleModel sm = getScheduleFromDB(GENERATED_PLAN_ID); // Replace the int here with the actual schedule ID
            Evaluator eval = new Evaluator();

            // Construct a preference set.
            Preferences pref = GetPreferencesFromDB(PARAMETER_SET_ID); // Replace the int here with an actual preference set ID

            // Associate the schedule with a given preference set:
            sm.PreferenceSet = pref;
            Criteria mb = new MathBreaks(2.0);
            double result = mb.getResult(sm);
            Assert.AreEqual(result, 0.0);
        }

        [TestMethod, TestCategory("MathBreaks")]
        public void TestMathBreaksValidSchedule() {
            const int MATH_DEPT = 54;
            ScheduleModel sm = new ScheduleModel
            {
                Quarters = new List<Quarter> {
                    new Quarter{
                        Id = "1",
                        Courses = new List<Course>{
                            new Course{DepartmentID = MATH_DEPT}
                        }
                    },
                    new Quarter{
                        Id = "2",
                        Courses = new List<Course>{ 
                            new Course{DepartmentID = MATH_DEPT}
                        }
                    },
                    new Quarter{ 
                        Id = "3",
                        Courses = new List<Course>{ 
                            new Course{DepartmentID = MATH_DEPT}
                        }
                    },
                    new Quarter{ 
                        Id = "4",
                        Courses = new List<Course>{ 
                            new Course{DepartmentID = MATH_DEPT + 1}
                        }
                    },
                }
            };
            Criteria mb = new MathBreaks(1.0);
            double result = mb.getResult(sm);
            Assert.AreEqual(1.0, result);
        }

        [TestMethod, TestCategory("MathBreaks")]
        public void TestMathBreaksInvalidSchedule() {
            const int MATH_DEPT = 54;
            ScheduleModel sm = new ScheduleModel
            {
                Quarters = new List<Quarter> {
                    new Quarter{
                        Id = "1",
                        Courses = new List<Course>{
                            new Course{DepartmentID = MATH_DEPT}
                        }
                    },
                    new Quarter{
                        Id = "2",
                        Courses = new List<Course>{
                            new Course{DepartmentID = MATH_DEPT + 1}
                        }
                    },
                    new Quarter{
                        Id = "3",
                        Courses = new List<Course>{
                            new Course{DepartmentID = MATH_DEPT}
                        }
                    },
                    new Quarter{
                        Id = "4",
                        Courses = new List<Course>{
                            new Course{DepartmentID = MATH_DEPT + 1}
                        }
                    },
                }
            };
            Criteria mb = new MathBreaks(1.0);
            double result = mb.getResult(sm);
            Assert.AreEqual(0.0, result);
        }

        [TestMethod, TestCategory("MaxQuarters")]
        public void TestMaxQuartersValidSchedule()
        {
            ScheduleModel sm = new ScheduleModel
            {
                Quarters = new List<Quarter> {
                    new Quarter {
                        Id = "1"
                    },
                    new Quarter {
                        Id = "2"
                    },
                    new Quarter {
                        Id = "3"
                    },
                    new Quarter {
                        Id = "4"
                    }
                },
                PreferenceSet = new Preferences
                {
                    MaxQuarters = 4
                }
            };
            Criteria mq = new MaxQuarters(1.0);
            double result = mq.getResult(sm);
            Assert.AreEqual(1.0, result);
        }

        [TestMethod, TestCategory("MaxQuarters")]
        public void TestMaxQuartersInvalidSchedule()
        {
            ScheduleModel sm = new ScheduleModel
            {
                Quarters = new List<Quarter> {
                    new Quarter {
                        Id = "1"
                    },
                    new Quarter {
                        Id = "2"
                    },
                    new Quarter {
                        Id = "3"
                    },
                    new Quarter {
                        Id = "4"
                    }
                },
                PreferenceSet = new Preferences
                {
                    MaxQuarters = 3
                }
            };
            Criteria mq = new MaxQuarters(1.0);
            double result = mq.getResult(sm);
            Assert.AreEqual(0.0, result);
        }

        // These DB methods ARE NOT TESTED.
        private ScheduleModel getScheduleFromDB(int generatedPlanID)
        {
            ScheduleModel result = new ScheduleModel
            {
                Quarters = new List<Quarter>(),
                Id = generatedPlanID
            };

            string query = "SELECT CourseNumber as CNUM, QuarterID as QID, YearID as YID, " +
                "c.CourseId as CID, DepartmentID as DID " +
                "FROM StudyPlan as sp JOIN course as c ON c.CourseID = sp.CourseID " +
                $" WHERE GeneratedPlanID = {generatedPlanID}";
            DataTable table = conn.ExecuteToDT(query);
            foreach (DataRow row in table.Rows)
            {
                string courseName = (string)row["CNUM"];
                int quarter = (int)row["QID"];
                int year = (int)row["YID"];
                int courseId = (int)row["CID"];
                int deptId = (int)row["DID"];
                Quarter quarterItem = result.Quarters.FirstOrDefault(s => s.Id == $"{year}{quarter}" && s.Year == year);
                if (quarterItem == null)
                {
                    result.Quarters.Add(new Quarter() { Id = $"{year}{quarter}", Title = $"{year}-{quarter}", Year = year });
                    quarterItem = result.Quarters.First(s => s.Id == $"{year}{quarter}" && s.Year == year);
                }
                if (quarterItem.Courses == null)
                    quarterItem.Courses = new List<Course>();

                quarterItem.Courses.Add(new Course
                {
                    Description = courseName + $"({courseId})",
                    Id = courseName,
                    Title = courseName + $"({courseId})",
                    DepartmentID = deptId
                });
            }
            return result;
        }

        private Preferences GetPreferencesFromDB(int preferenceSetID)
        {
            Preferences result;
            string query = "SELECT sp.MajorID as MID, NumberCoreCoursesPerQuarter as CPQ, MaxNumberofQuarters as MNQ, " +
                "CreditsPerQuarter as CreditsPQ, SummerPreference as SP, PreferedMathStart as PMS, " +
                "PreferedEnglishStart as PES, QuarterPreferenceID as Q, TimePreferenceID as T, DepartmentID as DID " +
                "FROM ParameterSet as sp JOIN Major as m on m.MajorID = sp.MajorID " +
                $"WHERE sp.parameterSetID = {preferenceSetID}";
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
                TimePreference = row["T"].ToString(),
                CreditsPerQuarter = (int)row["CreditsPQ"],
                SummerPreference = ((string)row["SP"]).Equals("Yes"),
                PreferredMathStart = (int)row["PMS"],
                DepartmentID = (int)row["DID"]

            };

            return result;
        }

        private void WriteScheduleToDebug(ScheduleModel sm) {
            foreach (Quarter q in sm.Quarters)
            {
                Console.WriteLine("Quarter: " + q.Year);
                foreach (Course c in q.Courses)
                {
                    Console.WriteLine("\tCourse: " + c.Description);
                }
            }
        }

        private Exception ArgumentException(string v)
        {
            throw new NotImplementedException();
        }
    }
}
