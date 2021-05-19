using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

/*
 * The terminology, EG means Elective group that contains the different electives and their course ids
 */
namespace Scheduler.Algorithms
{
    using System.Data;
    using Newtonsoft.Json;
    public partial class AssociateDegree
    {
        public List<int> creditRequirements; //credit reqs to fulfill for each req
        public List<Requirement> requirements; //electives groups present for each req-stored here
        public Dictionary<int, string> coursesIndex; //map to store the courseID as key and the index of the object in which it is placed as the value of the string.

        #region Constructors
        
        public AssociateDegree()
        {
            requirements = null;
            creditRequirements = null;
            coursesIndex = new Dictionary<int, string>();
        }

        #endregion

        //function to evaluate the credits satisfied and qualified for all the list of requirements for this associate degree
        public void evaluateAllRequirements()
        {
            this.requirements.ForEach(x => x.evaluateCreditsSatifsied());
        }
    }

    public partial class Requirement
    {
        public int creditstosatisfy; //total credit reqs to fulfill for each req
        public int creditsAlreadysatisfied; //the sum of credits of all courses that are taken which accounts to this requirement's satisfaction.
        public int remcreditsTosatisfy;//remaining credits from this requirement to satisfy
        public List<Elective> electivegroups; //electives groups present for each req-stored here
        public List<int> UnSatisfiedEG; //list of EG ids whose minimum credit requirements havent been satisfied.
        public List<int> RemainingSchedulableEG; //list of the EG ids which can be used to schedule the remaining credit requirements to satisfy

        #region Constructors

        public Requirement()
        {
            electivegroups = null;
            creditstosatisfy = 0;
            remcreditsTosatisfy = 0;
            creditsAlreadysatisfied = 0;
            UnSatisfiedEG = null;
            RemainingSchedulableEG = new List<int>();
        }

        public void init()
        {
            remcreditsTosatisfy = 0;
            creditsAlreadysatisfied = 0;
            UnSatisfiedEG = null;
            RemainingSchedulableEG = new List<int>();
        }

        #endregion

        //function that calculates the credits satisfied already in this requirement and thereby compute the remaining credits to satisfy in that requirement
        public void evaluateCreditsSatifsied()
        {
            int EGid = 0;
            init(); //initiliase the varibales of this class so that all their valuation starts fresh
            foreach(Elective e in electivegroups)
            {
                e.evaluateCreditsQualified(); //call the function to evaluate the credits qualified from that elective group
                creditsAlreadysatisfied += e.creditsqualified; //calculate the credits already satisfied from each EG
                if(e.remcreditstosatisfy > 0)
                {
                    if(UnSatisfiedEG==null)
                    {
                        UnSatisfiedEG = new List<int>();
                    }
                    UnSatisfiedEG.Add(EGid);
                }
                if (!e.maxcreditssatisfied && e.remaelectivecourses.Count > 0) //store the list of EG ids which can be used for scheduling the remaining courses to satisfy that requirement
                    RemainingSchedulableEG.Add(EGid);

                EGid++;
            }
            //check if all the elective groups in this requiremnt are satisfied and its credits satisfied is greater than the number of credits to meet the satisfaction
            computeRemainingCreditsToSatisfy();
        }

        //function to comoute the remaining credits to satisfy for the set of requirements out of the UnsatisfiedEGs
        public void computeRemainingCreditsToSatisfy()
        {
            //initiliase remaining credits to satisfy before computing it
            remcreditsTosatisfy = 0;
            //check if all the elective groups in this requiremnt are satisfied and its credits satisfied is greater than the number of credits to meet the satisfaction
            if (UnSatisfiedEG == null || !(UnSatisfiedEG.Any()))
            {
                if (creditsAlreadysatisfied >= creditstosatisfy)
                    remcreditsTosatisfy = 0;
                else
                {
                    remcreditsTosatisfy = creditstosatisfy - creditsAlreadysatisfied;
                }
            }
            else
            {
                remcreditsTosatisfy = this.getRequiredcreditsToSatisfy();
            }
        }


        //Function that gives the sum of required credits still to take from the elective groups list - these elective groups are first required to be satisfied
        public int getRequiredcreditsToSatisfy()
        {
            int requirementNotSatisfied = 0;
            foreach(Elective e in electivegroups)
            {
                if (e.remcreditstosatisfy > 0)
                {
                    requirementNotSatisfied += e.remcreditstosatisfy;
                   
                }                    
            }
            return requirementNotSatisfied;
        }

        //Function that satisfies the unsatisfied elective groups- by taking the courses that meets the min credits requirements
        public void satisfyUnsatisfiedEGs()
        {
            if(UnSatisfiedEG!=null) //this happens only when there are elective gropus in this requirement tht are not satisfied to its minimum credits
            {
                List<int> UnSatisfiedEGscopy = new List<int>(); //just for having a copy of the unsatified EGS for looping
                List<int> EGnumsToRemove = new List<int>();
                UnSatisfiedEGscopy.AddRange(UnSatisfiedEG);
                foreach (int EGnum in UnSatisfiedEGscopy)
                {
                    int creditsqualifiedprev = electivegroups[EGnum].creditsqualified;
                    Debug.WriteLine("Scheduling required credits for EG: " + EGnum + " --- credits to meet:"+ electivegroups[EGnum].remcreditstosatisfy);
                    int creditsscheduled = electivegroups[EGnum].scheduleForMinCredits();
                    int creditsqualifiedlater = electivegroups[EGnum].creditsqualified;
                    Debug.WriteLine("Scheduled credits are: " + creditsscheduled);
                    if (creditsscheduled > 0)
                        this.creditsAlreadysatisfied += creditsqualifiedlater - creditsqualifiedprev;
                    if (electivegroups[EGnum].remcreditstosatisfy <= 0) //since the required credits have been satisfied, remove the element from the unsatisfied list
                        UnSatisfiedEG.Remove(EGnum);
                    //if the max credits satisfied is reached then remove that EG from the remaining schedulable egS
                    if (electivegroups[EGnum].maxcreditssatisfied || electivegroups[EGnum].remaelectivecourses.Count <= 0)
                        RemainingSchedulableEG.RemoveAll(x => x == EGnum);
                }
                //UnSatisfiedEG.Clear();                
            }
            //now reevaluate the entire needed credits to satisfy for this entire requirement
            computeRemainingCreditsToSatisfy();
            Debug.WriteLine("Remaining credits to satisfy are for this requirement:"+remcreditsTosatisfy);
        }

        //This function schedules courses for the requirement - to meet the credits to complete this requirement
        public void satisfyRemainingCreditsForRequirement()
        {
            while(remcreditsTosatisfy > 0 && RemainingSchedulableEG.Count > 0)
            {
                /*
                //removing this random part - to select teh first element in EGs for scheuling the course
                //pick a random elective group and schedule a course in it
                Random r = new Random();
                int rand_index = r.Next(0, RemainingSchedulableEG.Count);
                */
                int rand_index = 0; //seeting the random index to 0
                int creditsscheduled = electivegroups[RemainingSchedulableEG[rand_index]].scheduleForRemainingCreditsForRequirement(remcreditsTosatisfy);
                remcreditsTosatisfy -= creditsscheduled;
                if (remcreditsTosatisfy < 0)
                    remcreditsTosatisfy = 0;
                creditsAlreadysatisfied += creditsscheduled;
                if(electivegroups[RemainingSchedulableEG[rand_index]].maxcreditssatisfied || electivegroups[RemainingSchedulableEG[rand_index]].remaelectivecourses.Count <= 0)
                    //RemainingSchedulableEG.RemoveAll(x => x == rand_index);
                    RemainingSchedulableEG.RemoveAt(rand_index);
            }
            Debug.WriteLine("credits data for this requirement after scheduling:---creditsTosatisfy:" + creditstosatisfy + "  credits already satisfied:" + creditsAlreadysatisfied + "  credits remaining to be satisfied:" + remcreditsTosatisfy); ;
        }
    }


    public partial class Elective
    {
        public int totalnumelectives; //stores total num of all electives
        public int remnumelectives; //remanining number of electives to be taken
        public int totalmaxcreditstosatisfy; //total max crdits to satisfy in this elective group
        public int mincreditstosatisfy;//min credits in a elective group to satisfy
        public int remcreditstosatisfy; //remaining credits to satisy from taking electives in this group- this will become more than 0 only if the credits taken in this EG is lesser than the minimum credits to satisfy
        public int creditsdone; // total credits of the courses taken from this elective group
        public int creditsqualified; // total credits taken from this elective group that accounts to this requirement's credits requirements
        public bool maxcreditssatisfied; //boolean varibale to denote if this EG's max credit limit is already satisfied.

        public List<ElectiveCourse> finalelectivecourses; //elective courses stored to have the list of all elective courses
        public List<ElectiveCourse> remaelectivecourses; //elective courses stored to have the rest of courses 

        //The below varibales store the courses that get scheduled after explicitly calling them to get scheduled or to get selected
        public List<ElectiveCourse> coursesNeeded; //list of courses to take (needed) to satisfy the minimum credits - this will have entries only when this EG's remcreditstosatisy>0
        public List<ElectiveCourse> coursesExtra;  //list of courses to take extra to meet the requirements final credits



        public Elective()
        {
            totalnumelectives = 0;
            remnumelectives = 0;
            totalmaxcreditstosatisfy = 0;
            mincreditstosatisfy = 0;
            remcreditstosatisfy = 0;
            creditsdone = 0;
            creditsqualified = 0;
            maxcreditssatisfied = false;
        }

        //function that evaluates the credits that qualify for the requiremnt's credits requiremts
        public void evaluateCreditsQualified()
        {
            //if the credits taken is greater than the maximum that can be eligible for this elective group's eligiblity, then its used
            if(this.totalmaxcreditstosatisfy != 0)
            {
                if(creditsdone >= totalmaxcreditstosatisfy && creditsdone >= mincreditstosatisfy)
                {
                    creditsqualified = totalmaxcreditstosatisfy;
                    maxcreditssatisfied = true; //set the maximum credits satisfied for this EG
                }
                else
                {
                    creditsqualified = creditsdone;
                }
            }
            else
            {
                creditsqualified = creditsdone;
            }
            //calculate the remaining credits to satisfy in order to fulfill this elective group
            if(creditsqualified < mincreditstosatisfy)
            {
                remcreditstosatisfy = mincreditstosatisfy - creditsdone;
            }
        }

        //function to pick the remaining courses and match the remaining credits to satisfy in order to finish this course. returns the total credits scheduled/added
        //what this function does return the credits scheduled in order to meet the min credits required for this EG
        public int scheduleForMinCredits()
        {           
            int creditsScheduled = 0;
            //do the below functionality until all the remaining credits are satisfied. - so this happens in a loop

            while (remcreditstosatisfy > 0)
            {
                // for now just implementing the logic to get all the courses whose credits are equal to the remaining credits to meet the min requirements
                /*List<ElectiveCourse> equalCreditCourses = remaelectivecourses.FindAll(x => x.credits == remcreditstosatisfy);
                //pick the random element index from this list
                Random r = new Random();
                int rand_index = r.Next(0, equalCreditCourses.Count);
                ElectiveCourse courseToSchedule = equalCreditCourses[rand_index];
                */
                //get the course that needs to be scheduled for this
                ElectiveCourse courseToSchedule = decideWhichCourseToSchedule(remcreditstosatisfy, remaelectivecourses);
                if (coursesNeeded == null)
                    coursesNeeded = new List<ElectiveCourse>();
                coursesNeeded.Add(courseToSchedule); // add this course to the list of courses needeed to be scheduled
                creditsdone += courseToSchedule.credits; // add the credits of this course to the creditsdone for this EG 
                creditsScheduled  += courseToSchedule.credits;
                evaluateCreditsQualified(); //call this function to again recalculate the remaining credits to satisfy and the credits qualified.
                //subtract this course's credits from the remaining credits to satisfy
                remcreditstosatisfy -= courseToSchedule.credits;
                if (remcreditstosatisfy < 0)
                    remcreditstosatisfy = 0;
                remaelectivecourses.RemoveAll(x => x.courseid == courseToSchedule.courseid); ///remove that course scheduled from the remaining elective courses that can be used for scheduling

                Debug.WriteLine("Scheduled course : "+ courseToSchedule.courseid + "  : " + courseToSchedule.credits);
            }
            return creditsScheduled;
        }


        //function to pick the remaining courses and schedules a course to satisfy the credits requiremet passed. returns the total credits scheduled/added
        //what this function does: returns the credits scheduled in order 
        public int scheduleForRemainingCreditsForRequirement(int creditsToSchedule)
        {
            int creditsScheduled = 0;
            //do the below functionality until all the remaining credits are satisfied. - so this happens in a loop
            //get the course that needs to be scheduled for this
            ElectiveCourse courseToSchedule = decideWhichCourseToSchedule(creditsToSchedule, remaelectivecourses);
            if(courseToSchedule!=null)
            {
                if (coursesExtra == null)
                    coursesExtra = new List<ElectiveCourse>();
                coursesExtra.Add(courseToSchedule); // add this course to the list of courses that are extra to be schedules for this requirement
                creditsdone += courseToSchedule.credits; // add the credits of this course to the creditsdone for this EG 
                creditsScheduled += courseToSchedule.credits;
                evaluateCreditsQualified(); //call this function to again recalculate the remaining credits to satisfy and the credits qualified.
                                            //subtract this course's credits from the remaining credits to satisfy
                                            //remcreditstosatisfy -= courseToSchedule.credits;
                                            //if (remcreditstosatisfy < 0)
                                            //remcreditstosatisfy = 0;
                remaelectivecourses.RemoveAll(x => x.courseid == courseToSchedule.courseid); ///remove that course scheduled from the remaining elective courses that can be used for scheduling
                Debug.WriteLine("Scheduled course : " + courseToSchedule.courseid + "  : " + courseToSchedule.credits);
            }                     
            return creditsScheduled;
        }

        //function to decide which course to schedule. take the most possible ans - this first sees if there are equal credit courses and picks one from it randowmly. 
        //else takes the first least highest credit course in the list. 
        //else it takes the highest credit course from the list of coures having lesser credits than the credits that need to be fulfilled.

        //parameters- coursesList: list of elective courses from which the course for scheduling needs to be selected
        //creditstocompoensate - number of credits for which it has to be scheduled.
        public ElectiveCourse decideWhichCourseToSchedule(int creditsToCompensate, List<ElectiveCourse> coursesList)
        {
            ElectiveCourse course_selected = null;
            List<ElectiveCourse> equalCreditCourses = coursesList.FindAll(x => x.credits == creditsToCompensate);
            if(equalCreditCourses.Count > 0)
            {
                //currently removing the logic for picking the random index for course scheduling
                
                Random r = new Random();
                int rand_index = r.Next(0, equalCreditCourses.Count);
                
                //just pick the first element for scheduling the 
                //int rand_index = 0;

                course_selected = equalCreditCourses[rand_index];
            }
            else
            {
                List<ElectiveCourse> greaterCreditCourses = coursesList.FindAll(x => x.credits > creditsToCompensate);
                if(greaterCreditCourses.Count > 0)
                {
                    var newSortedList = greaterCreditCourses.OrderBy(x => x.credits).ToList();
                    course_selected = newSortedList[0];
                }
                else
                {
                    List<ElectiveCourse> lesserrCreditCourses = coursesList.FindAll(x => x.credits < creditsToCompensate);
                    if(lesserrCreditCourses.Count > 0)
                    {
                        var newSortedList = lesserrCreditCourses.OrderByDescending(x => x.credits).ToList();
                        course_selected = newSortedList[0];
                    }                    
                }                
            }
            return course_selected;
        }
                
    }

    public partial class ElectiveCourse
    {
        public int courseid;
        public int credits;
        public bool taken;
        public ElectiveCourse()
        {
            courseid = 0;
            credits = 0;
            taken = false;
        } 

    }
}
