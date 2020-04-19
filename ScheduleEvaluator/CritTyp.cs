using System;
using System.Collections.Generic;
using System.Text;

// The enumeration CritTyp boils all of the different subclassses into easily identifiable
// integers. 
// For every sub-class of Criteria there should be a label in the enumeration for it.
namespace ScheduleEvaluator
{
    public enum CritTyp { 
        AllPrereqs = 1,
        CoreClassesLastYear,
        CoreCreditsAQuarter,
        ElectiveRelevancy,
        EnglishTime,
        MajorSpecificBreaks,
        MathBreaks,
        MaxQuarters,
        PreRequisiteOrder,
        TimeOfDay
    };
}
