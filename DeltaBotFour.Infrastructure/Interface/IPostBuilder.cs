using System.Collections.Generic;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IPostBuilder
    {
        (string, string) BuildDeltaLogPost(string mainPostTitle, string mainPostPermalink, string opUsername, List<DeltaComment> deltaComments);
    }
}
