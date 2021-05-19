using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Newtonsoft.Json;



namespace DataGenerator
{
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.InteropServices;
    using ApiCore;
    using FluentAssertions;
    using Models;
    using ScheduleEvaluator;
    using Scheduler;
    using DBConnection = Scheduler.DBConnection;
    using Preferences = Models.Preferences;
    using Scheduler.Algorithms;
    using DataModel;
    using commonDataModel;

    //using Clustering;

    [TestClass]
    public class DataGenerator
    {
        private DBConnection db;
        [TestInitialize]
        public void Initialize()
        {
            db = new DBConnection();
        }

        // This is commmented out to ignore this test. This test generates a lot of data
        // and should be ran sparingly as to not fill the database with a lot of redundant
        // test data.
        [TestMethod]
        public void GenerateData()
        {
            Console.WriteLine("HELLLOOOOOOO....");
            Debug.WriteLine("HELLLOOOOOOO....");
            var insertedList = new List<int>();
            var coursePrefList = new List<CourseObject>();
            //var schools = new List<string>(){ "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            //var majors = new List<string>(){ "1", "2", "3", "4", "6", "7", "8", "9", "10", "11", "12", "13", "14", "16", "17", "18", "19", "20", "21", "22", "23", "24", "26", "27", "28", "29", "30", "31", "32" };
            // var schools = new List<string>(){"1"};
            //var majors = new List<string>(){"1"};

            var schools = new List<string>() { "6" };
            var majors = new List<string>() { "22" };

            /*var schools = new List<string>() { "1" };
            var majors = new List<string>() { "16" };*/ //mech
            /*foreach (string school in schools)
            {
                foreach (string major in majors)
                {
                    var courseObj = new CourseObject()
                    {
                        school = school,
                        courses = "5",
                        credits = "19",
                        major = major,
                        quarters = "29",
                        enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                        job = ((int)Constants.JobType.Unemployed).ToString(),
                        summer = "Y"
                    };

                   //// var courseObj = new CourseObject()
                   //// {
                   //     school = school,
                   //     courses = "5",
                   //     credits = "10",
                   //     major = major,
                   //     quarters = "20",
                   //     enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                   //     job = ((int)Constants.JobType.Unemployed).ToString(),
                   //     summer = "N"
                   // //};
                    coursePrefList.Add(courseObj);
                    //VaryEnrollment(courseObj, coursePrefList);
                    //VaryJob(courseObj, coursePrefList);
                    //VaryCredits(courseObj, coursePrefList);
                    //VaryMaxCreditsPerQuarter(courseObj, coursePrefList);
                    GenerateAssociatesPlan(coursePrefList, insertedList);
                }
            }*/


            var associatesprefobj = new CourseObject()
            {
                school = "1",
                courses = "5",
                credits = "19",
                major = "22",
                quarters = "29",
                enrollment = "1",
                job = "3",
                summer = "Y"
            };
            coursePrefList.Add(associatesprefobj);
            GenerateAssociatesPlan(coursePrefList, insertedList);
            insertedList.Should().NotBeEmpty();
        }

        [TestMethod]
        public void GenerateAssociateDegreePlanData()
        {
            var insertedList = new List<int>();
            var coursePrefList = new List<CourseObject>();
            var associatesprefobj = new CourseObject()
            {
                school = "1",
                courses = "5",
                credits = "19",
                major = "22",
                quarters = "29",
                enrollment = "1",
                job = "3",
                summer = "Y"
            };
            coursePrefList.Add(associatesprefobj);
            GenerateAssociatesPlan(coursePrefList, insertedList);
            insertedList.Should().NotBeEmpty();
        }



        private void VaryMaxCreditsPerQuarter(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var quarters = new List<int>() { 1, 2, 4, 8, 9, 12, 15 };
            foreach (var credit in quarters)
            {
                var newCourseObj = new CourseObject()
                {
                    job = courseObj.job,
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = courseObj.credits,
                    summer = courseObj.summer,
                    enrollment = courseObj.enrollment,
                    quarters = credit.ToString()
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        [TestMethod]
        public void GetAllRating()
        {
            var schedule = new List<int>() { };
            var connection = new DBConnection();
            var eval = new Evaluator();
            var scheduleQuery = $"select GeneratedPlanId from GeneratedPlan where GeneratedPlanId > 600";
            var schedules = connection.ExecuteToDT(scheduleQuery);
            foreach (DataRow schedulesRow in schedules.Rows)
            {
                schedule.Add((int)schedulesRow["GeneratedPlanId"]);
            }
            

            foreach (int scheduleId in schedule)
            {
                var parameterQuery = $"select ParameterSetId from GeneratedPlan where GeneratedPlanId={scheduleId}";
                var parameterId = (int)connection.ExecuteToDT(parameterQuery).Rows[0]["ParameterSetId"];

                var parameterSetQuery = $"select ParameterSet.MajorID, SchoolID, TimePeriod, MaxNumberOfQuarters, NumberCoreCoursesPerQuarter, CreditsPerQuarter, SummerPreference, DepartmentId from ParameterSet join Major on ParameterSet.MajorID = Major.MajorID" +
                                        $" join TimePreference on TimePreference.TimePreferenceID = ParameterSet.TimePreferenceID"+ 
                                        $" where ParameterSetId = {parameterId}";
                var parameterSetResult = connection.ExecuteToDT(parameterSetQuery);

                var parameters = Preferences.ConvertFromDatabase(parameterSetResult, parameterId);

                var query = "select CourseNumber, QuarterID, YearID, Course.CourseId, Course.DepartmentID from StudyPlan" +
                            " join course on Course.CourseID = StudyPlan.CourseID" +
                            $" where GeneratedPlanID = {scheduleId}";

                var results = connection.ExecuteToDT(query);
                var model = ScheduleModel.ConvertFromDatabase(results, scheduleId, parameters);
                var rating = eval.evalaute(model);

                RatingHelper.UpdateWeakLabelScore(scheduleId, rating);

            }
        }

        [TestMethod]
        public void GetAssociateDegreeRows()
        {
            var connection = new DBConnection();
            List<int> allAssociatedgreeelectivescoursesids = new List<int>();
            var associatedegmodel = new AssociateDegree
            {
                creditRequirements = new List<int>(),
                requirements = new List<Requirement>(),
                coursesIndex = new Dictionary<int, string>()
            };
            //var requirementsQuery = $"select RequirementID, Credits from Requirement where MajorID = 36"; //business
            var requirementsQuery = $"select RequirementID, Credits from Requirement where MajorID = 17"; //mech - pre engg associates major
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
                foreach(DataRow electiveIDrow in elctiveIDresult.Rows)
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
                    foreach(DataRow electiveRow in electivesResult.Rows)
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

                        if(associatedegmodel.coursesIndex.ContainsKey(electivecourseid))
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
            CourseNetwork PrerequisiteNetwork;
            string rawpreqs = connection.ExecuteToString("SELECT p.CourseID, MaxCredit as credits, GroupID, PrerequisiteID, PrerequisiteCourseID " +
                 "FROM Prerequisite as p " +
                 "LEFT JOIN Course as c ON c.CourseID = p.CourseID " +
                 "FOR JSON PATH");
            string rawcourses = connection.ExecuteToString("SELECT CourseID, MaxCredit as credits " +
                "FROM Course " +
                "FOR JSON PATH");
            //NETWORK BUILD
            PrerequisiteNetwork = new CourseNetwork(rawcourses, rawpreqs);
            PrerequisiteNetwork.BuildNetwork();


            //This is the functionality of initDegreePlan() inside SchedulerBase() which thereby calls planBuilder() to create the list of jobs for these courseids
           /* string degreeplanquery = "SELECT arc.CourseID as CID, c.MaxCredit, c.DepartmentID as Credits " +
                 "FROM AdmissionRequiredCourses as arc " +
                 "LEFT JOIN Course as c ON c.CourseID = arc.CourseID " +
                 "WHERE MajorID = 22 and SchoolID = 6";//business */
            string degreeplanquery = "SELECT arc.CourseID as CID, c.MaxCredit, c.DepartmentID as Credits " +
                 "FROM AdmissionRequiredCourses as arc " +
                 "LEFT JOIN Course as c ON c.CourseID = arc.CourseID " +
                 "WHERE MajorID = 16 and SchoolID = 1"; //mechanical
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
                foreach(int prereqcourse in prerequistecourseslist)
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
            foreach(int electiveid in allAssociatedgreeelectivescoursesids)
            {
                if(allReqCourseIds.Contains(electiveid) && !electivesAsReqCourses.Contains(electiveid))
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
            foreach(Requirement r in associatedegmodel.requirements)
            {
                Debug.WriteLine("requireemnt :"+ i + " : needed-total:" + r.creditstosatisfy + "  already satisfied:"+ r.creditsAlreadysatisfied + "  remaining: "+ r.remcreditsTosatisfy);
                if(r.remcreditsTosatisfy > 0)
                {
                    Debug.WriteLine("Reamining schedulable EGs:" + string.Join(",", r.RemainingSchedulableEG));
                    if(r.UnSatisfiedEG != null)
                    {
                        Debug.WriteLine("Unsatisfied EG:" + string.Join(",", r.UnSatisfiedEG));
                        Debug.WriteLine("Courses in the remaining list that can be scheduled for this unsatisfiedEG:--");
                        foreach(int x in r.UnSatisfiedEG)
                        {
                            Debug.WriteLine("The remaining credits to satisfy for this Eg:" + r.electivegroups[x].remcreditstosatisfy);
                            foreach(ElectiveCourse ec in r.electivegroups[x].remaelectivecourses)
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


            i = 0;
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


            //testing the string generation part for scheduling
            
            


            List<int> compiledListOfAllCourses = new List<int>();
            foreach (Requirement r in associatedegmodel.requirements)
            {
                foreach (Elective e in r.electivegroups)
                {
                    if (e.coursesNeeded != null && e.coursesNeeded.Count > 0)
                    {
                        foreach (ElectiveCourse ec in e.coursesNeeded)
                        {
                            compiledListOfAllCourses.Add(ec.courseid);
                        }
                        //compiledListOfAllCourses.AddRange(e.coursesNeeded);
                    }
                    if (e.coursesExtra != null && e.coursesExtra.Count > 0)
                    {
                        //compiledListOfAllCourses.AddRange(e.coursesExtra);
                        foreach (ElectiveCourse ec in e.coursesExtra)
                        {
                            compiledListOfAllCourses.Add(ec.courseid);
                        }
                    }
                }

            }

            string assocCoursesCommaJoined = string.Join(",", compiledListOfAllCourses);
            string associatdegreecoursesquery = "SELECT CourseID as CID, MaxCredit, DepartmentID as Credits from Course where CourseID in (" + assocCoursesCommaJoined + ")";
            var con_temp = new DBConnection();
            DataTable dt = con_temp.ExecuteToDT(associatdegreecoursesquery);
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
            if(!prereqsnotexist)
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
                if(!courseslist.Contains(courseID))
                    addedprereqlist.Add(courseID);
                //jobs[currentLevel].Add(job);
                return addedprereqlist;
            }
             
        }

        //function to measure and set the values of the elective groups and credits in each of them based on the courses already present
        public void assessAssociateDegreeModelObj(ref AssociateDegree assoobj, List<int> allPrereqReqdElectiveCourses)
        {
            foreach(int courseid in allPrereqReqdElectiveCourses)
            {
                if(assoobj.coursesIndex.ContainsKey(courseid))
                {
                    string result = assoobj.coursesIndex[courseid];
                    string[] splitresults = result.Split(':');
                    List<string> splittedresultsList = new List<string>(splitresults.Length);
                    splittedresultsList.AddRange(splitresults);
                    foreach(string electiveidLocation in splittedresultsList)
                    {
                        string[] objectidsarray = electiveidLocation.Trim().Split(',');
                        List<string> objectidsList = new List<string>(objectidsarray.Length);
                        objectidsList.AddRange(objectidsarray);
                        //The first index is requireemts section object index, second index is elective groups object index
                        int reqIndex = int.Parse(objectidsList[0].Trim());
                        int electivegrpIndex = int.Parse(objectidsList[1].Trim());
                        //access this in the associatedegree object itself
                        ElectiveCourse ec = assoobj.requirements[reqIndex].electivegroups[electivegrpIndex].remaelectivecourses.Find(x => x.courseid == courseid);
                        if(ec!=null) //its checked for null to ensure there is no repeated processing of course ids as the allPrereqReqdElectiveCourses list can have duplicates
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

        //copied this from schedulerbase class just for testing here in datagenerator
        public int GetShortestGroup(List<Scheduler.CourseNode> groups)
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

        [TestMethod]
        public void ComputerEngineeringPreReqRegularTest()
        {
            // Constants for a pre-determined major/school combo
            const string COMPUTER_ENGINEERING_MAJOR = "1";
            const string UNIVERSITY_WA = "1";
            const string MAX_QUARTERS = "15";

            // From my experience this seems to be a common schedule preference
            // set. Using this object as a concrete example to help debug and test.
            int schedId = PreferenceHelper.ProcessPreference(new CourseObject()
            {
                school = UNIVERSITY_WA,
                credits = "10",
                courses = "10",
                major = COMPUTER_ENGINEERING_MAJOR,
                quarters = MAX_QUARTERS,
                enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                job = ((int)Constants.JobType.Unemployed).ToString(),
                summer = "N"
            }, false);

            // Check that a schedule has been been made
            Assert.AreNotEqual(0, schedId);

            // The first check should be that all the prereqs are satisfied in the schedule.
            string result = db.ExecuteToString(getFulfillQueryString(schedId));

            // This query will return 0 if all of the prereqs are met.
            Assert.AreEqual(0, Int32.Parse(result));

            result = db.ExecuteToString(getScheduleQueryString(schedId));
            Debug.WriteLine("THIS IS THE OUTPUT for ID" + schedId + " " + result);
        }

        // Very similar to the above, although this method tests the shortest schedule.s
        [TestMethod]
        public void ComputerEngineeringPreReqShortTest()
        {
            // Constants for a pre-determined major/school combo
            const string COMPUTER_ENGINEERING_MAJOR = "1";
            const string UNIVERSITY_WA = "1";
            const string MAX_QUARTERS = "15";

            // From my experience this seems to be a common schedule preference
            // set. Using this object as a concrete example to help debug and test.
            int schedId = PreferenceHelper.ProcessPreference(new CourseObject()
            {
                school = UNIVERSITY_WA,
                credits = "10",
                courses = "10",
                major = COMPUTER_ENGINEERING_MAJOR,
                quarters = MAX_QUARTERS,
                enrollment = ((int)Constants.EnrollmentType.FullTime).ToString(),
                job = ((int)Constants.JobType.Unemployed).ToString(),
                summer = "N"
            }, true);

            // Check that a schedule has been been made
            Assert.AreNotEqual(0, schedId);

            // The first check should be that all the prereqs are satisfied in the schedule.
            string result = db.ExecuteToString(getFulfillQueryString(schedId));

            // This query will return 0 if all of the prereqs are met.
            Assert.AreEqual(0, Int32.Parse(result));

            result = db.ExecuteToString(getScheduleQueryString(schedId));
            Debug.WriteLine("THIS IS THE OUTPUT for ID" + schedId + " " + result);
        }

        private string getFulfillQueryString(int id)
        {
            return "SELECT COUNT(*) FROM (SELECT DISTINCT PrerequisiteCourseID as PID " +
                "FROM StudyPlan as sp, Prerequisite as p WHERE sp.CourseID = p.CourseID AND sp.GeneratedPlanID = " + id + ") as interim " +
                "LEFT JOIN StudyPlan as p ON interim.PID = p.CourseID " +
                "WHERE p.GeneratedPlanID is NULL";
        }

        private string getScheduleQueryString(int id)
        {
            return "SELECT   YearID as YID, CourseID as CID " +
                "FROM StudyPlan WHERE GeneratedPlanID = " + id +
                "ORDER BY QuarterID ASC";
        }

        private void VaryCredits(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var credits = new List<int>() { 5, 10, 15, 3, 1 };
            foreach (var credit in credits)
            {
                var newCourseObj = new CourseObject()
                {
                    job = courseObj.job,
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = credit.ToString(),
                    summer = courseObj.summer,
                    enrollment = courseObj.enrollment,
                    quarters = courseObj.quarters
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        private void VaryJob(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var jobTypes = new List<Constants.JobType>() { Constants.JobType.FullTime, Constants.JobType.PartTime, Constants.JobType.Unemployed };
            foreach (var jobType in jobTypes)
            {
                var newCourseObj = new CourseObject()
                {
                    job = ((int)jobType).ToString(),
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = courseObj.credits,
                    summer = courseObj.summer,
                    enrollment = courseObj.enrollment,
                    quarters = courseObj.quarters
                };
                coursePrefList.Add(newCourseObj);
            }
        }

        private void VaryEnrollment(CourseObject courseObj, List<CourseObject> coursePrefList)
        {
            var enrollmentTypes = new List<Constants.EnrollmentType>() { Constants.EnrollmentType.FullTime, Constants.EnrollmentType.PartTime };
            foreach (var enrollmentType in enrollmentTypes)
            {
                var newCourseObj = new CourseObject()
                {
                    job = courseObj.job,
                    school = courseObj.school,
                    major = courseObj.major,
                    credits = courseObj.credits,
                    summer = courseObj.summer,
                    enrollment = ((int)enrollmentType).ToString(),
                    quarters = courseObj.quarters
                };
                coursePrefList.Add(newCourseObj);
            }
        }
        

        private void GeneratePlan(List<CourseObject> courseObj, List<int> insertedList)
        {
            foreach (var courseObject in courseObj)
            {
                var insertedId = PreferenceHelper.ProcessPreference(courseObject, true);
                //temporarily commenting this for testing - iswarya please remove in the future for any further testing
                //insertedId.Should().NotBe(0);
                Debug.WriteLine("inserted ID is:" + insertedId);
                insertedList.Add(insertedId);
            }
        }

        private void GenerateAssociatesPlan(List<CourseObject> courseObj, List<int> insertedList)
        {
            foreach (var courseObject in courseObj)
            {
                var insertedId = PreferenceHelper.ProcessAssociatesPreference(courseObject, true);
                
                //temporarily commenting this for testing - iswarya please remove in the future for any further testing
                //insertedId.Should().NotBe(0);
                Debug.WriteLine("inserted ID is:" + insertedId);
                insertedList.Add(insertedId);
            }
        }


        // This is commmented out to ignore this test. This test generates a lot of data
        // and should be ran sparingly as to not fill the database with a lot of redundant
        // test data.
        [TestMethod]
        public void dataModelTest()
        {

            DataModel.Program prog = new Program();
            //prog.test();
            prog.executeClustering();
            Debug.WriteLine("Executed the data model test...");
        }

        //testing recommendations
        [TestMethod]
        public void testRecommendations()
        {
            var connection = new DBConnection();
            var parameterQuery = $"select ParameterSetId from GeneratedPlan where GeneratedPlanId=9713";
            var parameterId = (int)connection.ExecuteToDT(parameterQuery).Rows[0]["ParameterSetId"];

            var parameterSetQuery = $"select ParameterSet.MajorID, SchoolID, TimePeriod, MaxNumberOfQuarters, NumberCoreCoursesPerQuarter, CreditsPerQuarter, SummerPreference, DepartmentId from ParameterSet join Major on ParameterSet.MajorID = Major.MajorID" +
                                    $" join TimePreference on TimePreference.TimePreferenceID = ParameterSet.TimePreferenceID" +
                                    $" where ParameterSetId = {parameterId}";
            var parameterSetResult = connection.ExecuteToDT(parameterSetQuery);

            var parameters = Preferences.ConvertFromDatabase(parameterSetResult, parameterId);

            var similarPlansQuery = $"select gp.GeneratedPlanID from GeneratedPlan as gp join ParameterSet as ps on gp.ParameterSetID=ps.ParameterSetID where ps.MajorID={parameters.MajorID} and ps.SchoolID= {parameters.SchoolId}";
            var similarPlansResults = connection.ExecuteToDT(similarPlansQuery);
            foreach (DataRow row in similarPlansResults.Rows)
            {
                var planID = (int)row["GeneratedPlanID"];

            }
            Random r = new Random();
            int rand_index0 = r.Next(0, similarPlansResults.Rows.Count);
            int rand_index1 = r.Next(0, similarPlansResults.Rows.Count);

            Debug.WriteLine("Random indices are:" + rand_index0 + "  " + similarPlansResults.Rows[rand_index0]["GeneratedPlanID"]);
            Debug.WriteLine("Random indices are:" + rand_index1 + "  " + similarPlansResults.Rows[rand_index1]["GeneratedPlanID"]);

            var query = "select CourseNumber, QuarterID, YearID, Course.CourseId, DepartmentId from StudyPlan" +
                        " join course on Course.CourseID = StudyPlan.CourseID" +
                        $" where GeneratedPlanID = {similarPlansResults.Rows[rand_index0]["GeneratedPlanID"]}";


            var results = connection.ExecuteToDT(query);
            var model = ScheduleModel.ConvertFromDatabase(results, rand_index0, parameters);
            var response = JsonConvert.SerializeObject(model);
            //comtinutaion
            var query1 = "select CourseNumber, QuarterID, YearID, Course.CourseId, DepartmentId from StudyPlan" +
                        " join course on Course.CourseID = StudyPlan.CourseID" +
                        $" where GeneratedPlanID = {similarPlansResults.Rows[rand_index1]["GeneratedPlanID"]}";
            var results1 = connection.ExecuteToDT(query1);
            var model1 = ScheduleModel.ConvertFromDatabase(results1, rand_index1, parameters);

            response += "\n";
            response += JsonConvert.SerializeObject(model1);
            Debug.WriteLine(response);
        }
    }
}
