using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Contracts
{
    interface IScheduler
    {
        Schedule CreateSchedule(bool preferShortest);
    } 
}
