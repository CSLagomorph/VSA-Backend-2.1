﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleEvaluator.ConcreteCriterias
{
    using Models;

    public class AllPrereqs : Criteria
    {
        private Evaluator e;

        public AllPrereqs(double weight) : base(weight) {
            e = new Evaluator();
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
            Task.Run(async() =>
            {
                prereqs = await e.getCourseNetwork(courseId);
            }).GetAwaiter().GetResult();

            if (prereqs == null) throw new Exception("Could not get CourseNetwork");

            // Verify that each course's prereqs have been completed
            foreach (CourseNode cn in prereqs)
            {
                if (!complete.Contains(cn.courseID)) return false;
            }
            return true;
        }
    }
}
