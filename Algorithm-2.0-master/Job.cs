using Newtonsoft.Json;

namespace Scheduler {
    public class Job {
        #region NOTES
        /*
         * This appears to be how courses are represented. Each course has an ID,
         * a boolean for if the course was scheduled, a boolean for if the course has
         * all the prerequisites of at least one prerequisite group scheduled, which quarter
         * the prerequisite was scheduled, and which year it was scheduled. -Andrue
         * 
         * I took the liberty of reorganizing the code into regions of like code, all the getter methods in one location,
         * all the setter methods in another location, etc. -Andrue
         */
        #endregion

        #region Class Variables
        [JsonProperty]
        private int id;
        [JsonIgnore]
        private bool scheduled;
        [JsonIgnore]
        private bool prerequisitesScheduled;
        [JsonIgnore]
        private int quarterScheduled;
        [JsonIgnore]
        private int yearScheduled;
        [JsonIgnore]
        private int credits;
        [JsonIgnore]
        private bool coreCourse;
        #endregion

        #region Constructor
        //------------------------------------------------------------------------------
        // constructor
        //------------------------------------------------------------------------------
        public Job()
        {
            id = -0;
            scheduled = false;
            prerequisitesScheduled = false;
            quarterScheduled = 0;
            yearScheduled = 0;

            // These are placeholder values...
            credits = 0;
            coreCourse = false;
        }

        public Job(int id) {
            this.id = id;
            scheduled = false;
            quarterScheduled = -1;
            yearScheduled = -1;
            prerequisitesScheduled = false;

            // These are placeholder values...
            credits = 0;
            coreCourse = false;
        }

        public Job(int id, int credits, bool coreClass) {
            this.id = id;
            scheduled = false;
            quarterScheduled = -1;
            yearScheduled = -1;
            prerequisitesScheduled = false;
            this.credits = credits;
            coreCourse = coreClass;
        }
        #endregion

        #region Getters
        //------------------------------------------------------------------------------
        // boolean for scheduled or not
        //------------------------------------------------------------------------------
        public bool GetScheduled() {
            return scheduled;
        }

        //------------------------------------------------------------------------------
        // boolean for if the prerequisites are scheduled
        //------------------------------------------------------------------------------
        public bool GetPrerequisitesScheduled() {
            return prerequisitesScheduled;
        }

        //------------------------------------------------------------------------------
        // getter for wuarter scheduled
        //------------------------------------------------------------------------------
        public int GetQuarterScheduled()
        {
            return quarterScheduled;
        }

        //------------------------------------------------------------------------------
        // getter for year scheduled
        //------------------------------------------------------------------------------
        public int GetYearScheduled()
        {
            return yearScheduled;
        }

        //------------------------------------------------------------------------------
        // getter for id of course; corresponds to what it is in DB
        //------------------------------------------------------------------------------
        public int GetID()
        {
            return id;
        }

        //------------------------------------------------------------------------------
        // getter for if this course is a major course; corresponds to what it is in DB
        //------------------------------------------------------------------------------
        public bool GetCoreCourse() {
            return coreCourse;
        }

        //------------------------------------------------------------------------------
        // getter for Number of credits; corresponds to what it is in DB
        //------------------------------------------------------------------------------
        public int GetNumCredits() {
            return credits;
        }
            
        #endregion

        #region Setters
        //------------------------------------------------------------------------------
        // 
        // setter for prerequisites
        // 
        //------------------------------------------------------------------------------
        public void SetPrerequisitesScheduled(bool b) {
            prerequisitesScheduled = b;
        }

        //------------------------------------------------------------------------------
        // 
        // seter for being scheduled
        // 
        //------------------------------------------------------------------------------
        public void SetScheduled(bool s) {
            scheduled = s;
        }

        //------------------------------------------------------------------------------
        // 
        // setter for quarter scheduled
        // 
        //------------------------------------------------------------------------------
        public void SetQuarterScheduled(int x) {
            quarterScheduled = x;
        }
        //------------------------------------------------------------------------------
        // 
        // setter for year scheduled
        // 
        //------------------------------------------------------------------------------
        public void SetYearScheduled(int x)
        {
            yearScheduled = x;
        }

        //------------------------------------------------------------------------------
        // setter for if this course is a major course; corresponds to what it is in DB
        //------------------------------------------------------------------------------
        public void SetCoreCourse(bool val)
        {
            coreCourse = val;
        }

        //------------------------------------------------------------------------------
        // setter for Number of credits; corresponds to what it is in DB
        //------------------------------------------------------------------------------
        public void SetNumCredits(int val)
        {
            credits = val;
        }
        #endregion

        #region Comparison Operators
        //------------------------------------------------------------------------------
        // equality
        //------------------------------------------------------------------------------
        public static bool operator ==(Job thisj, Job right) {
            if(object.ReferenceEquals(thisj, null) || object.ReferenceEquals(right, null)) {
                return false;
            }
            if(object.ReferenceEquals(thisj, right)) {
                return true;
            }
            return thisj.Equals(right);
        }

        public static bool operator !=(Job thisj, Job right) {
            return !(thisj == right);
        }

        public override bool Equals(object obj) {
            Job j = obj as Job;
            return this.id == j.id;
        }
        #endregion
    }
}
