using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Scheduler;


namespace DataGenerator
{
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using ApiCore;
    using FluentAssertions;

    [TestClass]
    public class DataGenerator
    {
        private DBConnection db;
        [TestInitialize]
        public void Initialize() {
            db = new DBConnection();
        }
        
        // This is commmented out to ignore this test. This test generates a lot of data
        // and should be ran sparingly as to not fill the database with a lot of redundant
        // test data.
        //[TestMethod]
        public void GenerateData()
        {
            var insertedList = new List<int>();
            var coursePrefList = new List<CourseObject>();
            var courseObj = new CourseObject()
            {
                school = "6",
                credits = "10",
                major = "22",
                quarters = "50",
                enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                job = ((int)Constants.JobType.Unemployed).ToString(),
                summer = "N"
            };
            coursePrefList.Add(courseObj);
            VaryEnrollment(courseObj, coursePrefList);
            VaryJob(courseObj, coursePrefList);
            VaryCredits(courseObj, coursePrefList);
            GeneratePlan(coursePrefList, insertedList);
            insertedList.Should().NotBeEmpty();

        }

        [TestMethod]
        public void ComputerEngineeringPreReqRegularTest() {
            // Constants for a pre-determined major/school combo
            const string COMPUTER_ENGINEERING_MAJOR = "1";
            const string UNIVERSITY_WA = "1";
            const string MAX_QUARTERS = "15";

            // From my experience this seems to be a common schedule preference
            // set. Using this object as a concrete example to help debug and test.
            int schedId = Preferences.ProcessPreference(new CourseObject()
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
        public void ComputerEngineeringPreReqShortTest() {
            // Constants for a pre-determined major/school combo
            const string COMPUTER_ENGINEERING_MAJOR = "1";
            const string UNIVERSITY_WA = "1";
            const string MAX_QUARTERS = "15";

            // From my experience this seems to be a common schedule preference
            // set. Using this object as a concrete example to help debug and test.
            int schedId = Preferences.ProcessPreference(new CourseObject()
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

        private string getFulfillQueryString(int id) { 
            return "SELECT COUNT(*) FROM (SELECT DISTINCT PrerequisiteCourseID as PID " + 
                "FROM StudyPlan as sp, Prerequisite as p WHERE sp.CourseID = p.CourseID AND sp.GeneratedPlanID = " + id + ") as interim " + 
                "LEFT JOIN StudyPlan as p ON interim.PID = p.CourseID " +
                "WHERE p.GeneratedPlanID is NULL";
        }

        private string getScheduleQueryString(int id) {
            return "SELECT   YearID as YID, CourseID as CID " +
                "FROM StudyPlan WHERE GeneratedPlanID = " + id +
                "ORDER BY QuarterID ASC";
        }

        private void VaryCredits(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var credits = new List<int>() { 5, 10, 15, 3, 1 };
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

                var insertedId = Preferences.ProcessPreference(courseObject, true);
                insertedId.Should().NotBe(0);
                insertedList.Add(insertedId);



            }
        }
    }
}
