using System;
using Newtonsoft.Json;

namespace Scheduler
{
    using Algorithms;

    public class Driver
    {
        //------------------------------------------------------------------------------
        // The driver is used to run the scheduler. Either Method is a valid means to run
        // the scheduler. Should function calls be made to alter the preferences
        // CreateScheduler() would need to be ran again to ensure correct output.
        //------------------------------------------------------------------------------
        static void Main(string[] args)
        {
            #region Method 1: Manual Input of Preferences
            JobShopScheduler scheduler = new JobShopScheduler(335, false);
            //scheduler.MakeStartingPoint("ENGL& 101", "MATH& 141");
            var r1 = scheduler.CreateSchedule(true);
            #endregion

            OpenShopScheduler scheduler2 = new OpenShopScheduler(335, false);
            //scheduler.MakeStartingPoint("ENGL& 101", "MATH& 141");
            var r2 = scheduler.CreateSchedule(true);

            //#region Method 2: Automated based off of ParameterSetID
            //Scheduler scheduler = new Scheduler(1);
            //#endregion
            var sch = JsonConvert.SerializeObject(scheduler, Formatting.Indented);
            Console.WriteLine(sch);
            Console.ReadLine();
        }
    }
}
