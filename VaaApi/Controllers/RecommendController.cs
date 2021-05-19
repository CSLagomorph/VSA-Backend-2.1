using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Scheduler;

namespace VaaApi.Controllers
{
    using System.Data;
    using System.Web.Http.Cors;
    using System.Web.Http.Results;
    using Models;
    using Newtonsoft.Json;
    using Scheduler.Contracts;
    public class RecommendController : ApiController
    {
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Get(int id)
        {
            var connection = new DBConnection();
            var parameterQuery = $"select ParameterSetId from GeneratedPlan where GeneratedPlanId={id}";
            var parameterId = (int)connection.ExecuteToDT(parameterQuery).Rows[0]["ParameterSetId"];

            var parameterSetQuery = $"select ParameterSet.MajorID, SchoolID, TimePeriod, MaxNumberOfQuarters, NumberCoreCoursesPerQuarter, CreditsPerQuarter, SummerPreference, DepartmentId from ParameterSet join Major on ParameterSet.MajorID = Major.MajorID" +
                                    $" join TimePreference on TimePreference.TimePreferenceID = ParameterSet.TimePreferenceID" +
                                    $" where ParameterSetId = {parameterId}";
            var parameterSetResult = connection.ExecuteToDT(parameterSetQuery);

            var parameters = Preferences.ConvertFromDatabase(parameterSetResult, parameterId);

            var similarPlansQuery = $"select gp.GeneratedPlanID from GeneratedPlan as gp join ParameterSet as ps on gp.ParameterSetID=ps.ParameterSetID where ps.MajorID={parameters.MajorID} and ps.SchoolID= {parameters.SchoolId}";
            var similarPlansResults = connection.ExecuteToDT(similarPlansQuery);
            Random r = new Random();
            int rand_index0 = r.Next(0, similarPlansResults.Rows.Count);
            int rand_index1 = r.Next(0, similarPlansResults.Rows.Count);

            var query = "select CourseNumber, QuarterID, YearID, Course.CourseId, DepartmentId from StudyPlan" +
                        " join course on Course.CourseID = StudyPlan.CourseID" +
                        $" where GeneratedPlanID = {similarPlansResults.Rows[rand_index0]["GeneratedPlanID"]}";


            var results = connection.ExecuteToDT(query);
            var model = ScheduleModel.ConvertFromDatabase(results, (int)similarPlansResults.Rows[rand_index0]["GeneratedPlanID"], parameters);
            var response = JsonConvert.SerializeObject(model);
            //comtinutaion for the second recommendation
            var query1 = "select CourseNumber, QuarterID, YearID, Course.CourseId, DepartmentId from StudyPlan" +
                        " join course on Course.CourseID = StudyPlan.CourseID" +
                        $" where GeneratedPlanID = {similarPlansResults.Rows[rand_index1]["GeneratedPlanID"]}";
            var results1 = connection.ExecuteToDT(query1);
            var model1 = ScheduleModel.ConvertFromDatabase(results1, (int)similarPlansResults.Rows[rand_index1]["GeneratedPlanID"], parameters);

            response += "\n";
            response += JsonConvert.SerializeObject(model1);

            return response;
        }
    }
}
