using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using System.Net.Http;
    using Models;
    using Newtonsoft.Json;

    public class PreRequisiteOrder : Criteria
    {
        HttpClient client;
        public PreRequisiteOrder(double weight) : base(weight)
        {
            client = new HttpClient();
        }

        public override double getResult(ScheduleModel s)
        {
            HashSet<string> completedCourses = new HashSet<string>();

            // Sort quarters from earliest to latest
            List<Quarter> quarters = s.Quarters;
            quarters.Sort();

            int invalidCourses = 0;

            foreach (Quarter q in quarters)
            {
                // Iterate over courses twice to concurrent classes from being
                // seen as completed prerequisites
                foreach (Course c in q.Courses)
                {
                    // First check if prereqs are met
                    if (!verifyPrereqs(c.Id, completedCourses)) invalidCourses++;
                }

                // Then add to completed courses
                foreach (Course c in q.Courses) completedCourses.Add(c.Id);
            }

            return (invalidCourses > 0 ? 0 : 1) * weight;
        }

        // Verifies completion of a course's prereqs
        private bool verifyPrereqs(string courseId, HashSet<string> complete)
        {
            List<CourseNode> prereqs = null;
            Task.Run(async () =>
            {
                prereqs = await getCourseNetwork(courseId);
            }).GetAwaiter().GetResult();

            if (prereqs == null) throw new Exception("Could not get CourseNetwork");

            // Verify that each course's prereqs have been completed
            foreach (CourseNode cn in prereqs)
            {
                if (cn.courseID != courseId)
                {
                    if (!complete.Contains(cn.courseID))
                    {
                        return false;
                    }
                }
                
            }
            return true;
        }

        public async Task<List<CourseNode>> getCourseNetwork(string id)
        {
            HttpResponseMessage resp;
            try
            {
                resp = await client.GetAsync(
                   $"http://vaacoursenetwork.azurewebsites.net/v1/CourseNetwork?course={id}"
                   );
                var responseStr = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<CourseNode>>(responseStr);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught During HTTP Request");
                Console.WriteLine("Message: {0}", e.Message);
            }
            return null;
         }

        public async Task<Dictionary<int, List<CourseNode>>> getCourseNetworks(List<int> courses) {
            HttpResponseMessage resp;
            string jsonInput = JsonConvert.SerializeObject(courses);
            try
            {
                resp = await client.GetAsync(
                   $"http://vaacoursenetwork.azurewebsites.net/v1/GetMultipleCourses?input={jsonInput}"
                   );
                return JsonConvert.DeserializeObject<Dictionary<int, List<CourseNode>>>
                    (await resp.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught During HTTP Request");
                Console.WriteLine("Message: {0}", e.Message);
            }
            return null;
        }
    }
}

