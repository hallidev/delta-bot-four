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
        public DeltaBotReplies Replies { get; private set; }
        public string GlobalFooter => _configuration["global_footer"];

        public AppConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();
            Replies = new DeltaBotReplies();
            Replies.Initialize(_configuration);
        }

        public class DeltaBotReplies
        {
            private IConfigurationRoot _configuration;

            public string DeltaAwarded => _configuration["validation_replies:delta_awarded"];
            public string CommentTooShort => _configuration["validation_replies:comment_too_short"];
            public string CannotAwardOP => _configuration["validation_replies:cannot_award_op"];
            public string CannotAwardDeltaBot => _configuration["validation_replies:cannot_award_deltabot"];
            public string CannotAwardSelf => _configuration["validation_replies:cannot_award_self"];
            public string WithIssues => _configuration["validation_replies:fail_with_issues"];
            public string Rejected => _configuration["validation_replies:rejected"];

            public void Initialize(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }
    }
}