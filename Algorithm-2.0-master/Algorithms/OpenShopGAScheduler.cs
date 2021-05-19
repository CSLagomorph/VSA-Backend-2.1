namespace Scheduler.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Contracts;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using ScheduleEvaluator;
    using System.Diagnostics;
    using Models;

    /// <summary>
    /// Implements the Open Shop Scheduling algorithm with genetic algorithm
    /// </summary>
    public class OpenShopGAScheduler : SchedulerBase, IScheduler
    {
        private OpenShopGASchedulerSettings CurrentBestFit = null;
        private Models.Preferences Preferences = null;
        #region Constructor
        //------------------------------------------------------------------------------
        // 
        // Constructors
        // 
        //------------------------------------------------------------------------------

        public OpenShopGAScheduler(int paramID, Models.Preferences preferences, bool preferShortest = true, OpenShopGASchedulerSettings currentBestFit = null, bool associatesSchedule=false)
        {
            this.CurrentBestFit = currentBestFit;
            this.Preferences = preferences;
            SetUp(paramID);
            MakeStartingPoint();
            if(associatesSchedule==true)
                InitDegreePlan(associatesSchedule);
            else
                InitDegreePlan();
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

            return FindBestSchedule(jobs, 4, 100, 25, this.CurrentBestFit);
        }

        public Schedule FindBestSchedule(SortedDictionary<int, List<Job>> jobs, int level = 20, int populationSize = 100, int topPercentToKeep = 80, OpenShopGASchedulerSettings currentBestFit = null)
        {
            //first, define a population
            var populationSet = new List<Schedule>();
            var rand = new Random();
            for (int i = 0; i < populationSize; i++)
            {
                var chromosome = ScheduleCourses(jobs, true);
                var settings = new OpenShopGASchedulerSettings() { Chromosome = chromosome };
                var sched = new Schedule()
                {
                    Courses = this.Schedule,
                    SchedulerName = nameof(OpenShopGAScheduler),
                    ScheduleSettings = settings,
                };
                var rating = GetRating(sched);
                sched.Rating = rating;
                populationSet.Add(sched);
            }

            var fittest = SelectFittest(populationSet, level, topPercentToKeep, currentBestFit);
            //printing the ratings of all schedules in the final population set
            var countSchedule = 0;
            Debug.WriteLine("Best schedule rating:" + fittest.OrderByDescending(s => s.Rating).First().Rating);
            //foreach( var s in fittest)
            //{
            //    Debug.WriteLine(countSchedule + "th schedule rating: " + s.Rating);
            //    countSchedule++;
            //}
            //comment this function if not used for associate degree schedules
            //saveAssociateDegreeSchedules(fittest);
            return fittest.OrderByDescending(s => s.Rating).First();
        }

        private void saveAssociateDegreeSchedules(List<Schedule> schedules)
        {
            foreach (var schedule in schedules)
            {

                var scheduleModel = schedule.ConvertToScheduleModel();
                int insertedId = 0;
                double rating = schedule.Rating;
                var prefid = DBPlugin.ExecuteToString("SELECT IDENT_CURRENT('ParameterSet')");
                var preferenceId = Convert.ToInt32(prefid);
                try
                {
                    var schedulerSettings = JsonConvert.SerializeObject(schedule.ScheduleSettings);
                    DBPlugin.ExecuteToString(
                        $"insert into GeneratedPlan (Name, ParameterSetID, DateAdded, LastDateModified, Status, SchedulerName, SchedulerSettings, WeakLabelScore) " +
                        $"Values ('latest', {preferenceId}, '{DateTime.UtcNow}', '{DateTime.UtcNow}', {1}, '{schedule.SchedulerName}', '{schedulerSettings}', {rating})");
                    var idString = DBPlugin.ExecuteToString("SELECT IDENT_CURRENT('GeneratedPlan')");
                    insertedId = Convert.ToInt32(idString);
                    scheduleModel.Id = insertedId;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                foreach (Quarter quarter in scheduleModel.Quarters)
                {
                    foreach (Course course in quarter.Courses)
                    {
                        try
                        {
                            DBPlugin.ExecuteToString(
                                $"insert into StudyPlan (GeneratedPlanID, QuarterID, YearID, CourseID, DateAdded, LastDateModified) " +
                                $"Values ({insertedId}, {quarter.QuarterKey}, {DateTime.UtcNow.Year + quarter.Year}, {course.Id}, '{DateTime.UtcNow}', '{DateTime.UtcNow}')");

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }

                }

            }
               

        }


        private double GetRating(Schedule sched)
        {
            var scheduleModel = sched.ConvertToScheduleModel();
            scheduleModel.PreferenceSet = this.Preferences;
            var eval = new Evaluator();
            return eval.evalaute(scheduleModel);
        }

        public List<Schedule> SelectFittest(List<Schedule> populationSet, int level = 20, int topPercentToKeep = 95, OpenShopGASchedulerSettings currentBestFit = null)
        {
            if (level == 0 || populationSet.Count <= 2)
            {
                return populationSet;
            }
            var rand = new Random();
            Schedule topSchedule = null;
            //select best from population
            if (currentBestFit == null)
            {
                topSchedule = populationSet.OrderByDescending(s => s.Rating).First();
            }
            else
            {
                ScheduleCourse(currentBestFit.Chromosome);
                topSchedule = new Schedule()
                {
                    Courses = this.Schedule,
                    SchedulerName = nameof(OpenShopGAScheduler),
                    ScheduleSettings = currentBestFit,
                };
                var rating = GetRating(topSchedule);
                topSchedule.Rating = rating;
            }

            var cutoff = populationSet.Count * topPercentToKeep / 100;
            populationSet = populationSet.OrderByDescending(s => s.Rating).Take(cutoff).ToList();
            if (populationSet.Contains(topSchedule))
            {
                populationSet.Remove(topSchedule);
            }
            List<Schedule> offSprings = new List<Schedule>();
            while (populationSet.Count > 0)
            {
                var mate = populationSet.First();
                //cross over
                var crossOvers = GetCrossOvers(topSchedule.ScheduleSettings, mate.ScheduleSettings, 2);
                foreach (var crossOver in crossOvers)
                {
                    //mutate (swap with some other schedule in the population)
                    var randomPop = rand.Next(populationSet.Count - 1);
                    var randomToMutate = populationSet[randomPop];
                    var mutation = GetCrossOvers(crossOver, randomToMutate.ScheduleSettings, 1);

                    ScheduleCourse(mutation.First().Chromosome);
                    var offspring = new Schedule()
                    {
                        Courses = this.Schedule,
                        SchedulerName = nameof(OpenShopGAScheduler),
                        ScheduleSettings = crossOver,
                    };
                    var rating = GetRating(offspring);
                    offspring.Rating = rating;
                    offSprings.Add(offspring);
                }

                populationSet.Remove(mate);

            }

            return SelectFittest(offSprings, level - 1);
        }

        private List<OpenShopGASchedulerSettings> GetCrossOvers(OpenShopGASchedulerSettings parent1, OpenShopGASchedulerSettings parent2, int count)
        {
            var random = new Random();
            var lowest = parent1.Chromosome.Count < parent2.Chromosome.Count ? parent1.Chromosome.Count : parent2.Chromosome.Count;
            if (parent1.Chromosome.Count <= 0) return new List<OpenShopGASchedulerSettings>() { parent1 };
            var crossOvers = new List<OpenShopGASchedulerSettings>();
            for (int i = 0; i < count; i++)
            {
                var crossOver = new OpenShopGASchedulerSettings()
                {
                    Chromosome = new List<Job>()
                };
                foreach (var job in parent1.Chromosome)
                {
                    crossOver.Chromosome.Add(job);
                }
                var randIndex = random.Next(lowest - 1);

                //swap at this index
                var old = crossOver.Chromosome[randIndex];
                var newVal = crossOver.Chromosome[randIndex];

                crossOver.Chromosome[randIndex] = newVal;
                int jobToReplace = -1;
                for (int j = 0; i < parent1.Chromosome.Count; i++)
                {
                    if (crossOver.Chromosome[i] == newVal)
                    {
                        jobToReplace = i;
                    }
                }

                if (jobToReplace != -1)
                {
                    crossOver.Chromosome[jobToReplace] = old;
                }
                crossOvers.Add(crossOver);
            }


            return crossOvers;
        }

        private List<Job> ScheduleCourses(SortedDictionary<int, List<Job>> jobs, bool mutate)
        {
            var courseDna = new List<Job>();
            //We shouldn't shuffle the actual order of the prereqs, only the courses at the same level
            foreach (var kvp in jobs)
            {
                IEnumerable<Job> orderedJobs = kvp.Value;


                foreach (var job in orderedJobs)
                {
                    if (!courseDna.Contains(job))
                    {
                        courseDna.Add(job);
                    }
                    ScheduleCourse(job);
                }
            }
            if (mutate)
            {
                //shuffle the order of courses for each level
                courseDna = courseDna.Shuffle().ToList();
            }
            return courseDna;
        }

        private void ScheduleCourse(List<Job> orderedJobs)
        {
            foreach (var job in orderedJobs)
            {
                ScheduleCourse(job);
            }
        }

        #endregion

    }

    public class OpenShopGASchedulerSettings
    {
        public List<Job> Chromosome { get; set; }
    }
}
