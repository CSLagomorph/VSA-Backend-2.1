using System.Collections.Generic;
using Newtonsoft.Json;

namespace Scheduler {
    class MachineNode {
        #region NOTES
        /*
         * From what I gather, each MachineNode represents a Quarter of scheduled "Machines" or,
         * in other words, courses. -Andrue
         * 
         * Added further documentation based on observation, took the liberty of moving the code around to 
         * regions defined by similar characteristics. - Andrue
         */
        #endregion

        #region Variables
        private int creditsScheduled; //Should keep track of all credits
        private int majorCreditsScheduled; //Should keep track of required courses
        private List<Machine> machines; //List of courses
        [JsonProperty]
        private int year; //The year this "MachineNode" is acting on
        [JsonProperty]
        private int quarter; //THe quarter the "MachineNode" is acting on
        private int classesScheduled; //Total classes scheduled
        #endregion

        #region Constructors
        //------------------------------------------------------------------------------
        // 
        // creates machine node from year and quarter
        // 
        //------------------------------------------------------------------------------
        public MachineNode(int year, int quarter) {
            this.year = year;
            this.quarter = quarter;
            machines = new List<Machine>();
            creditsScheduled = 0;
            majorCreditsScheduled = 0;
            classesScheduled = 0;
        }
        #endregion

        #region Getters
        //------------------------------------------------------------------------------
        // 
        // returns year
        // 
        //------------------------------------------------------------------------------
        public int GetYear() { return year; }

        //------------------------------------------------------------------------------
        // 
        // returns wuarter
        // 
        //------------------------------------------------------------------------------
        public int GetQuarter() { return quarter; }

        //------------------------------------------------------------------------------
        // 
        // returns ALL machined 
        // 
        //------------------------------------------------------------------------------
        public List<Machine> GetMachines() { return machines; }

        //------------------------------------------------------------------------------
        // 
        // returns credits currently scheduled on machine node. not implemented because
        // we dont have credits in machine. we only check for classes scheduled
        //------------------------------------------------------------------------------
        public int GetCreditsScheduled() {
            return creditsScheduled;
        }

        //------------------------------------------------------------------------------
        // 
        // temporary until getcreditsscheduled is implemented
        // 
        //------------------------------------------------------------------------------
        public int GetClassesScheduled() {
            return classesScheduled;
        }

        //------------------------------------------------------------------------------
        // 
        // will work with getcreditsscheduled for preferences
        // 
        //------------------------------------------------------------------------------
        public int GetMajorCreditsScheduled()
        {
            return majorCreditsScheduled;
        }
        //------------------------------------------------------------------------------
        // 
        // returns all machines for final plan used by scheduler
        // 
        //------------------------------------------------------------------------------
        public List<Machine> GetAllScheduledMachines()
        {
            List<Machine> scheduledMachines = new List<Machine>();
            for (int i = 0; i < machines.Count; i++)
            {
                Machine m = machines[i];
                if (m.CheckInUse()) scheduledMachines.Add(m);
            }
            return scheduledMachines;
        }
        #endregion

        #region Setters/Adjusters
            //------------------------------------------------------------------------------
            // 
            // temporary until getcreditsscheduled is implemented
            // 
            //------------------------------------------------------------------------------
        public void AddClassesScheduled(int k) {
            classesScheduled += k;
        }

        // Adds a job to this machine node. Updates that number of credits scheudled on the node
        // the number of course on the node and the number of core credits scheduled on this node.
        public void AddClassesScheduled(Job j) { 
            classesScheduled += 1;
            creditsScheduled += j.GetNumCredits();
            majorCreditsScheduled += j.GetCoreCourse() ? j.GetNumCredits() : 0;
        }

        //------------------------------------------------------------------------------
        // 
        // adds a new machine to node
        // 
        //------------------------------------------------------------------------------
        public void AddMachine(int year, int quarter, List<DayTime> dateTime, List<Job> jobs)
        {
            Machine m = new Machine(year, quarter, dateTime, jobs);
            machines.Add(m);
        }

        //------------------------------------------------------------------------------
        // 
        // adds a new machine to node
        // 
        //------------------------------------------------------------------------------
        public void AddMachine(Machine m)
        {
            machines.Add(m);
        }

        //------------------------------------------------------------------------------
        // 
        // removes machine from node
        // 
        //------------------------------------------------------------------------------
        public void RemoveMachine(Machine x)
        {
            if (machines.Contains(x)) machines.Remove(x);
        }
        #endregion
    }
}
