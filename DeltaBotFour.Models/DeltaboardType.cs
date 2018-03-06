using System;

namespace DeltaBotFour.Models
{
    [Flags]
    public enum DeltaboardType
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Yearly = 4,
        AllTime = 8
    }
}
