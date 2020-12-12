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

    /// <summary>
    /// Implements the Open Shop Scheduling algorithm with genetic algorithm
    /// </summary>
    public class OpenShopGAScheduler : SchedulerBase, IScheduler
    {
        private OpenShopGASchedulerSettings CurrentBestFit = null;
        private Models.Preferences Preferences = null;
        private int parameterId;
        #region Constructor
        //------------------------------------------------------------------------------
        // 
        // Constructors
        // 
        //------------------------------------------------------------------------------

        public OpenShopGAScheduler(int paramID, Models.Preferences preferences, bool preferShortest = true, OpenShopGASchedulerSettings currentBestFit = null)
        {
            this.CurrentBestFit = currentBestFit;
            this.Preferences = preferences;
            this.parameterId = paramID;
            SetUp(paramID);
            MakeStartingPoint();
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


            //Find the major courses with the longest prereq network. How to find it? Basically, create a new sortedList 
            //for each major course separately and process them one at a time.
            var prereqLists = new List<SortedDictionary<int, List<Job>>>();
            for (int i = 0; i < majorCourses.Count; i++)
            {
                var sortedPrereqs = new SortedDictionary<int, List<Job>>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
                Job job = majorCourses[i];
                AddPrerequisites(job, sortedPrereqs, preferShortest, -10);
                prereqLists.Add(sortedPrereqs);
            }
            //now, sort the prereqsList based on the longest path
            var prereqLongest = prereqLists.OrderByDescending(s => s.Count).ToList();
            var merged = new SortedDictionary<int, List<Job>>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
            foreach (SortedDictionary<int, List<Job>> sortedDictionary in prereqLongest)
            {
                merged = LongestPathScheduler.MergeDictionaries(merged, sortedDictionary);
            }

            return FindBestSchedule(merged, 20, 100, 45, this.CurrentBestFit);
        }

        public Schedule FindBestSchedule(SortedDictionary<int, List<Job>> jobs, int level = 20, int populationSize = 100, int topPercentToKeep = 80, OpenShopGASchedulerSettings currentBestFit = null)
        {
            //first, define a population
            var populationSet = new List<Schedule>();
            var rand = new Random();
            for (int i = 0; i < populationSize; i++)
            {
                SetUp(this.parameterId);
                var chromosome = ScheduleCourses(jobs, i > 0);
                var settings = new OpenShopGASchedulerSettings() { Chromosome = chromosome };
                Schedule = GetBusyMachines();
                var sched = new Schedule()
                {
                    Courses = CopyMachines(this.Schedule),
                    SchedulerName = nameof(OpenShopGAScheduler),
                    ScheduleSettings = settings,
                };
                var rating = GetRating(sched);
                sched.Rating = rating;
                populationSet.Add(sched);
            }

            var fittest = SelectFittest(populationSet, level, topPercentToKeep, currentBestFit);
            return fittest.OrderByDescending(s => s.Rating).First();
        }

        private List<Machine> CopyMachines(List<Machine> schedule)
        {
            var copied = new List<Machine>();
            foreach (Machine machine in schedule)
            {
                var copiedMachine = new Machine(machine.GetYear(), machine.GetQuarter(), machine.GetDateTime(),
                                                machine.jobs);
                copiedMachine.SetCurrentJobProcessing(machine.GetCurrentJobProcessing());
                copied.Add(copiedMachine);
            }

            return copied;
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
                SetUp(this.parameterId);

                ScheduleCourse(currentBestFit.Chromosome);
                Schedule = GetBusyMachines();

                topSchedule = new Schedule()
                {
                    Courses = CopyMachines(this.Schedule),
                    SchedulerName = nameof(OpenShopGAScheduler),
                    ScheduleSettings = currentBestFit,
                };
                var rating = GetRating(topSchedule);
                topSchedule.Rating = rating;
            }

            var cutoff = populationSet.Count * topPercentToKeep / 100;
            var safePop = populationSet;

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
                    var mutation = Mutate(crossOver, randomToMutate.ScheduleSettings, 1);
                    SetUp(this.parameterId);
                    ScheduleCourse(mutation.First().Chromosome);
                    Schedule = GetBusyMachines();

                    var offspring = new Schedule()
                    {
                        Courses = CopyMachines(this.Schedule),
                        SchedulerName = nameof(OpenShopGAScheduler),
                        ScheduleSettings = crossOver,
                    };
                    var rating = GetRating(offspring);
                    offspring.Rating = rating;
                    offSprings.Add(offspring);
                }

                populationSet.Remove(mate);

            }

            safePop.AddRange(offSprings);
            var nextGen = safePop.OrderByDescending(s => s.Rating).Take(cutoff).ToList();
            var initialSize = safePop.Count;

            //get some random performers too
            var numRandoms = (int) safePop.Count * 0.1;
            var randGen = new Random();
            for (int i = 0; i < numRandoms; i++)
            {
                var randIndx = randGen.Next(safePop.Count);
                nextGen.Add(safePop[randIndx]);
            }
            return SelectFittest(nextGen, level - 1, 25);
        }

        private List<OpenShopGASchedulerSettings> Mutate(OpenShopGASchedulerSettings crossOver, OpenShopGASchedulerSettings scheduleSettings, int i)
        {
            var combined = new List<Job>();
            combined.AddRange(crossOver.Chromosome);
            combined.AddRange(scheduleSettings.Chromosome);
           var unique = new List<Job>();
           foreach (Job job in combined)
           {
               if (!unique.Contains(job))
               {
                    unique.Add(job);
               }
           }
           return new List<OpenShopGASchedulerSettings>()
           {
               new OpenShopGASchedulerSettings()
               {
                   Chromosome = unique
               }
           };
        }

        private void ResetQuarters()
        {
            foreach (MachineNode machineNode in this.Quarters)
            {
                machineNode.ResetMachines();
            }
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
            var coinflip = new Random();
            int currentLevel = 0;
            if (!mutate)
            {

                foreach (var kvp in jobs)
                {
                    var increment = coinflip.Next(2);
                    if (increment == 0)
                    {
                        currentLevel++;
                    }
                    foreach (var job in kvp.Value)
                    {
                        if (!courseDna.Contains(job))
                        {
                            courseDna.Add(job);
                        }
                        currentLevel = ScheduleCourse(job, currentLevel);
                    }
                }
            }
            else
            {
                List<Job> jobsToSchedule = new List<Job>();
                foreach (KeyValuePair<int, List<Job>> job in jobs)
                {
                    foreach (Job job1 in job.Value)
                    {
                        jobsToSchedule.Add(job1);
                    }
                }

                jobsToSchedule = jobsToSchedule.Shuffle().ToList();
                courseDna = jobsToSchedule;
                ScheduleCourse(jobsToSchedule);
                //foreach (var kvp in jobs)
                //{
                //    var increment = coinflip.Next(2);
                //    if (increment == 0)
                //    {
                //        currentLevel++;
                //    }
                //    var shuffled = kvp.Value.Shuffle().ToList();
                //    foreach (var job in shuffled)
                //    {
                //        if (!courseDna.Contains(job))
                //        {
                //            courseDna.Add(job);
                //            currentLevel = ScheduleCourse(job, currentLevel);
                //        }
                //    }
                //}
            }

            return courseDna;
        }

        private void ScheduleCourse(List<Job> orderedJobs)
        {
            foreach (var job in orderedJobs)
            {
                ScheduleCourse(job, 0);
            }
        }

        #endregion

    }

    public class OpenShopGASchedulerSettings
    {
        public List<Job> Chromosome { get; set; }
    }
}
