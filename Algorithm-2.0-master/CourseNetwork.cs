#region Author Information
/*
 * Andrue Cashman
 * University of Washington Bothell
 * Undergraduate Research Winter 2018
 * Advisor: Dr. Erika Parsons
 * Project: Virtual Student Advisor
*/
#endregion

#region CourseNetwork Information
/*
 * CourseNetwork
 * Purpose: To build a network of courses and their respective prerequisites to be used in the 
 *          Virtual Student Advisor Project in assisting students and/or advisors in scheduling
 *          courses.
 * 
 * Assumptions: All courses and prerequisiste information are correct in that they are saved as 
 *              json string files.
 *                  -SQL queries for correct JSON structure based on the VSA database
 *                      -AllCourses: select CourseID from Course for JSON AUTO;
 *                      -PrereqNetwork: Select CourseID, GroupID, PrerequisiteID from Prerequisite for JSON AUTO
 *              All information from the database is correct.
 *              CourseNode is in the structure of int CourseID, int GroupID, int PrerequisiteID, List<CourseNode> prereqs
*/
#endregion

#region Libraries and Namespaces
using System;
using System.Collections.Generic;
using Newtonsoft.Json; //IMPORTANT: Used for translating the JSON strings into a usable format
#endregion

namespace Scheduler {
    public class CourseNetwork {
        #region Private Structures and Variables
        private string allCourses; //represents all the courses that are offered
        private string prereqs; //represents the prerequisites that exist for all courses
        private List<CourseNode> courseNetwork; //The Final combined structure of courses and their prerequisites
        private List<int> courseIndex; //A helper object that offers a O(1) lookup of courses
        #endregion

        #region Constructor
        //Input parameter 1: the JSON string that represents all the courses
        //Input parameter 2: the JSON string that represents all the prerequisites
        public CourseNetwork(string rawcourses, string rawpreqs) {
            allCourses = rawcourses;
            prereqs = rawpreqs;
        }
        #endregion

        #region Build Network
        //Purpose: Creates the Course Network and Course Index objects from the two strings used
        //         to instantiate the object (allCourses, PrereqNetwork)
        //Assumptions: Courses are not sequential and the amount of courses can be a value that is less than
        //             the value of the CourseID's
        //             Correct files and format were used to instantiate ALL_COURSES, and PRE_REQS
        //NOTE: Prerequisites added to their respective courses can occur in random order and should not be depended
        //      on for sequencing or sorting algorithms.
        public void BuildNetwork() {
            //Function Variables
            courseNetwork = JsonConvert.DeserializeObject<List<CourseNode>>(allCourses);
            List<CourseNode> smaller = JsonConvert.DeserializeObject<List<CourseNode>>(prereqs);
            BuildIndex(); //Builds the Index for the courses for O(1) lookup

            for (int i = 0; i < smaller.Count; i++) //iterates over the entire string of prerequisite information
            {
                //Checks to see if the prereq object in the CourseNetwork-->CourseNode is empty
                if (courseNetwork[courseIndex[smaller[i].courseID]].prereqs == null) {
                    courseNetwork[courseIndex[smaller[i].courseID]].prereqs = new List<CourseNode>();
                }

                //Adds the Prereq CourseNode to the Prereq List of the CourseNetwork CourseNode
                courseNetwork[courseIndex[smaller[i].courseID]].prereqs.Add(smaller[i]);
            }
        }
        #endregion

        #region Build Index
        //Purpose: Create an object to allow fast O(1) access to the Course Network
        //Assumptions: CourseID's are non-negative
        //Result: Creates a table whose index is a CourseID that exists in the CourseNetwork and whose value
        //        is the index of CourseNetwork where the CourseID resides. 
        private void BuildIndex() {
            courseIndex = new List<int>();

            //Builds the Course Index to be used for O(1) access to information in CourseNetwork Object
            for (int i = 0; i < courseNetwork.Count; i++) //iterates over all the courses
            {
                //Check to see if the CourseID is greater than the size of the Index table
                if ((courseIndex.Count - 1) < courseNetwork[i].courseID) {
                    for (int j = courseIndex.Count - 1; j < courseNetwork[i].courseID; j++) {
                        courseIndex.Add(-1); //Sets all newly added indices to -1 for error checking purposes
                    }
                    //Sets the value of the index to the index of the CourseNetwork object of the corresponding courseID
                    //Example: CourseID 199 exists as the value of CourseNetwork[197]; courseIndex[199] provides 197.
                    courseIndex[courseNetwork[i].courseID] = i;
                }
            }
        }
        #endregion

        #region Find Short Path
        //Purpose: Creates an object of a target Course and that has its direct prerequisite paths indexed separately
        //         into their respective Group ID's. This is to help with the AND/OR nature of prerequisites for some 
        //         courses.
        //         EXAMPLE: COURSE 199 has 4 groupID's each group representing the prerequisite courses required to take 
        //                  COURSE 199. Each groupID represents a different selection of courses. Though 199 has 17 
        //                  prerequisite courses in total, only the courses in any one of the Groups need to be completed
        //                  to take course 199.
        //Input: A course ID
        //Output: a List of Course Nodes in the format
        //          foo[0]->CourseID
        //          foo[1]->GroupID 1, Prereq[0]->prerequisite course, prereq[1]->2nd prerequisite course
        //Assumptions: The prerequisites (should they exist) of each course are in no particular order
        public List<CourseNode> FindShortPath(int targetID) {
            List<CourseNode> targetCourse = new List<CourseNode>(); //creates object to be passed
            int index = courseIndex[targetID]; //Used for consistency and readability

            //check for valid course
            if (index > -1) {
                targetCourse.Add(new CourseNode()); //Adds first element: targetCourse[0]
                targetCourse[0].courseID = targetID; //sets identifying course ID

                //Checks if Course has prerequisites
                if (courseNetwork[index].prereqs != null) {
                    //iterates over prerequisites for coures
                    for (int j = 0; j < courseNetwork[index].prereqs.Count; j++) {
                        int groupPath = courseNetwork[index].prereqs[j].groupID; //For Consistency and readability

                        //Makes an index for each group path where the index is equal to the group ID
                        if (groupPath > (targetCourse.Count - 1)) {
                            while ((targetCourse.Count - 1) < groupPath) {
                                targetCourse.Add(new CourseNode());
                            }
                        }

                        //Checks to see if List object for the Group Course Node is empty 
                        if (targetCourse[groupPath].prereqs == null) {
                            targetCourse[groupPath].makeNewList();
                        }

                        //Assign Values
                        targetCourse[groupPath].courseID = targetID;
                        targetCourse[groupPath].groupID = groupPath;
                        var coursePrereq = FindShortPath(courseNetwork[index].prereqs[j].PrerequisiteCourseID);
                        courseNetwork[index].prereqs[j].prereqs = coursePrereq;
                        targetCourse[groupPath].prereqs.Add(new CourseNode(courseNetwork[index].prereqs[j], true));
                    }

                    //var shortestPath = 1;
                    ////find the path with the least prereqs
                    //foreach (var courseNode in targetCourse)
                    //{
                    //    if (courseNode.prereqs!=null && courseNode.prereqs.Count < targetCourse[shortestPath].prereqs.Count)
                    //    {
                    //        shortestPath = courseNode.groupID;
                    //    }
                    //}

                    //targetCourse = targetCourse[shortestPath].prereqs;
                }
            } else {
                return null; //Course Does Not Exist
            }
            return targetCourse;
        }
        #endregion

        #region Show Network
        //A Console output of the Course Network Object
        public void ShowNetwork() {
            //Iterates over the Course Network
            for (int i = 0; i < courseNetwork.Count; i++) {
                //Display Course Number
                Console.Write("\t" + courseNetwork[i].courseID + ": ");

                //Checks if Course has Prerequisite Courses
                if (courseNetwork[i].prereqs == null) {
                    Console.WriteLine("No Prerequisite Courses");
                } else {
                    //Starts List of Prerequistes
                    Console.Write("Prerequisite Courses: ");

                    //Iterates over Course Node Prerequisite List
                    for (int j = 0; j < courseNetwork[i].prereqs.Count; j++) {
                        Console.Write(courseNetwork[i].prereqs[j].prerequisiteID + ", ");
                        //Console.Write(courseNetwork[i].prereqs[j].groupID + "; ");
                    }
                    Console.WriteLine(""); //End Line for readaability
                }
            }
        }
        #endregion

        #region Show Short List
        //Purpose Shows the prerequisite paths of a List of Course Node Objects
        public void ShowShortList(List<CourseNode> targetList) {
            //Checks to see if targetList is valid
            if (targetList != null) {
                //Checks to see if targetList has prerequisites in groupPaths
                if (targetList.Count > 1) {
                    //Course Information
                    Console.WriteLine("\tCourse " + targetList[0].courseID + " ");
                    //iterates over the targetList object
                    for (int i = 1; i < targetList.Count; i++) {
                        //Shows Group Information
                        if (targetList[i].prereqs != null) {
                            Console.Write("\t\tGroup " + targetList[i].groupID + " Prerequisites: ");
                            //iterates over Prerequisite Object
                            for (int j = 0; j < targetList[i].prereqs.Count; j++) {
                                Console.Write(targetList[i].prereqs[j].prerequisiteID + "; ");
                            }
                            Console.WriteLine("");
                        }
                    }
                } else {
                    Console.WriteLine("\tCourse " + targetList[0].courseID + " has no Prerequisites");
                }
            } else {
                Console.WriteLine("\tCourse Does Not exist");
            }

        }
    }
    #endregion
}
