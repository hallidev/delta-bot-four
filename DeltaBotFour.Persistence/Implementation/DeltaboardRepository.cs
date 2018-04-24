using System.Collections.Generic;
using System.Linq;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using LiteDB;

namespace DeltaBotFour.Persistence.Implementation
{
    public class DeltaboardRepository : IDeltaboardRepository
    {
        private const string DbFileName = "Deltaboards.db";

        public List<Deltaboard> GetCurrentDeltaboards()
        {
            using (var db = new LiteDatabase(DbFileName))
            {
                var deltaboards = db.GetCollection<Deltaboard>();

                return deltaboards.FindAll().ToList();
            }
        }

        public List<DeltaboardEntry> GetCurrentEntriesForUser(string username)
        {
            return new List<DeltaboardEntry>();
        }
    }
}
