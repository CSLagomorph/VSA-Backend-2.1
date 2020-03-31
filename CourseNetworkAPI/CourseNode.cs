#region Author Information
/*
 * Andrue Cashman
 * University of Washington Bothell
 * Undergraduate Research Winter 2018
 * Advisor: Dr. Erika Parsons
 * Project: Virtual Student Advisor
*/
#endregion

#region Course Node Information
/*
 * Defines the structure of the Course Node that is used by the Course Network
*/
#endregion

#region Libraries and Namespaces
using System.Collections.Generic;
#endregion

namespace CourseNetworkAPI
{
    public class CourseNode 
    {
        #region Structure Variables
        public int courseID { get; set; } //Represents the Unique ID number for a course
        public int credits { get; set; }
        public int groupID { get; set; } //Represents the Group ID for the Course
        public int prerequisiteID { get; set; } //Represents the Unique ID number for a Prerequisite Course
        public int PrerequisiteCourseID { get; set; } //Represents the Unique ID number for a Prerequisite Course
        public List<CourseNode> prereqs { get; set; } //A list of prerequisites
        #endregion

        private Dictionary<int, int> PrerequisiteMap = new Dictionary<int, int>();
        #region Constructors
        #region Default Constructor
        public CourseNode()
        {
            courseID = 0;
            groupID = 0;
            prerequisiteID = 0;
            prereqs = null;
            credits = 0;
        }
        #endregion]

        #region Two-Parameter Constructor
        public CourseNode(CourseNode temp, bool withList)
        {
            courseID = temp.courseID;
            groupID = temp.groupID;
            prerequisiteID = temp.prerequisiteID;
            PrerequisiteCourseID = temp.PrerequisiteCourseID;
            credits = temp.credits;
            if (withList && temp.prereqs != null)
            {
                prereqs = temp.prereqs;
            }
            else
            {
                prereqs = null;
            }
        }


        #endregion
        #endregion

        #region Helper Function
        public bool makeNewList()
        {
            prereqs = new List<CourseNode>();
            return true;
        }
        #endregion
    }
}
