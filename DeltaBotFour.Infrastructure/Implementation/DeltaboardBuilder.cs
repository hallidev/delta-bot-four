using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaboardBuilder : IDeltaboardBuilder
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IWikiEditor _wikiEditor;

        public DeltaboardBuilder(AppConfiguration appConfiguration, IWikiEditor wikiEditor)
        {
            _appConfiguration = appConfiguration;
            _wikiEditor = wikiEditor;
        }

        public void Build(DeltaboardType type)
        {
            //[​] (HTTP://DB3PARAMSSTART
            //{
            //    "daily": [],
            //    "weekly": [],
            //    "monthly": [
            //    {
            //        "username": "MystK",
            //        "deltaCount": 1,
            //        "newestDeltaTime": 1489379983
            //    }
            //    ],
            //    "yearly": [],
            //    "updateTimes": {
            //        "yearly": "As of 10/21/17 04:24 EDT",
            //        "monthly": "As of 3/1/18 21:41 Pacific Standard Time",
            //        "weekly": "As of 3/1/18 21:41 Pacific Standard Time",
            //        "daily": "As of 3/1/18 21:41 Pacific Standard Time"
            //    }
            //}
            //DB3PARAMSEND)


            string deltaboards = _wikiEditor.GetPage(_appConfiguration.WikiUrlDeltaboards);
            ConsoleHelper.WriteLine($"DeltaBot built the Deltaboard -> '{type}'", ConsoleColor.Green);
        }
    }
}
