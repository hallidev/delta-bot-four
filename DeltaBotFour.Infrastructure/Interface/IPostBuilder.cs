using System.Collections.Generic;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IPostBuilder
    {
        string BuildDeltaLogPost(List<DeltaComment> deltaComments);
    }
}
