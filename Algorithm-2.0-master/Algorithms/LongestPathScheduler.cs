namespace Scheduler.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Contracts;
    using Newtonsoft.Json;

    /// <summary>
    /// Taking inspiration from the shortest path scheduler in OS, this implements a longest path
    /// scheduler. Why? In our cases, our biggest issues are the courses with the longest paths
    /// (too many pre-reqs) so we need to get those prioritized and out of the way fast
    /// </summary>
    public class LongestPathScheduler : SchedulerBase, IScheduler
    {
        #region Constructor
        //------------------------------------------------------------------------------
        // 
        // Constructors
        // 
        //------------------------------------------------------------------------------

        public LongestPathScheduler(int paramID, bool preferShortest = true)
        {
            SetUp(paramID);
            MakeStartingPoint();
            InitDegreePlan();
            CreateSchedule(preferShortest);
        }
        #endregion

        //------------------------------------------------------------------------------
        // Order in which the scheduler processes the jobs is not fixed in advance
        // Of course, we have prerequisites so not everything can be scheduled at random
        // so what we do is define the leaves (stuff we can parallelize) and try to find the best 
        // path for those.
        //------------------------------------------------------------------------------
        public Schedule CreateSchedule(bool preferShortest)
        {
            List<Job> majorCourses = RequiredCourses.GetList(0);


            //Find the major courses with the longest prereq network. How to find it? Basically, create a new sortedList 
            //for each major course separately and process them one at a time.
            var prereqLists = new List<SortedDictionary<int, List<Job>>>();
            var addedJobs = new List<int>();
            for (int i = 0; i < majorCourses.Count; i++)
            {
                var sortedPrereqs = new SortedDictionary<int, List<Job>>();
                Job job = majorCourses[i];
                AddPrerequisites(job, sortedPrereqs, preferShortest, 0);
                prereqLists.Add(sortedPrereqs);
            }
            //now, sort the prereqsList based on the longest path
            var prereqLongest = prereqLists.OrderByDescending(s => s.Count).ToList();
            var merged = new SortedDictionary<int, List<Job>>();
            foreach (SortedDictionary<int, List<Job>> sortedDictionary in prereqLongest)
            {
                merged = MergeDictionaries(merged, sortedDictionary);
            }

            ScheduleCourses(merged);


            Schedule = GetBusyMachines();
            return new Schedule()
            {
                Courses = this.Schedule,
                SchedulerName = nameof(LongestPathScheduler)
            };
        }

        public static SortedDictionary<int, List<Job>> MergeDictionaries(SortedDictionary<int, List<Job>> merged, SortedDictionary<int, List<Job>> sortedDictionary)
        {
            var targt = merged;
            foreach (KeyValuePair<int, List<Job>> keyValuePair in sortedDictionary)
            {
                if (merged.ContainsKey(keyValuePair.Key))
                {
                    foreach (var job in keyValuePair.Value)
                    {
                        if (!merged[keyValuePair.Key].Contains(job))
                        {
                            merged[keyValuePair.Key].Add(job);
                        }

                    }
                }
                else
                {
                    merged.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            return merged;

        }

        private void ScheduleCourses(SortedDictionary<int, List<Job>> jobs)
        {
            int currentLevel = 0;
            foreach (var kvp in jobs)
            {
                currentLevel++;
                foreach (var job in kvp.Value)
                {
                    var quarter = ScheduleCourse(job, currentLevel);
                    if (quarter > currentLevel)
                    {
                        currentLevel = quarter;
                    }
                }
            }
        }
    }
}
