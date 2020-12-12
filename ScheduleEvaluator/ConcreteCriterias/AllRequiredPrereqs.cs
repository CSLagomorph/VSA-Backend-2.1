using System;
using System.Collections.Generic;
using System.Text;


namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    public class AllPrereqs : Criteria
    {
        static Dictionary<string, List<int>> RequiredCourses = new Dictionary<string, List<int>>();
        public AllPrereqs(double weight) : base(weight)
        {
        }

        public override double getResult(ScheduleModel s)
        {
            var targetSchool = s.PreferenceSet.SchoolId;
            var targetMajor = s.PreferenceSet.MajorID;
            var key = $"{targetSchool}_{targetMajor}";
            if (!RequiredCourses.ContainsKey(key))
            {
                LoadRequiredCourses(key, targetSchool, targetMajor);
            }

            var requiredCourses = RequiredCourses[key];
            var coursesScheduled = new HashSet<string>();

            foreach (Quarter sQuarter in s.Quarters)
            {
                foreach (Course course in sQuarter.Courses)
                {
                    coursesScheduled.Add(course.Id);
                }
            }

            foreach (int requiredCourse in requiredCourses)
            {
                if (!coursesScheduled.Contains(requiredCourse.ToString())) return 0;
            }

            return 1 * this.weight;
        }

        private void LoadRequiredCourses(string key, int targetSchool, int targetMajor)
        {
            var coursesQuery = $"select * from AdmissionRequiredCourses where MajorID={targetMajor} and SchoolId={targetMajor}";
            var connection = new DBConnection();
            var sqlResults = connection.ExecuteToDT(coursesQuery);
            var reqCourses = new List<int>();
            foreach (System.Data.DataRow sqlResult in sqlResults.Rows)
            {
                reqCourses.Add((int)sqlResult["CourseId"]);
            }


            RequiredCourses.Add(key, reqCourses);
        }
    }
}