using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleEvaluator
{
    class Schedule
    {
        // Figure out how to store a schedule here.
        // I think the best way to do this is abstract the schedule.
        // by this I mean talk to all of the resources above the evaluator package
        // probably at the API level, and at the level talk to all of the 
        // resources and build this object. 

        // That way the evaluator stays true to its one purpose of just evlauating
        // a schedule. 

        // This will hopefully make it easier for defining the concrete Criteria
        // classes getResults method.
        
        // Regardless all of the information that any of the implementations of Criteria
        // need should be stored here. NOT passed into its getResult method exclusive of 
        // this objcect. Helps maintain a MVC esque pattern

    }
}
