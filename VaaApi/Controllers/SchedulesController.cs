using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using Scheduler;

namespace VaaApi.Controllers
{
    using System.Data;
    using System.Web.Http.Cors;
    using System.Web.Http.Results;
    using Models;
    using Newtonsoft.Json;
    using Scheduler.Contracts;

    public class SchedulesController : ApiController
    {
        // GET api/values
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Get(int id)
        {
            var connection = new DBConnection();
            var parameterQuery = $"select ParameterSetId from GeneratedPlan where GeneratedPlanId={id}";
            var parameterId = (int)connection.ExecuteToDT(parameterQuery).Rows[0]["ParameterSetId"];

            var parameterSetQuery= $"select ParameterSet.MajorID, SchoolID, MaxNumberOfQuarters, NumberCoreCoursesPerQuarter, CreditsPerQuarter, SummerPreference, DepartmentId from ParameterSet join Major on ParameterSet.MajorID = Major.MajorID"+
            $" where ParameterSetId = {parameterId}";
            var parameterSetResult = connection.ExecuteToDT(parameterSetQuery);

            var parameters = Preferences.ConvertFromDatabase(parameterSetResult, parameterId);
            var query = "select CourseNumber, QuarterID, YearID, Course.CourseId from StudyPlan" +
                        " join course on Course.CourseID = StudyPlan.CourseID" +
                        $" where GeneratedPlanID = {id}";

            
            var results = connection.ExecuteToDT(query);
            var model = ScheduleModel.ConvertFromDatabase(results, id, parameters);

            var response = JsonConvert.SerializeObject(model);
            return response;
        }

    }
}
