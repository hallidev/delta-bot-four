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

        public string DB4Username => _configuration["db4_username"];
        public string DB4Password => _configuration["db4_password"];
        public string DB4ClientId => _configuration["db4_client_id"];
        public string DB4ClientSecret => _configuration["db4_client_secret"];
        public string SubredditName => _configuration["subreddit_name"];
        public List<string> ValidDeltaIndicators => _configuration["valid_delta_indicators"].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        public AppConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();
            DeltaBotReplies.Initialize(_configuration);
        }

        public static class DeltaBotReplies
        {
            private static IConfigurationRoot _configuration;

            public static string DeltaAwarded => _configuration["validation_replies:delta_awarded"];
            public static string CommentTooShort => _configuration["validation_replies:comment_too_short"];
            public static string CannotAwardOP => _configuration["validation_replies:cannot_award_op"];
            public static string CannotAwardDeltaBot => _configuration["validation_replies:cannot_award_deltabot"];
            public static string CannotAwardSelf => _configuration["validation_replies:cannot_award_self"];
            public static string WithIssues => _configuration["validation_replies:fail_with_issues"];
            public static string Rejected => _configuration["validation_replies:rejected"];

            public static void Initialize(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }
    }
}
