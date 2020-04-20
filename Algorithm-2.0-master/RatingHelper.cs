using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler
{
    public class RatingHelper
    {
        public static void UpdateWeakLabelScore(int scheduleId, int score)
        {
            var connection = new DBConnection();
            var ratingQuery = $"update GeneratedPlan set WeakLabelScore={score} where GeneratedPlanId={scheduleId}";
            connection.ExecuteToString(ratingQuery);
        }
    }
}
