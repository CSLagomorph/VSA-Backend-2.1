using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore
{
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Scheduler;
    using Scheduler.Algorithms;

    public static class PreferenceHelper
    {
        public static int ProcessPreference(CourseObject content, bool preferShortest = true)
        {
            var DBPlugin = new DBConnection();
            var majorId = Convert.ToInt32(content.major);
            var schoolId = Convert.ToInt32(content.school);
            var job = Convert.ToInt32(content.job);
            var enrollment = Convert.ToInt32(content.enrollment);
            var coreCourses = Convert.ToInt32(content.courses);
            var quarters = Convert.ToInt32(content.quarters);
            var creditPerQuarter = Convert.ToInt32(content.credits);
            var summer = content.summer;

            //get the departmentId
            var query = "select DepartmentId from Major" +
                        $" where MajorId = {majorId}";

            var results = DBPlugin.ExecuteToDT(query);
            var department = (int)results.Rows[0]["DepartmentId"];
            var preferences = new Models.Preferences()
            {
                CoreCoursesPerQuarter = coreCourses,
                CreditsPerQuarter = creditPerQuarter,
                MajorID = majorId,
                MaxQuarters = quarters,
                QuarterPreference = quarters,
                SummerPreference = summer == "Y",
                DepartmentID = department
            };
            DBPlugin.ExecuteToString($"insert into ParameterSet (MajorId, SchoolId, JobTypeID, TimePreferenceId, QuarterPreferenceId, DateAdded, NumberCoreCoursesPerQuarter, " +
                                     $"MaxNumberOfQuarters, CreditsPerQuarter, SummerPreference, EnrollmentTypeId, Status, LastDateModified) values ({majorId}, {schoolId}, {job}, {1}, {1}, '{DateTime.UtcNow}', {coreCourses}," +
                                     $"{quarters}, {creditPerQuarter}, '{summer}', {enrollment}, {1}, '{DateTime.UtcNow}')");
            var insertedId = DBPlugin.ExecuteToString("SELECT IDENT_CURRENT('ParameterSet')");
            var preferenceId = Convert.ToInt32(insertedId);
            var insertedSchedule = SaveSchedule(preferenceId, preferShortest, preferences);
            return insertedSchedule;
        }

        private static int SaveSchedule(int id, bool preferShortest, Models.Preferences preferences)
        {
            var scheduler = new OpenShopGAScheduler(id,preferences, preferShortest);
            var schedule = scheduler.CreateSchedule(preferShortest);

            var scheduleModel = schedule.ConvertToScheduleModel();

            int insertedId = 0;
            var DBPlugin = new DBConnection();
            double rating = schedule.Rating;
            try
            {
                var schedulerSettings = JsonConvert.SerializeObject(schedule.ScheduleSettings);
                DBPlugin.ExecuteToString(
                    $"insert into GeneratedPlan (Name, ParameterSetID, DateAdded, LastDateModified, Status, SchedulerName, SchedulerSettings, WeakLabelScore) " +
                    $"Values ('latest', {id}, '{DateTime.UtcNow}', '{DateTime.UtcNow}', {1}, '{schedule.SchedulerName}', '{schedulerSettings}', {rating})");
                var idString = DBPlugin.ExecuteToString("SELECT IDENT_CURRENT('GeneratedPlan')");
                insertedId = Convert.ToInt32(idString);
                scheduleModel.Id = insertedId;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            foreach (Quarter quarter in scheduleModel.Quarters)
            {
                foreach (Course course in quarter.Courses)
                {
                    try
                    {
                        DBPlugin.ExecuteToString(
                            $"insert into StudyPlan (GeneratedPlanID, QuarterID, YearID, CourseID, DateAdded, LastDateModified) " +
                            $"Values ({insertedId}, {quarter.QuarterKey}, {DateTime.UtcNow.Year + quarter.Year}, {course.Id}, '{DateTime.UtcNow}', '{DateTime.UtcNow}')");

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
                
            }
          
            //save the plan if needed
            return insertedId;
        }
    }
}
