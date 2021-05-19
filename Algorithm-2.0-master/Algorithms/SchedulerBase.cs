using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Algorithms
{
    using System.Data;
    using Newtonsoft.Json;
    using System.Diagnostics;

    public class SchedulerBase
    {
        #region Class Variables
        [JsonIgnore]
        protected DBConnection DBPlugin;  //Declare the SQL connection to Database

        [JsonIgnore] protected DegreePlan RequiredCourses;
        [JsonIgnore]
        protected CourseNetwork PrerequisiteNetwork;   //A network of Courses and their respective prerequisite chains
        [JsonIgnore]
        protected List<MachineNode> Quarters; //Each node is one Quarter
        [JsonProperty]
        protected List<Machine> Schedule; //output schedule
        [JsonIgnore]
        protected Preferences StudentPreferences; //obtainable from the database or by manual entry
        [JsonIgnore]
        protected List<Job> completedPrior;
        [JsonProperty]
        protected List<Job> unableToSchedule;//list of courses that didn't fit into the schedule

        [JsonIgnore] protected int MaxYearLength = 7;
        [JsonIgnore] protected int MaxQuarters = 28;
        [JsonIgnore]
        protected int quarters = 0; //Preference
        [JsonIgnore]
        protected bool attendSummer = false; //Preference here for feedback if needed
        [JsonIgnore]
        protected int yearlength = 0; //either 3 quarters or 4 quarters depending on Summer preference
        [JsonIgnore]
        protected int years; //derived from dividing the total amount of quarters by the yearlength
        #endregion

        #region Setup
        //------------------------------------------------------------------------------
        // Does the setup of the variables and runs functions common to the constructors
        //
        // Queries the database for preferences and ignores quartersDeclared
        // and summerIntent. MakeStartingPoint and InitDegreePlan can be automated through
        // input from the database query
        //------------------------------------------------------------------------------
        protected void SetUp(int paramId)
        {
            DBPlugin = new DBConnection();
            Quarters = new List<MachineNode>(); //Quarters
            Schedule = new List<Machine>(); //Courses
            completedPrior = new List<Job>();
            unableToSchedule = new List<Job>();

            StudentPreferences = new Preferences(paramId);
            DeterminePlanLength(StudentPreferences.getQuarters(), StudentPreferences.getSummer());
            InitializeMachineNodes();
            InitMachines();
            normalizeMachines(); //Disposable after live data
            InitNetwork();
        }

        protected void DeterminePlanLength(int quartersDeclared, bool summerIntent)
        {
            attendSummer = summerIntent;
            this.quarters = quartersDeclared;
            years = quarters / 4;
            yearlength = MaxYearLength;

        }

        //------------------------------------------------------------------------------ 
        // initiates prerequisite network
        //------------------------------------------------------------------------------
        protected void InitNetwork()
        {
            string rawpreqs = DBPlugin.ExecuteToString("SELECT p.CourseID, MaxCredit as credits, GroupID, PrerequisiteID, PrerequisiteCourseID " +
                 "FROM Prerequisite as p " +
                 "LEFT JOIN Course as c ON c.CourseID = p.CourseID " +
                 "FOR JSON PATH");
            string rawcourses = DBPlugin.ExecuteToString("SELECT CourseID, MaxCredit as credits " +
                "FROM Course " +
                "FOR JSON PATH");
            //NETWORK BUILD
            PrerequisiteNetwork = new CourseNetwork(rawcourses, rawpreqs);
            PrerequisiteNetwork.BuildNetwork();
        }


        //------------------------------------------------------------------------------
        // creates a list of jobs that are ignored by the algorithm.
        // protected METHOD: starting point is determined by database query (table: ParameterSet)
        // PUBLIC METHOD: Starting Point determined by strings passed to function
        //------------------------------------------------------------------------------
        protected void MakeStartingPoint()
        {

            completedPrior = StudentPreferences.getPriors();
        }



        //------------------------------------------------------------------------------
        // Creates machineNodes which are representative of the quarters given through 
        // preferences. For example: if 8 quarters are declared, 8 machineNodes are 
        // created. 
        //------------------------------------------------------------------------------
        protected void InitializeMachineNodes()
        {
            for (int i = 0; i < MaxYearLength; i++)
            {
                for (int j = 0; j < MaxQuarters; j++)
                {
                    MachineNode m = new MachineNode(i, j);
                    Quarters.Add(m);
                }
            }

        }

        //------------------------------------------------------------------------------
        // WARNING!!! - This function can overwrite information. PLEASE READ
        //
        // This makes the list of classes available over successive years. Current data
        // being used is for the duration of one year, therefore this function sets up
        // the machineNodes in such a way they work off the assumption that classes
        // will be held at the same time, on the same days, year after year.
        //------------------------------------------------------------------------------
        void normalizeMachines()
        {   //transfer all the same classes to the set of machine nodes
            if (quarters >= yearlength)
            {
                for (int i = yearlength; i < quarters; i++)
                {
                    MachineNode oldMn = Quarters[i % yearlength];
                    MachineNode newMn = Quarters[i];
                    for (int j = 0; j < oldMn.GetMachines().Count; j++)
                    {
                        Machine oldMachine = oldMn.GetMachines()[j];
                        Machine newMachine = new Machine(oldMachine);
                        newMachine.SetYear(i / yearlength);
                        newMn.AddMachine(newMachine);
                    }
                }
                return;
            }
            else
            {
                return;
            }

        }

        protected void InitDegreePlan(bool associatesSchedule=false)
        {
            string query = "SELECT arc.CourseID as CID, c.MaxCredit, c.DepartmentID as Credits " +
                 "FROM AdmissionRequiredCourses as arc " +
                 "LEFT JOIN Course as c ON c.CourseID = arc.CourseID " +
                 "WHERE MajorID = " + StudentPreferences.getMajor() + " and SchoolID = " + StudentPreferences.getSchool();
            //This part is added for associate degree code:
            List<int> allAssociateCourses = GetAssociateDegreeRows();
            if (allAssociateCourses != null)
            {
                string assocCoursesCommaJoined = string.Join(",", allAssociateCourses);
                string associatdegreecoursesquery = "SELECT CourseID as CID, MaxCredit, DepartmentID as Credits from Course where CourseID in (" + assocCoursesCommaJoined + ")";
                planBuilder(DBPlugin.ExecuteToDT(query), DBPlugin.ExecuteToDT(associatdegreecoursesquery));
            }
            else
            {
                //if there is no associate degree query/courses query
                planBuilder(DBPlugin.ExecuteToDT(query), DBPlugin.ExecuteToDT(null));
            }
        }
        

        //------------------------------------------------------------------------------
        // HELPER FUNCTION FOR InitDegreePlan
        // Adds the courses from the query to the list of courses that need to be scheduled
        //------------------------------------------------------------------------------
        protected void planBuilder(DataTable dt, DataTable assoc_deg_dt)
        {
            bool CORE_CLASS = true;
            List<Job> courseNums = new List<Job>();

            //adding the admission required courses rows
            foreach (DataRow row in dt.Rows)
            {
                // Need to store more information that we get from the DT
                Job job = new Job((int)row.ItemArray[0], (int)row.ItemArray[1], CORE_CLASS, (int)row.ItemArray[2]);
                courseNums.Add(job);
            }

            //add the associate degree courses entries only if that datatable is not null
            if (assoc_deg_dt != null)
            {
                foreach (DataRow row in assoc_deg_dt.Rows)
                {
                    if (!(courseNums.Any(j => j.GetID() == (int)row.ItemArray[0])))
                    // Need to store more information that we get from the DT
                    {
                        Job job = new Job((int)row.ItemArray[0], (int)row.ItemArray[1], CORE_CLASS, (int)row.ItemArray[2]);
                        courseNums.Add(job);
                    }
                }
            }
            RequiredCourses = new DegreePlan(courseNums);
        }

        //------------------------------------------------------------------------------
        // WARNING!! --Please Read SPECIAL NOTE below--
        // Runs a query which pulls all the courses that exist in CourseTime. Conceptually,
        // this creates a list of every class that has been offered. Details like the 
        // Course ID, the time the class starts, the time the class ends, days offered,
        // quarter offered, and the section ID or collected to their respective 
        // counterparts. This provides a means to reduce the amount of objects being handled
        // and can allow implmentation of day-specific and time-specific preferences.
        // 
        // Various checks prevent duplication of machines.
        //
        // SPECIAL NOTE: If we have the means to, and are required to do so, of 
        //               implementing different course offerings on a yearly basis
        //               this is where we would need to change it. DOING SO WOULD MEAN
        //               THAT THE FUNCTION, NORMALIZEMACHINES(), WOULD BE INCOMPATIBLE
        //               WITH THIS ALGORITHM.
        //------------------------------------------------------------------------------
        protected void InitMachines()
        {

            // This query returns ~2000 rows of course info. We pre-process a lot of info
            // with adding jobs and decidingg where to place maachinens/jobs.

            // If we know the school ID. Then we have what looks like an average of 300 classes less.
            //string oldquery = "select CourseID, StartTimeID, EndTimeID, DayID, QuarterID, SectionID from CourseTime order by CourseID ASC;";
            string query = "SELECT ct.CourseID, StartTimeID, EndTimeID, DayID, QuarterID, ct.SectionID, c.MaxCredit, c.DepartmentID " +
                "FROM CourseTime as ct " +
                "LEFT JOIN Course as c ON c.CourseID = ct.CourseID " +
                "ORDER BY ct.CourseID ASC";
            DataTable dt = DBPlugin.ExecuteToDT(query);
            int dt_size = dt.Rows.Count - 1;
            DataRow dr = dt.Rows[dt_size];

            //Temporary Machine Variables
            Machine dummyMachine = new Machine();
            DayTime dummyDayTime = new DayTime();
            int course = 0;
            int start = 0;
            int end = 0;
            int day = 0;
            int quarter = 0;
            int section = 0;
            int credits = 0;
            int currentCourse = (int)dr.ItemArray[0];  //USED FOR PEAKING THE NEXT ROW
            int currentQuarter = (int)dr.ItemArray[4]; //USED FOR PEAKING THE NEXT ROW
            int currentSection = (int)dr.ItemArray[5]; //USED FOR PEAKING THE NEXT ROW

            //Treats the information gained from the query like a FILO object
            while (dt_size >= 0)
            {
                dr = dt.Rows[dt_size];
                //check for null values
                if (dr.ItemArray[0] == DBNull.Value || dr.ItemArray[1] == DBNull.Value ||
                    dr.ItemArray[2] == DBNull.Value || dr.ItemArray[3] == DBNull.Value ||
                    dr.ItemArray[4] == DBNull.Value || dr.ItemArray[5] == DBNull.Value)
                {
                    dt_size--; //IF any portion is null, then the row is discarded entirely.
                    continue;
                }
                //going to have to do the same with year probably; Andrue Note: Most likely the case
                course = (int)dr.ItemArray[0];
                start = (int)dr.ItemArray[1];
                end = (int)dr.ItemArray[2];
                day = (int)dr.ItemArray[3];
                quarter = (int)dr.ItemArray[4];
                section = (int)dr.ItemArray[5];
                credits = (int)dr.ItemArray[6];
                var deptId = (int) dr.ItemArray[7];

                //same course but different section OR different quarter is a different machine
                //different course is a different machine 
                if ((currentCourse == course && (currentSection != section || currentQuarter != quarter)) || (currentCourse != course))
                {
                    dummyMachine = new Machine(); //creates a new machine to be used
                    currentCourse = (int)dr.ItemArray[0];
                    currentQuarter = (int)dr.ItemArray[4];
                    currentSection = (int)dr.ItemArray[5];
                }

                dummyDayTime = new DayTime();
                dummyDayTime.SetDayTime(day, start, end);
                dummyMachine.AddDayTime(dummyDayTime);
                dummyMachine.SetQuarter(quarter);

                //we add a new machine when we peek to the next row and see
                //(different course) OR (same course and (different section OR dif qtr))
                //Andrue Note: Maybe isolate these arguments into helper functions for ease-of-use?
                //if (itself(?)) OR (not same course) OR (IS course but NOT SAME Section OR Quarter)
                int next = dt_size - 1;
                if (dt_size == 0 || ((int)dt.Rows[next].ItemArray[0] != currentCourse ||
                    ((int)dt.Rows[next].ItemArray[0] == currentCourse &&
                    ((int)dt.Rows[next].ItemArray[5] != currentSection)
                    || (int)dt.Rows[next].ItemArray[4] != currentQuarter)))
                {
                    addMachine(dummyMachine, new Job(course, credits, false));
                }
                dt_size--;
            }
        }

        //------------------------------------------------------------------------------
        // HELPER FUNCTION FOR INITMACHINES()
        //
        // Adds a machine to the machine list for offered courses by first doing a search
        // amongst the machineNodes if the Course already exists there and acts 
        // accordingly.
        //------------------------------------------------------------------------------
        void addMachine(Machine dummyMachine, Job job)
        {
            dummyMachine.AddJob(job); //adds job
            for (int i = 0; i < Quarters.Count; i++)
            {
                MachineNode mn = Quarters[i];
                List<Machine> machines = mn.GetMachines();
                if (machines.Count > 0)
                {
                    for (int j = 0; j < machines.Count; j++)
                    {
                        Machine m = machines[j];
                        if (m == dummyMachine)
                        { //found the machine, just add job
                            m.AddJob(job);
                            break;
                        }
                        else if (dummyMachine.GetYear().Equals(mn.GetYear()) && dummyMachine.GetQuarter().Equals(mn.GetQuarter()))
                        { //machine does not exist, add it in
                            machines.Add(dummyMachine);
                            break;
                        }
                    }
                }
                else if (dummyMachine.GetYear().Equals(mn.GetYear()) && dummyMachine.GetQuarter().Equals(mn.GetQuarter()))
                {
                    machines.Add(dummyMachine);
                    break;
                }
                else //in the instance that machines == 0 and either year or quarter were different 
                {
                    //NOTE: This isn't so much an error as a bookkeeping check. Because CourseTime contains only 1 year
                    //      machines dated beyond the first year throw this error. So this is a database issue.
                    /*
                     Console.WriteLine("Dummy Machine Year: " + dummyMachine.GetYear());
                     Console.WriteLine("Dummy Machine Quarter: " + dummyMachine.GetQuarter());
                     Console.WriteLine("Dummy Course ID: " + course);
                     Console.WriteLine("mn Year: " + mn.GetYear());
                     Console.WriteLine("mn Quarter: " + mn.GetQuarter());
                     Console.WriteLine('\n');
                     */
                }
            }
        }
        #endregion

        protected void ScheduleCourse(Job j)
        {
            if (IsScheduled(j))
            {
                return;
            }
            //we're always being called in order, no need to check for prereqs
            for (int i = 0; i < Quarters.Count; i++)
            {
                MachineNode mn = Quarters[i];
                // Check the number of credits scheduled per quarter make sure it does not exceed preference.
                // Check the number of core credits scheduled per quarter make sure it does not exceed preference.
                // TODO:    Add a default case. What if they dont have a preference should we assign all their classes in one quarter?
                //          Probably not...
                //System.Diagnostics.Debug.WriteLine("NUM OF CREDITS FOR JOB: " + j.GetNumCredits() + " CORE COURSE: " + j.GetCoreCourse());
                if (mn.GetCreditsScheduled() + j.GetNumCredits() > StudentPreferences.getCreditsPerQuarter() ||
                    (j.GetCoreCourse() && j.GetNumCredits() + mn.GetMajorCreditsScheduled() > StudentPreferences.getCoreCredits()))
                {
                    continue;
                }
                List<Machine> machines = mn.GetMachines();

                for (int k = 0; k < machines.Count; k++)
                {
                    //<<----------------------------INSERT DAY/TIME PREFERENCE AND CHECK AGAINST IT
                    Machine m = machines[k];
                    if (m.CanDoJob(j) && !m.CheckInUse())
                    { //if not in use and it can do the job
                        if (Overlap(j, m, mn))

                        { //can't schedule it if the times overlap even if machine found
                            continue;
                        }
                        m.SetCurrentJobProcessing(j);
                        m.SetInUse(true);
                        j.SetScheduled(true);
                        j.SetQuarterScheduled(m.GetQuarter());
                        j.SetYearScheduled(m.GetYear());
                        // Need to update the machine node such that it reflects the new amount of credits, core credits, etc.
                        //mn.AddClassesScheduled(1);
                        mn.AddClassesScheduled(j);
                        Schedule.Add(m);
                        return;
                    }
                }
            }
        }

        protected void AddPrerequisites(Job job, SortedDictionary<int, List<Job>> jobs, bool preferShortest, int currentLevel)
        {
            var courseId = job.GetID();
            List<CourseNode> groups = PrerequisiteNetwork.FindShortPath(courseId);
            if (!PrereqsExist(groups))
            {
                if (!jobs.ContainsKey(currentLevel))
                {
                    jobs.Add(currentLevel, new List<Job>());
                }
                jobs[currentLevel].Add(job);
            }
            else
            {
                int nextLevel = currentLevel - 1;
                int selectedGroup;
                if (preferShortest)
                {
                    selectedGroup = GetShortestGroup(groups);
                }
                else
                {
                    selectedGroup = GetAnyGroup(groups);

                }

                List<CourseNode> group = groups[selectedGroup].prereqs;

                for (int j = 0; j < group.Count; j++)
                {

                    Job myJob = new Job(group[j].PrerequisiteCourseID, group[j].credits, false);
                    AddPrerequisites(myJob, jobs, preferShortest, nextLevel);
                }
                if (!jobs.ContainsKey(currentLevel))
                {
                    jobs.Add(currentLevel, new List<Job>());
                }
                //now finally, add the course
                jobs[currentLevel].Add(job);
            }
        }


        #region Results
        //------------------------------------------------------------------------------
        // This returns a list of the courses that have been scheduled
        //------------------------------------------------------------------------------
        public List<Machine> GetBusyMachines()
        {
            List<Machine> busy = new List<Machine>();
            for (int i = 0; i < Quarters.Count; i++)
            {
                //Console.WriteLine(machineNodes.Count);
                //Console.WriteLine(machineNodes[i].GetYear() + " " + machineNodes[i].GetQuarter());
                List<Machine> machines = Quarters[i].GetAllScheduledMachines();
                for (int j = 0; j < machines.Count; j++)
                {
                    busy.Add(machines[j]);
                }
            }
            return busy;
        }

        //------------------------------------------------------------------------------
        // This returns a list of courses that were not scheduled.
        //------------------------------------------------------------------------------
        public List<Job> GetUnscheduledCourses()
        {
            return unableToSchedule;
        }
        #endregion

        #region Helper Functions
        //------------------------------------------------------------------------------
        // checks if a course is already scheduled. Because courses are returned as 
        // numbers from Cashman network and not Job type, we can't check for the 
        // instance, we have to find it
        //------------------------------------------------------------------------------
        protected bool IsScheduled(Job j)
        {
            if (completedPrior != null)
            {
                if (completedPrior.Count > 0)
                {
                    for (int i = 0; i < completedPrior.Count; i++)
                    {
                        if (j.GetID() == completedPrior[i].GetID())
                        {
                            return true;
                        }
                    }
                }
            }

            for (int i = 0; i < Schedule.Count; i++)
            {
                Machine m = Schedule[i];
                if (m.GetCurrentJobProcessing().GetID() == j.GetID())
                {
                    return true;
                }
            }
            return false;
        }

        //------------------------------------------------------------------------------
        // checks if prerequisite exists; this function can be eliminated, I just didn't
        // quite understand why cashman network had so many lists of lists.
        // 
        // THIS IS A GOOD CHECK AGAINST UNNECCESSARY WORK 
        //------------------------------------------------------------------------------
        protected bool PrereqsExist(List<CourseNode> groups)
        {
            if (groups != null)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].prereqs != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //------------------------------------------------------------------------------
        // AN ERROR CHECKING METHOD TO PREVENT CLASSES FROM HAPPENING DURING THE SAME TIME
        // check to see if a job overlaps with another job's times in a single 
        // MachineNode; can't be in 2 places at once you know?
        //------------------------------------------------------------------------------
        protected bool Overlap(Job j, Machine goal, MachineNode mn)
        {
            bool flag = false; //Defaulted to continue

            //need list of all the start and end times from goal
            List<DayTime> dt = goal.GetDateTime();
            List<Machine> myMachines = mn.GetAllScheduledMachines(); //Look for Scheduled Courses in All Quarters

            for (int i = 0; i < myMachines.Count; i++)
            {
                Machine m = myMachines[i];
                List<DayTime> tempDT = m.GetDateTime();

                //Each class is available for the same amount of days
                if (dt.Count == tempDT.Count)
                {
                    for (int k = 0; k < dt.Count; k++)
                    {
                        //Checks to see if the start time or end time exists between the start and end of a scheduled course
                        if ((dt[k].GetStartTime() >= tempDT[k].GetStartTime() && dt[k].GetStartTime() <= tempDT[k].GetEndTime()) ||
                        (dt[k].GetEndTime() >= tempDT[k].GetStartTime() && dt[k].GetEndTime() <= tempDT[k].GetEndTime()))
                        {
                            flag = true;
                        }
                    }
                }
                else
                {
                    int min = Math.Min(dt.Count, tempDT.Count); //Which class starts earlier
                    //Compares the courses for the day they are taken on
                    if (dt.Count == min)
                    {
                        flag = compareDays(dt, tempDT);
                    }
                    else
                    {
                        flag = compareDays(tempDT, dt);
                    }
                    if (flag)
                    {
                        return flag;
                    }
                }
            }
            return flag;
        }

        //------------------------------------------------------------------------------
        // helper function for overlap. it's pretty tricky to compare the lists when
        // they are not the same length because of nulls and out of ranges. Can be 
        // eliminated with a more clever algorithm that works of any size lists
        //------------------------------------------------------------------------------
        protected bool compareDays(List<DayTime> smaller, List<DayTime> larger)
        {
            for (int k = 0; k < smaller.Count; k++)
            {// go through all days in smaller
                int smallDay = smaller[k].GetDay(); //get day from smaller
                int largeDayIndex = -1;

                for (int j = 0; j < larger.Count; j++)
                { //find that day in larger
                    if (larger[j].GetDay() == smallDay)
                    {
                        largeDayIndex = j;
                        break;
                    }
                }
                if (largeDayIndex == -1)
                    return false;
                //compare that day
                if ((smaller[k].GetStartTime() >= larger[largeDayIndex].GetStartTime() && smaller[k].GetStartTime() <= larger[largeDayIndex].GetEndTime()) ||
                (smaller[k].GetEndTime() >= larger[largeDayIndex].GetStartTime() && smaller[k].GetEndTime() <= larger[largeDayIndex].GetEndTime()))
                {
                    return true;
                }
            }
            return false;
        }

        //------------------------------------------------------------------------------
        // if for course A you have to take [B, F, K] OR [J, Z], we pick the latter
        // option because we don't want to take a lot of classes; in the long run,
        // this is not always the fastest option so this can be optimized
        //------------------------------------------------------------------------------
        protected int GetShortestGroup(List<CourseNode> groups)
        {
            if (groups == null)
            {
                return 0;
            }
            int shortest = int.MaxValue;
            int shortestGroup = int.MaxValue;
            for (int j = 1; j < groups.Count; j++)
            { //find the shortest group that is not null
                var groupCount = 1 + GetShortestGroup(groups[j].prereqs);
                if (groupCount < shortest)
                {
                    //shortest = groupCount;
                    shortestGroup = j;
                }
            }//so now we have the shortest list
            return shortestGroup;
        }

        //------------------------------------------------------------------------------
        // if for course A you have to take [B, F, K] OR [J, Z], we pick the latter
        // option because we don't want to take a lot of classes; in the long run,
        // this is not always the fastest option so this can be optimized
        //------------------------------------------------------------------------------
        protected int GetAnyGroup(List<CourseNode> groups)
        {
            if (groups == null)
            {
                return 0;
            }
            int shortest = 1;
            var random = new Random();
            var randomGroup = random.Next(groups.Count - 1);
            return Math.Max(shortest, randomGroup);


        }
        #endregion


        #region associatedegree
        public List<int> GetAssociateDegreeRows()
        {
            var connection = new DBConnection();
            List<int> allAssociatedgreeelectivescoursesids = new List<int>();
            var associatedegmodel = new AssociateDegree
            {
                creditRequirements = new List<int>(),
                requirements = new List<Requirement>(),
                coursesIndex = new Dictionary<int, string>()
            };
            int associatesMajorID = getAssociateDegreeMajorID();
            //if the associates major id is 0, then there is no mapping present, so return null
            if (associatesMajorID == 0)
                return null;
                        //var requirementsQuery = $"select RequirementID, Credits from Requirement where MajorID = 36";//for business
            var requirementsQuery = $"select RequirementID, Credits from Requirement where MajorID = {associatesMajorID}";//for mech- pre engg associates
            var requirements = connection.ExecuteToDT(requirementsQuery);
            //variables below to store the index of requiremnet to create entries in the map
            int requirementsIndex = 0;
            string requirementsStr = "";
            string seperator = ","; //sepertor string to actually denote on what literal to seperate.
            string entriesseperator = ":";//seperator to seperate the different entries of a course oobject
            foreach (DataRow reqRow in requirements.Rows)
            {
                Debug.WriteLine("RequirementsID:" + reqRow["RequirementID"] + "    credits:" + reqRow["credits"]);
                //schedule.Add((int)schedulesRow["GeneratedPlanId"]);
                var reqID = (int)reqRow["RequirementID"];
                var electiveIdquery = $"select ElectiveID, MinimumCredits, MaximumCredits from Elective where RequirementID = {reqID}";
                var con1 = new DBConnection();
                var elctiveIDresult = con1.ExecuteToDT(electiveIdquery);
                requirementsStr = requirementsIndex.ToString();

                //object of the requirement class that stores all the elective groups associated with that requirement
                var reqobj = new Requirement()
                {
                    creditstosatisfy = (int)reqRow["credits"],
                    remcreditsTosatisfy = (int)reqRow["credits"],
                    electivegroups = new List<Elective>()
                };
                //variables to store the indices of the courses as they are present in the map
                int electivesgroupindex = 0;
                //string to store the elecive grups index for the dictionary of courses
                string electivegroupstr = "";
                //execute the query to get the elective groups for that requirement ID - reqID
                foreach (DataRow electiveIDrow in elctiveIDresult.Rows)
                {
                    var electiveID = (int)electiveIDrow["ElectiveID"];

                    //set the value of the electivegroup index to the electivegroup index string
                    electivegroupstr = electivesgroupindex.ToString();
                    string finalelectiverepStr = string.Concat(string.Concat(requirementsStr, seperator), electivegroupstr);

                    //sometimes the maximum credits can be null as stored in the db-hence to handle it
                    var maxcreditscheck = electiveIDrow["MaximumCredits"];
                    int maxcredits = 0;
                    if (maxcreditscheck == DBNull.Value)
                    {
                        Debug.WriteLine("max creits is null");
                        maxcredits = 0;
                    }
                    else
                    {
                        maxcredits = (int)electiveIDrow["MaximumCredits"];
                    }

                    var electivesQuery = $"select ec.CourseID as courseid from ElectiveClass as ec INNER JOIN ElectiveToClass as etc on etc.ElectiveClassID = ec.ElectiveClassID INNER JOIN Elective as e on e.ElectiveID = etc.ElectiveID where e.ElectiveID={electiveID}";
                    var con2 = new DBConnection();
                    var electivesResult = con2.ExecuteToDT(electivesQuery);
                    //denotes a particular elective group in a requirement
                    var electivegroup = new Elective()
                    {
                        finalelectivecourses = new List<ElectiveCourse>(),
                        remaelectivecourses = new List<ElectiveCourse>(),
                        totalmaxcreditstosatisfy = maxcredits,
                        mincreditstosatisfy = (int)electiveIDrow["MinimumCredits"],
                        creditsdone = 0

                    };
                    //The query results to obtain the elective course details for each elective group
                    foreach (DataRow electiveRow in electivesResult.Rows)
                    {
                        //Debug.WriteLine("Electives are:"+electiveRow["courseid"]);
                        var electivecourseid = (int)electiveRow["courseid"];
                        var coursecreditsquery = $"select Maxcredit from Course where CourseID={electivecourseid}";
                        var electivecoursecreditresult = connection.ExecuteToString(coursecreditsquery);
                        var electivecredits = Int32.Parse(electivecoursecreditresult);
                        var electivecourse = new ElectiveCourse()
                        {
                            courseid = electivecourseid,
                            credits = electivecredits
                        };
                        //add the elective course to that elective group
                        electivegroup.finalelectivecourses.Add(electivecourse);

                        //This is the logic for adding the electives of all the associate degree possible ones.
                        //add all the course ids into a list that is maintained to store the course ids possible for all the associate degree electives
                        //can delete: Debug.WriteLine("req ID: " + requirementsIndex + "  elecyive grp index:" + electivesgroupindex);
                        allAssociatedgreeelectivescoursesids.Add(electivecourseid);

                        if (associatedegmodel.coursesIndex.ContainsKey(electivecourseid))
                        {
                            string result = associatedegmodel.coursesIndex[electivecourseid];
                            associatedegmodel.coursesIndex[electivecourseid] = string.Concat(string.Concat(result, entriesseperator), finalelectiverepStr);
                        }
                        else
                        {
                            //add the elective course id into the map and the string associated with it
                            associatedegmodel.coursesIndex.Add(electivecourseid, finalelectiverepStr);
                        }
                    }
                    //compute the total number of elective courses in each group and then set into its member variable
                    electivegroup.totalnumelectives = electivegroup.finalelectivecourses.Count;
                    electivegroup.remnumelectives = electivegroup.totalnumelectives;
                    //set the remaining elective courses as the same as final elective courses list- when processing this later we will remove that course id
                    electivegroup.remaelectivecourses.AddRange(electivegroup.finalelectivecourses);

                    //add this elective group to the reqiurement obj
                    reqobj.electivegroups.Add(electivegroup);
                    //increase the elective groups variable
                    electivesgroupindex++;
                }
                //add the reuirement obj to the associate degree obj.
                associatedegmodel.requirements.Add(reqobj);
                //increase the requirements index by 1
                requirementsIndex++;
            }

            //This part of just for testing
            //just creating an object of prereqnetwork-courseNetwork and then executing the functionalities as that of initnetwork() in schedulerbase class
            //CourseNetwork PrerequisiteNetwork;
            //string rawpreqs = connection.ExecuteToString("SELECT p.CourseID, MaxCredit as credits, GroupID, PrerequisiteID, PrerequisiteCourseID " +
            //     "FROM Prerequisite as p " +
            //     "LEFT JOIN Course as c ON c.CourseID = p.CourseID " +
            //     "FOR JSON PATH");
            //string rawcourses = connection.ExecuteToString("SELECT CourseID, MaxCredit as credits " +
            //    "FROM Course " +
            //    "FOR JSON PATH");
            ////NETWORK BUILD
            //PrerequisiteNetwork = new CourseNetwork(rawcourses, rawpreqs);
            //PrerequisiteNetwork.BuildNetwork();


            //This is the functionality of initDegreePlan() inside SchedulerBase() which thereby calls planBuilder() to create the list of jobs for these courseids
            /*string degreeplanquery = "SELECT arc.CourseID as CID, c.MaxCredit, c.DepartmentID as Credits " +
                 "FROM AdmissionRequiredCourses as arc " +
                 "LEFT JOIN Course as c ON c.CourseID = arc.CourseID " +
                 "WHERE MajorID = 22 and SchoolID = 6"; //business */
            string degreeplanquery = "SELECT arc.CourseID as CID, c.MaxCredit, c.DepartmentID as Credits " +
     "FROM AdmissionRequiredCourses as arc " +
     "LEFT JOIN Course as c ON c.CourseID = arc.CourseID " +
     "WHERE MajorID = " + StudentPreferences.getMajor() + " and SchoolID = " + StudentPreferences.getSchool(); //mechanical 
            List<int> allReqCourseIds = new List<int>();
            DataTable dtar = connection.ExecuteToDT(degreeplanquery);
            foreach (DataRow row in dtar.Rows)
            {
                // Need to store more information that we get from the DT into the list of all required courses for admission
                allReqCourseIds.Add((int)row.ItemArray[0]);
                Debug.WriteLine("Admission required course id:" + (int)row.ItemArray[0]);
            }

            //iterate through all the course ids and find its prerequisites and store them in a list
            //List<int> prerequistecourseslist = new List<int>();
            List<int> finalprerequistecourseslist = new List<int>();
            foreach (int reqCourse in allReqCourseIds)
            {
                List<int> prerequistecourseslist = new List<int>();
                prerequistecourseslist = addAllPreqCourses(prerequistecourseslist, reqCourse, PrerequisiteNetwork);
                foreach (int prereqcourse in prerequistecourseslist)
                {
                    if (!finalprerequistecourseslist.Contains(prereqcourse))
                        finalprerequistecourseslist.Add(prereqcourse);
                }
            }


            //print all the prereq courses in final prereq list
            //can delete: Debug.WriteLine("All prereq courses list");
            foreach (int id in finalprerequistecourseslist)
            {
                //can delete: Debug.WriteLine(" pre req id is: " + id);
                //add these prereq courses to the required courses list- to get a final compiled list of all these possible courses
                if (!allReqCourseIds.Contains(id))
                    allReqCourseIds.Add(id);
            }

            //Maintain a list of all associate degree elective courses that are already present in the required courses list
            List<int> electivesAsReqCourses = new List<int>();
            //Loop/generate the courses list 
            foreach (int electiveid in allAssociatedgreeelectivescoursesids)
            {
                if (allReqCourseIds.Contains(electiveid) && !electivesAsReqCourses.Contains(electiveid))
                {
                    electivesAsReqCourses.Add(electiveid);
                }
            }

            //Now call the function to access the associate degree object and change the values of member variables to
            //have the remaining courses in each group to be scheduled and the remaining credits to be scheduled in each group updated
            assessAssociateDegreeModelObj(ref associatedegmodel, electivesAsReqCourses);

            //For this associate degree object, calculate the credits qualified and satisfied propoerties for all the EGs
            associatedegmodel.evaluateAllRequirements();

            //just to print and check which requirements are done and not done
            int i = 0;
            foreach (Requirement r in associatedegmodel.requirements)
            {
                Debug.WriteLine("requireemnt :" + i + " : needed-total:" + r.creditstosatisfy + "  already satisfied:" + r.creditsAlreadysatisfied + "  remaining: " + r.remcreditsTosatisfy);
                if (r.remcreditsTosatisfy > 0)
                {
                    Debug.WriteLine("Reamining schedulable EGs:" + string.Join(",", r.RemainingSchedulableEG));
                    if (r.UnSatisfiedEG != null)
                    {
                        Debug.WriteLine("Unsatisfied EG:" + string.Join(",", r.UnSatisfiedEG));
                        Debug.WriteLine("Courses in the remaining list that can be scheduled for this unsatisfiedEG:--");
                        foreach (int x in r.UnSatisfiedEG)
                        {
                            Debug.WriteLine("The remaining credits to satisfy for this Eg:" + r.electivegroups[x].remcreditstosatisfy);
                            foreach (ElectiveCourse ec in r.electivegroups[x].remaelectivecourses)
                            {
                                //Debug.WriteLine(" course Credits:" + ec.credits);
                            }
                        }
                    }
                }
                i++;
            }

            i = 0;
            //schedule the unsatisfie EGS:
            Debug.WriteLine("\n-----scheduling unsatisfied Requiremnets and EGs necessary to schedule------");
            foreach (Requirement r in associatedegmodel.requirements)
            {

                if (r.UnSatisfiedEG != null)
                {
                    Debug.WriteLine("--------------------Requireemnt: " + i + " -------------");
                    r.satisfyUnsatisfiedEGs();
                }
                i++;
            }


            //schedule the courses in Egs where the requirements have more than 0 in remaining credits to satisfy
            Debug.WriteLine("\n-----scheduling remaining Requiremnets credits for meeting credits------");
            foreach (Requirement r in associatedegmodel.requirements)
            {

                if (r.remcreditsTosatisfy > 0)
                {
                    Debug.WriteLine("--------------------Requirement: " + i + " -------------");
                    r.satisfyRemainingCreditsForRequirement();
                }
                i++;
            }

            //return the list of courses added in all the requirements->EGs->courses needed and courses extra
            List<int> compiledListOfAllCourses = new List<int>();
            foreach (Requirement r in associatedegmodel.requirements)
            {
                foreach(Elective e in r.electivegroups)
                {
                    if(e.coursesNeeded!=null && e.coursesNeeded.Count>0)
                    {
                        foreach(ElectiveCourse ec in e.coursesNeeded) {
                            compiledListOfAllCourses.Add(ec.courseid);
                        }
                        //compiledListOfAllCourses.AddRange(e.coursesNeeded);
                    }
                    if(e.coursesExtra != null && e.coursesExtra.Count > 0)
                    {
                        //compiledListOfAllCourses.AddRange(e.coursesExtra);
                        foreach (ElectiveCourse ec in e.coursesExtra)
                        {
                            compiledListOfAllCourses.Add(ec.courseid);
                        }
                    }
                }
                
            }

            return compiledListOfAllCourses;

        }

        public List<int> addAllPreqCourses(List<int> courseslist, int courseID, CourseNetwork PrerequisiteNetwork)
        {
            List<Scheduler.CourseNode> groups = PrerequisiteNetwork.FindShortPath(courseID);
            bool prereqsnotexist = false;
            if (groups != null)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].prereqs != null)
                    {
                        prereqsnotexist = true;
                        break;
                    }
                }
            }
            if (!prereqsnotexist)
            {
                courseslist.Add(courseID);
                return courseslist;
            }
            else
            {
                //int nextLevel = currentLevel - 1;
                int selectedGroup;
                selectedGroup = GetShortestGroup(groups);

                List<Scheduler.CourseNode> group = groups[selectedGroup].prereqs;
                List<int> addedprereqlist = new List<int>();
                addedprereqlist.AddRange(courseslist);
                for (int j = 0; j < group.Count; j++)
                {
                    addedprereqlist.AddRange(addAllPreqCourses(addedprereqlist, group[j].PrerequisiteCourseID, PrerequisiteNetwork));
                    //Job myJob = new Job(group[j].PrerequisiteCourseID, group[j].credits, false);
                    //AddPrerequisites(myJob, jobs, preferShortest, nextLevel);
                }
                //now finally, add the course
                if (!courseslist.Contains(courseID))
                    addedprereqlist.Add(courseID);
                //jobs[currentLevel].Add(job);
                return addedprereqlist;
            }

        }

        //function to measure and set the values of the elective groups and credits in each of them based on the courses already present
        public void assessAssociateDegreeModelObj(ref AssociateDegree assoobj, List<int> allPrereqReqdElectiveCourses)
        {
            foreach (int courseid in allPrereqReqdElectiveCourses)
            {
                if (assoobj.coursesIndex.ContainsKey(courseid))
                {
                    string result = assoobj.coursesIndex[courseid];
                    string[] splitresults = result.Split(':');
                    List<string> splittedresultsList = new List<string>(splitresults.Length);
                    splittedresultsList.AddRange(splitresults);
                    foreach (string electiveidLocation in splittedresultsList)
                    {
                        string[] objectidsarray = electiveidLocation.Trim().Split(',');
                        List<string> objectidsList = new List<string>(objectidsarray.Length);
                        objectidsList.AddRange(objectidsarray);
                        //The first index is requireemts section object index, second index is elective groups object index
                        int reqIndex = int.Parse(objectidsList[0].Trim());
                        int electivegrpIndex = int.Parse(objectidsList[1].Trim());
                        //access this in the associatedegree object itself
                        ElectiveCourse ec = assoobj.requirements[reqIndex].electivegroups[electivegrpIndex].remaelectivecourses.Find(x => x.courseid == courseid);
                        if (ec != null) //its checked for null to ensure there is no repeated processing of course ids as the allPrereqReqdElectiveCourses list can have duplicates
                        {
                            Debug.WriteLine("Going to remove:" + ec.courseid + " req index: " + reqIndex + " elec grp index: " + electivegrpIndex);
                            int coursecredits = ec.credits;// save the credits of the course to be removed and minus it from the remainig credits to be satisfied in the elective group and also in the requirement obj
                            assoobj.requirements[reqIndex].electivegroups[electivegrpIndex].creditsdone += coursecredits;
                            //remove the course from the remaining courses list
                            assoobj.requirements[reqIndex].electivegroups[electivegrpIndex].remaelectivecourses.RemoveAll(x => x.courseid == courseid);
                            ec.taken = true; // set the taken of that course to be true as its already taken

                            //going the long way round by taking the index
                            int index = assoobj.requirements[reqIndex].electivegroups[electivegrpIndex].finalelectivecourses.FindIndex(x => x.courseid == courseid);
                            assoobj.requirements[reqIndex].electivegroups[electivegrpIndex].finalelectivecourses[index].taken = true;
                        }
                        else
                        {
                            Debug.WriteLine("null could not to remove:" + courseid + " req index: " + reqIndex + " elec grp index: " + electivegrpIndex);
                        }
                    }

                }
            }

        }
        
        //This function gets the associate degree 
        public int getAssociateDegreeMajorID()
        {
            int normalmajorid = StudentPreferences.getMajor();
            int associatesmajorid = 0;
            if (normalmajorid == 22)
                associatesmajorid = 36;
            else if (normalmajorid == 16)
                associatesmajorid = 17;
            else if (normalmajorid == 1 || normalmajorid == 20)
                associatesmajorid = 18;
            else if (normalmajorid == 32 || normalmajorid == 6)
                associatesmajorid = 37;
            else if (normalmajorid == 14 || normalmajorid == 8)
                associatesmajorid = 19;
            return associatesmajorid;
        }

        #endregion



    }
}
