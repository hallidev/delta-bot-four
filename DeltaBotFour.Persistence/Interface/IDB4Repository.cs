using System;
using System.Collections.Generic;
using DeltaBotFour.Models;

namespace DeltaBotFour.Persistence.Interface
{
    public interface IDB4Repository
    {
        DateTime GetLastProcessedCommentTimeUtc();
        void SetLastProcessedCommentTimeUtc();
        List<Deltaboard> GetCurrentDeltaboards();
        void AddDeltaboardEntry(string username);
        void RemoveDeltaboardEntry(string username);
    }
}
