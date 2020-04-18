using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Contracts
{
    using Newtonsoft.Json.Linq;

    public class Schedule
    {
        public List<Machine> Courses { get; set; }
        public string SchedulerName { get; set; }
        public JToken ScheduleSettings { get; set; }
    }
}
