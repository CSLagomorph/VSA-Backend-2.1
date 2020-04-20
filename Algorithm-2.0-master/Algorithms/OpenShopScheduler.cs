namespace Scheduler.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Contracts;
    using Newtonsoft.Json;

    /// <summary>
    /// Implements the Open Shop Scheduling algorithm
    /// </summary>
    public class OpenShopScheduler : SchedulerBase, IScheduler
    {
        #region Constructor
        //------------------------------------------------------------------------------
        // 
        // Constructors
        // 
        //------------------------------------------------------------------------------

        public OpenShopScheduler(int paramID, bool preferShortest = true)
        {
            SetUp(paramID);
            MakeStartingPoint();
            InitDegreePlan();
            CreateSchedule(preferShortest);
        }
        #endregion
        
        #region Scheduling Algorithm
        //------------------------------------------------------------------------------
        // Order in which the scheduler processes the jobs is not fixed in advance
        // Of course, we have prerequisites so not everything can be scheduled at random
        // so what we do is define the leaves (stuff we can parallelize) and try to find the best 
        // path for those.
        //------------------------------------------------------------------------------
        public Schedule CreateSchedule(bool preferShortest)
        {
            List<Job> majorCourses = RequiredCourses.GetList(0);
            SortedDictionary<int, List<Job>> jobs = new SortedDictionary<int, List<Job>>();
            for (int i = 0; i < majorCourses.Count; i++)
            {
                Job job = majorCourses[i];
                AddPrerequisites(job, jobs, preferShortest, 0);
            }

            ScheduleCourses(jobs);
            Schedule = GetBusyMachines(); //SUGGEST BETTER NAMING CONVENTION?//
                                          //return proposed schedule
            return new Schedule()
            {
                Courses = this.Schedule,
                SchedulerName = nameof(OpenShopScheduler)
            };
        }

        private void ScheduleCourses(SortedDictionary<int, List<Job>> jobs)
        {

            foreach (var kvp in jobs)
            {
                foreach (var job in kvp.Value)
                {
                    ScheduleCourse(job);
                }
            }
        }


        #endregion

    }
}
