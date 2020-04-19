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

    public class SchedulesController : ApiController
    {
        // GET api/values
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Get(int id)
        {
            var model = new ScheduleModel
            {
                Quarters = new List<Quarter>(),
                Id = id
            };
            var query = "select CourseNumber, QuarterID, YearID, Course.CourseId from StudyPlan" +
                        " join course on Course.CourseID = StudyPlan.CourseID" +
                        $" where GeneratedPlanID = {id}";

            var connection = new DBConnection();
            var results = connection.ExecuteToDT(query);
            foreach (DataRow row in results.Rows)
            {
                var courseName = (string) row["CourseNumber"];
                var quarter = (int) row["QuarterID"];
                var year = (int) row["YearID"];
                var courseId = (int) row["CourseId"];
                var quarterItem=model.Quarters.FirstOrDefault(s => s.Id == $"{year}{quarter}" && s.Year == year);
                if (quarterItem == null)
                {
                    model.Quarters.Add(new Quarter(){Id = $"{year}{quarter}", Title = $"{year}-{quarter}", Year = year});
                    quarterItem = model.Quarters.First(s => s.Id == $"{year}{quarter}" && s.Year == year);
                }

                if (quarterItem.Courses == null)
                {
                    quarterItem.Courses = new List<Course>();
                }
                quarterItem.Courses.Add(new Course(){Description = courseName+$"({courseId})", Id = courseName, Title = courseName + $"({courseId})" });
            }

            var response = JsonConvert.SerializeObject(model);
            return response;
        }

    }
}
