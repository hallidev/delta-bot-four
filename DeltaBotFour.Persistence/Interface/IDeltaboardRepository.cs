using System.Collections.Generic;
using DeltaBotFour.Models;

namespace DeltaBotFour.Persistence.Interface
{
    public interface IDeltaboardRepository
    {
        List<Deltaboard> GetCurrentDeltaboards();
        List<DeltaboardEntry> GetCurrentEntriesForUser(string username);
    }
}
