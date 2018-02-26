using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;

namespace DeltaBotFour.ServiceImplementations
{
    public class DeltaboardBuilder : IDeltaboardBuilder
    {
        public void Build(DeltaboardType type)
        {
            ConsoleHelper.WriteLine($"DeltaBot built the Deltaboard -> '{type}'", ConsoleColor.Green);
        }
    }
}
