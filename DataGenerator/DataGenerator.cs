using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;


namespace DataGenerator
{
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;
    using ApiCore;
    using FluentAssertions;
    using Models;
    using ScheduleEvaluator;
    using Scheduler;
    using DBConnection = Scheduler.DBConnection;
    using Preferences = Models.Preferences;

    public class Combination
    {
        public int SchoolId { get; set; }
        public int MajorId { get; set; }
    }

    [TestClass]
    public class DataGenerator
    {
        private DBConnection db;
        [TestInitialize]
        public void Initialize()
        {
            db = new DBConnection();
        }

        // This is commmented out to ignore this test. This test generates a lot of data
        // and should be ran sparingly as to not fill the database with a lot of redundant
        // test data.
        [TestMethod]
        public void GenerateData()
        {
            var insertedList = new List<int>();
            var coursePrefList = new List<CourseObject>();
            var combinations = new List<Combination>()
            {
                new Combination(){MajorId = 22, SchoolId = 6},
                new Combination(){MajorId = 32, SchoolId = 3},
                //new Combination(){MajorId = 11, SchoolId = 1},
                //new Combination(){MajorId = 16, SchoolId = 1},
                //new Combination(){MajorId = 20, SchoolId = 1},
                //new Combination(){MajorId = 20, SchoolId = 2},
                //new Combination(){MajorId = 22, SchoolId = 6},
                //new Combination(){MajorId = 40, SchoolId = 1},
                //new Combination(){MajorId = 41, SchoolId = 15},
                //new Combination(){MajorId = 46, SchoolId = 1},
                //new Combination(){MajorId = 48, SchoolId = 1}
            };
            
            foreach (var combination in combinations)
            {

                var courseObj = new CourseObject()
                {
                    school = combination.SchoolId.ToString(),
                    courses = "10",
                    credits = "10",
                    major = combination.MajorId.ToString(),
                    quarters = "20",
                    enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                    job = ((int)Constants.JobType.Unemployed).ToString(),
                    summer = "N"
                };
                coursePrefList.Add(courseObj);
                VaryEnrollment(courseObj, coursePrefList);
                VaryJob(courseObj, coursePrefList);
                VaryCredits(courseObj, coursePrefList);
                VaryMaxCreditsPerQuarter(courseObj, coursePrefList);
                GeneratePlan(coursePrefList, insertedList);

            }


            insertedList.Should().NotBeEmpty();
        }

        private void VaryMaxCreditsPerQuarter(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var quarters = new List<int>() { 1, 2, 4, 8, 9, 12, 15 };
            foreach (var credit in quarters)
            {
                var newCourseObj = new CourseObject()
                {
                    job = courseObj.job,
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = courseObj.credits,
                    summer = courseObj.summer,
                    enrollment = courseObj.enrollment,
                    quarters = credit.ToString()
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        [TestMethod]
        public void GetAllRating()
        {
            var schedule = new List<int>() { };
            var connection = new DBConnection();
            var eval = new Evaluator();
            var scheduleQuery = $"select GeneratedPlanId from GeneratedPlan where GeneratedPlanId >9238";
            var schedules = connection.ExecuteToDT(scheduleQuery);
            foreach (DataRow schedulesRow in schedules.Rows)
            {
                schedule.Add((int)schedulesRow["GeneratedPlanId"]);
            }


            foreach (int scheduleId in schedule)
            {
                var parameterQuery = $"select ParameterSetId from GeneratedPlan where GeneratedPlanId={scheduleId}";
                var parameterId = (int)connection.ExecuteToDT(parameterQuery).Rows[0]["ParameterSetId"];

                var parameterSetQuery = $"select ParameterSet.MajorID, SchoolID, TimePeriod, MaxNumberOfQuarters, NumberCoreCoursesPerQuarter, CreditsPerQuarter, SummerPreference, DepartmentId from ParameterSet join Major on ParameterSet.MajorID = Major.MajorID" +
                                        $" join TimePreference on TimePreference.TimePreferenceID = ParameterSet.TimePreferenceID" +
                                        $" where ParameterSetId = {parameterId}";
                var parameterSetResult = connection.ExecuteToDT(parameterSetQuery);

                var parameters = Preferences.ConvertFromDatabase(parameterSetResult, parameterId);

                var query = "select CourseNumber, QuarterID, YearID, Course.CourseId, Course.DepartmentID from StudyPlan" +
                            " join course on Course.CourseID = StudyPlan.CourseID" +
                            $" where GeneratedPlanID = {scheduleId}";

                var results = connection.ExecuteToDT(query);
                var model = ScheduleModel.ConvertFromDatabase(results, scheduleId, parameters);
                var rating = eval.evalaute(model);

                RatingHelper.UpdateWeakLabelScore(scheduleId, rating);

            }
        }

        [TestMethod]
        public void ComputerEngineeringPreReqRegularTest()
        {
            // Constants for a pre-determined major/school combo
            const string COMPUTER_ENGINEERING_MAJOR = "1";
            const string UNIVERSITY_WA = "1";
            const string MAX_QUARTERS = "15";

            // From my experience this seems to be a common schedule preference
            // set. Using this object as a concrete example to help debug and test.
            int schedId = PreferenceHelper.ProcessPreference(new CourseObject()
            {
                school = UNIVERSITY_WA,
                credits = "10",
                courses = "10",
                major = COMPUTER_ENGINEERING_MAJOR,
                quarters = MAX_QUARTERS,
                enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                job = ((int)Constants.JobType.Unemployed).ToString(),
                summer = "N"
            }, false);

            // Check that a schedule has been been made
            Assert.AreNotEqual(0, schedId);

            // The first check should be that all the prereqs are satisfied in the schedule.
            string result = db.ExecuteToString(getFulfillQueryString(schedId));

            // This query will return 0 if all of the prereqs are met.
            Assert.AreEqual(0, Int32.Parse(result));

            result = db.ExecuteToString(getScheduleQueryString(schedId));
            Debug.WriteLine("THIS IS THE OUTPUT for ID" + schedId + " " + result);
        }

        // Very similar to the above, although this method tests the shortest schedule.s
        [TestMethod]
        public void ComputerEngineeringPreReqShortTest()
        {
            // Constants for a pre-determined major/school combo
            const string COMPUTER_ENGINEERING_MAJOR = "1";
            const string UNIVERSITY_WA = "1";
            const string MAX_QUARTERS = "15";

            // From my experience this seems to be a common schedule preference
            // set. Using this object as a concrete example to help debug and test.
            int schedId = PreferenceHelper.ProcessPreference(new CourseObject()
            {
                school = UNIVERSITY_WA,
                credits = "10",
                courses = "10",
                major = COMPUTER_ENGINEERING_MAJOR,
                quarters = MAX_QUARTERS,
                enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                job = ((int)Constants.JobType.Unemployed).ToString(),
                summer = "N"
            }, true);

            // Check that a schedule has been been made
            Assert.AreNotEqual(0, schedId);

            // The first check should be that all the prereqs are satisfied in the schedule.
            string result = db.ExecuteToString(getFulfillQueryString(schedId));

            // This query will return 0 if all of the prereqs are met.
            Assert.AreEqual(0, Int32.Parse(result));

            result = db.ExecuteToString(getScheduleQueryString(schedId));
            Debug.WriteLine("THIS IS THE OUTPUT for ID" + schedId + " " + result);
        }

        private string getFulfillQueryString(int id)
        {
            return "SELECT COUNT(*) FROM (SELECT DISTINCT PrerequisiteCourseID as PID " +
                "FROM StudyPlan as sp, Prerequisite as p WHERE sp.CourseID = p.CourseID AND sp.GeneratedPlanID = " + id + ") as interim " +
                "LEFT JOIN StudyPlan as p ON interim.PID = p.CourseID " +
                "WHERE p.GeneratedPlanID is NULL";
        }

        private string getScheduleQueryString(int id)
        {
            return "SELECT   YearID as YID, CourseID as CID " +
                "FROM StudyPlan WHERE GeneratedPlanID = " + id +
                "ORDER BY QuarterID ASC";
        }

        private void VaryCredits(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var credits = new List<int>() { 5, 10, 15, 3 };
            foreach (var credit in credits)
            {
                var newCourseObj = new CourseObject()
                {
                    job = courseObj.job,
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = credit.ToString(),
                    summer = courseObj.summer,
                    enrollment = courseObj.enrollment,
                    quarters = courseObj.quarters
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        private void VaryJob(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var jobTypes = new List<Constants.JobType>() { Constants.JobType.FullTime, Constants.JobType.PartTime, Constants.JobType.Unemployed };
            foreach (var jobType in jobTypes)
            {
                var newCourseObj = new CourseObject()
                {
                    job = ((int)jobType).ToString(),
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = courseObj.credits,
                    summer = courseObj.summer,
                    enrollment = courseObj.enrollment,
                    quarters = courseObj.quarters
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        private void VaryEnrollment(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var enrollmentTypes = new List<Constants.EnrollmentType>() { Constants.EnrollmentType.FullTime, Constants.EnrollmentType.PartTime };
            foreach (var enrollmentType in enrollmentTypes)
            {
                var newCourseObj = new CourseObject()
                {
                    job = courseObj.job,
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = courseObj.credits,
                    summer = courseObj.summer,
                    enrollment = ((int)enrollmentType).ToString(),
                    quarters = courseObj.quarters
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        private void GeneratePlan(List<CourseObject> courseObj, List<int> insertedList)
        {
            foreach (var courseObject in courseObj)
            {
                var insertedId = PreferenceHelper.ProcessPreference(courseObject, true);
                insertedId.Should().NotBe(0);
                insertedList.Add(insertedId);
            }
        }
    }
}
