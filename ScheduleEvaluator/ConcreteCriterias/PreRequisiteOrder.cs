using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using System.Linq;
    using System.Net.Http;
    using Models;
    using Newtonsoft.Json;

    public class PreRequisiteOrder : Criteria
    {
        static Dictionary<string, List<CourseNode>> PrerequisiteCache = new Dictionary<string, List<CourseNode>>();
        public PreRequisiteOrder(double weight) : base(weight)
        {
        }

        public override double getResult(ScheduleModel s)
        {
            Dictionary<string, int> completedCourses = new Dictionary<string, int>();

            // Sort quarters from earliest to latest
            List<Quarter> quarters = s.Quarters;
            quarters.Sort();

            int invalidCourses = 0;
            int partiallyValid = 0;
            foreach (Quarter q in quarters)
            {
                // Iterate over courses twice to concurrent classes from being
                // seen as completed prerequisites
                foreach (Course c in q.Courses)
                {
                    var score = verifyPrereqs(c.Id, completedCourses, (int)q.Year + q.QuarterKey);
                    // First check if prereqs are met
                    if (Math.Abs(score) < 0.01)
                    {
                        invalidCourses++;
                    }
                    else if (Math.Abs(score - 0.5) < 0.01)
                    {
                        partiallyValid++;
                    }
                }

                // Then add to completed courses
                foreach (Course c in q.Courses)
                {
                    if (!completedCourses.ContainsKey(c.Id))
                    {
                        completedCourses.Add(c.Id, (int)q.Year + q.QuarterKey);
                    }

                }
            }

            if (invalidCourses > 0)
            {
                return (1 - 0.5 * invalidCourses) * this.weight;
            }
            else if (partiallyValid > 0)
            {
                return (1 - 0.1 * partiallyValid) * this.weight;
            }
            else
            {
                return 1 * this.weight;
            }
        }

        // Verifies completion of a course's prereqs
        private double verifyPrereqs(string courseId, Dictionary<string, int> complete, int currentQuarter)
        {
            List<CourseNode> prereqs = null;
            if (PrerequisiteCache.ContainsKey(courseId))
            {
                prereqs = PrerequisiteCache[courseId];
            }
            else
            {
                prereqs = getCourseNetwork(courseId).GetAwaiter().GetResult();
                PrerequisiteCache.Add(courseId, prereqs);
            }

            if (prereqs == null) throw new Exception("Could not get CourseNetwork");

            var numGroups = 0;
            var groupsFailed = 0;
            var fracFailedGroups = 0;
            // Verify that each course's prereqs have been completed
            foreach (CourseNode cn in prereqs)
            {
                if (cn.prereqs != null)
                {
                    foreach (CourseNode courseNode in cn.prereqs)
                    {
                        numGroups++;
                        if (!complete.ContainsKey(courseNode.PrerequisiteCourseID.ToString()))
                        {
                            groupsFailed++;
                        }
                        else if (complete[courseNode.PrerequisiteCourseID.ToString()] == currentQuarter)
                        {
                            fracFailedGroups++;
                        }
                    }
                }
            }
            if (numGroups == 0) return 1;
            if (groupsFailed >= numGroups) return 0;
            if (fracFailedGroups >= numGroups) return 0.5;
            return 1;
        }

        public async Task<List<CourseNode>> getCourseNetwork(string id)
        {
            HttpClient client = new HttpClient();
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
    }
}

