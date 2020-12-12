using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Contracts
{
    using Algorithms;
    using Models;
    using Newtonsoft.Json.Linq;

    public class Schedule
    {
        public List<Machine> Courses { get; set; }
        public string SchedulerName { get; set; }
        public OpenShopGASchedulerSettings ScheduleSettings { get; set; }
        public double Rating { get; set; }

        public ScheduleModel ConvertToScheduleModel()
        {
            var model = new ScheduleModel
            {
                Quarters = new List<Quarter>()
            };

            var byYear = this.Courses.GroupBy(s => s.GetYear());
            foreach (var kvp in byYear)
            {
                var byQuarter = kvp.GroupBy(s => s.GetQuarter());
                foreach (var quarter in byQuarter)
                {
                    var quarterItem = new Quarter
                    {
                        Year = kvp.Key,
                        Title = $"{DateTime.UtcNow.Year + kvp.Key}-{quarter.Key.ToString()}",
                        Id = $"{quarter.Key}",
                        Courses = new List<Course>(),
                        QuarterKey = quarter.Key
                    };
                    model.Quarters.Add(quarterItem);

                    foreach (var course in quarter)
                    {
                        var description = course.GetCurrentJobProcessing().GetID().ToString();
                        int departmentId = course.GetCurrentJobProcessing().GetDepartmentID();
                        quarterItem.Courses.Add(new Course()
                            {Description = description, Id = description, Title = description, DepartmentID = departmentId});

                    }
                }
            }

            return model;
        }
    }
}
