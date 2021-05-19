using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Scheduler;
using Models;

namespace VaaApi.Controllers
{
    using System.Web.Http.Cors;
    using System.Data;
    using System.Web.Http.Results;
    using ApiCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    public class CoursesController : ApiController
    {
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Get(int id)
        {
            //The obtained id is the school ID
            // The model contains courses objects to store their attributes
            var model = new CourseList
            {
                Courses = new List<Course>()
            };
            var query = "select Course.CourseNumber, Course.Title, Course.CourseID from Course" +
            " join AdmissionRequiredCourses on AdmissionRequiredCourses.CourseID = Course.CourseID" +
            $" where SchoolID = {id}";
            var connection = new DBConnection();
            //The query after executing returns a datatable
            var results = connection.ExecuteToDT(query);
            //Iterating through every row which is a course data object
            foreach (DataRow row in results.Rows)
            {
                var courseID = ((int)row["CourseID"]).ToString();
                var title = row["Title"].ToString();
                var description = row["CourseNumber"].ToString();
                model.Courses.Add(new Course { Description = description, Id = courseID, Title = title });
            }
            //The model holds the attributes of the course dataobjects retrieved and stored in it
            var response = JsonConvert.SerializeObject(model);
            return response;
        }


        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Get()
        {
            //This method returns all the courses in the database
            // The model contains courses objects to store their attributes
            var model = new CourseList
            {
                Courses = new List<Course>()
            };
            var query = "select Course.CourseNumber, Course.Title, Course.CourseID from Course" ;
            var connection = new DBConnection();
            //The query after executing returns a datatable
            var results = connection.ExecuteToDT(query);
            //Iterating through every row which is a course data object
            foreach (DataRow row in results.Rows)
            {
                var courseID = ((int)row["CourseID"]).ToString();
                var title = row["Title"].ToString();
                var description = row["CourseNumber"].ToString();
                model.Courses.Add(new Course { Description = description, Id = courseID, Title = title });
            }
            //The model holds the attributes of the course dataobjects retrieved and stored in it
            var response = JsonConvert.SerializeObject(model);
            return response;
        }
    }
}
