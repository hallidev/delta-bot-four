using System.ComponentModel;

namespace DeltaBotFour.Models
{
    public enum DeltaboardType
    {
        Daily,
        Weekly,
        Monthly,
        Yearly,
        [Description("All Time")]
        AllTime
    }
}
