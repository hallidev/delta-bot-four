using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class AppConfiguration
    {
        private IConfigurationRoot _configuration;

        public string DB4Username => _configuration["DB4Username"];
        public string DB4Password => _configuration["DB4Password"];
        public string DB4ClientId => _configuration["DB4ClientId"];
        public string DB4ClientSecret => _configuration["DB4ClientSecret"];
        public string SubredditName => _configuration["SubredditName"];
        public List<string> ValidDeltaIndicators => _configuration["ValidDeltaIndicators"].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        public AppConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();
        }
    }
}
