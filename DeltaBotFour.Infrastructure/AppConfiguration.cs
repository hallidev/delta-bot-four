using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DeltaBotFour.Models;
using Microsoft.Extensions.Configuration;

namespace DeltaBotFour.Infrastructure
{
    public class AppConfiguration
    {
        private readonly IConfigurationRoot _configuration;

        public string DB4Username => _configuration["db4_username"];
        public string DB4Password => _configuration["db4_password"];
        public string DB4ClientId => _configuration["db4_client_id"];
        public string DB4ClientSecret => _configuration["db4_client_secret"];
        public string RedditBaseUrl => _configuration["reddit_base_url"];
        public string SubredditName => _configuration["subreddit_name"];
        public string DeltaLogSubredditName => _configuration["deltalog_subreddit_name"];
        public List<string> ValidWATTUsers => _configuration["valid_watt_users"].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public List<string> ValidDeltaIndicators => _configuration["valid_delta_indicators"].Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        public int HoursToUnawardDelta => int.Parse(_configuration["hours_to_unaward_delta"]);
        public string WikiUrlDeltaboards => _configuration["wiki_url_deltaboards"];
        public string WikiUrlUser => _configuration["wiki_url_user"];
        public Regex DeltaboardSidebarRegex => new Regex(_configuration["deltaboard_sidebar_regex"], RegexOptions.Singleline);
        public Regex HiddenParamsRegex => new Regex(_configuration["hidden_params_regex"], RegexOptions.Singleline);
        public string DefaultHiddenParamsComment => _configuration["default_hidden_params_comment"];
        public Regex GetWikiLinkRegex(string subredditName, string contextNumber)
        { 
            return new Regex(_configuration["wiki_link_regex"]
                .Replace(ReplaceTokens.SubredditToken, subredditName)
                .Replace(ReplaceTokens.ContextNumberToken, contextNumber));
        }
        public DeltaBotTemplateFiles TemplateFiles { get; }
        public DeltaBotReplaceTokens ReplaceTokens { get; }
        public DeltaBotPrivateMessages PrivateMessages { get; }
        public DeltaBotPosts Posts { get; }
        public DeltaBotComments Comments { get; }
        public DeltaBotValidationValues ValidationValues { get; }

        public AppConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();

            TemplateFiles = new DeltaBotTemplateFiles(_configuration);

            ReplaceTokens = new DeltaBotReplaceTokens(_configuration);

            PrivateMessages = new DeltaBotPrivateMessages(_configuration);

            Posts = new DeltaBotPosts(_configuration);

            Comments = new DeltaBotComments(_configuration);

            ValidationValues = new DeltaBotValidationValues(_configuration);
        }

        public class DeltaBotTemplateFiles
        {
            private readonly IConfigurationRoot _configuration;

            public string DB4CommentTemplateFile => _configuration["template_files:db4_comment_template_file"];
            public string DeltaboardsTemplateFile => _configuration["template_files:deltaboards_template_file"];
            public string DeltaboardTemplateFile => _configuration["template_files:deltaboard_template_file"];
            public string DeltaboardRowTemplateFile => _configuration["template_files:deltaboard_row_template_file"];
            public string DeltaboardSidebarTemplateFile => _configuration["template_files:deltaboard_sidebar_template_file"];
            public string UserWikiTemplateFile => _configuration["template_files:user_wiki_template_file"];
            public string UserWikiRowTemplateFile => _configuration["template_files:user_wiki_row_template_file"];

            public DeltaBotTemplateFiles(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }

        public class DeltaBotReplaceTokens
        {
            private readonly IConfigurationRoot _configuration;

            public string SubredditToken => _configuration["replace_tokens:subreddit_token"];
            public string DeltaLogSubredditToken => _configuration["replace_tokens:deltalog_subreddit_token"];
            public string DateToken => _configuration["replace_tokens:date_token"];
            public string UsernameToken => _configuration["replace_tokens:username_token"];
            public string UserWikiLinkToken => _configuration["replace_tokens:user_wiki_link_token"];
            public string HiddenParamsToken => _configuration["replace_tokens:hidden_params_token"];
            public string DailyDeltaboardToken => _configuration["replace_tokens:daily_deltaboard_token"];
            public string WeeklyDeltaboardToken => _configuration["replace_tokens:weekly_deltaboard_token"];
            public string MonthlyDeltaboardToken => _configuration["replace_tokens:monthly_deltaboard_token"];
            public string YearlyDeltaboardToken => _configuration["replace_tokens:yearly_deltaboard_token"];
            public string AllTimeDeltaboardToken => _configuration["replace_tokens:all_time_deltaboard_token"];
            public string DeltaboardTypeToken => _configuration["replace_tokens:deltaboard_type_token"];
            public string DeltaboardRowsToken => _configuration["replace_tokens:deltaboard_rows_token"];
            public string RankToken => _configuration["replace_tokens:rank_token"];
            public string CountToken => _configuration["replace_tokens:count_token"];
            public string DeltasGivenCountToken => _configuration["replace_tokens:deltas_given_count_token"];
            public string DeltasReceivedCountToken => _configuration["replace_tokens:deltas_received_count_token"];
            public string WikiRowsGivenToken => _configuration["replace_tokens:wiki_rows_given_token"];
            public string WikiRowsReceivedToken => _configuration["replace_tokens:wiki_rows_received_token"];
            public string DateMMDYYYY => _configuration["replace_tokens:date_mmdyyyy"];
            public string PostTitle => _configuration["replace_tokens:post_title"];
            public string PostLink => _configuration["replace_tokens:post_link"];
            public string CommentLink => _configuration["replace_tokens:comment_link"];
            public string ParentAuthorNameToken => _configuration["replace_tokens:parent_author_name_token"];
            public string DeltasToken => _configuration["replace_tokens:deltas_token"];
            public string DB4ReplyToken => _configuration["replace_tokens:db4_reply_token"];
            public string ContextNumberToken => _configuration["replace_tokens:context_number_token"];
            public string WATTLinkToken => _configuration["replace_tokens:watt_link_token"];

            public DeltaBotReplaceTokens(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }

        public class DeltaBotPrivateMessages
        {
            private readonly IConfigurationRoot _configuration;

            public string WATTArticleCreatedSubject => _configuration["private_messages:watt_article_created_subject"];
            public string ModAddDeltaSubject => _configuration["private_messages:mod_add_delta_subject"];
            public string ModDeleteDeltaSubject => _configuration["private_messages:mod_delete_delta_subject"];
            public string ModAddedDeltaNotificationSubject => _configuration["private_messages:mod_added_delta_notification_subject"];
            public string ModAddedDeltaNotificationMessage => _configuration["private_messages:mod_added_delta_notification_message"];
            public string ModDeletedDeltaNotificationSubject => _configuration["private_messages:mod_deleted_delta_notification_subject"];
            public string ModDeletedDeltaNotificationMessage => _configuration["private_messages:mod_deleted_delta_notification_message"];
            public string FirstDeltaSubject => _configuration["private_messages:first_delta_subject"];
            public string FirstDeltaMessage => _configuration["private_messages:first_delta_message"];
            public string DeltaInQuoteSubject => _configuration["private_messages:delta_in_quote_subject"];
            public string DeltaInQuoteMessage => _configuration["private_messages:delta_in_quote_message"];
            public string ConfirmStopQuotedDeltaWarningMessage => _configuration["private_messages:confirm_stop_quoted_delta_warning_message"];

            public DeltaBotPrivateMessages(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }

        public class DeltaBotPosts
        {
            private readonly IConfigurationRoot _configuration;

            public string DeltaLogTitle => _configuration["posts:delta_log_title"];
            public string DeltaLogContent => _configuration["posts:delta_log_content"];

            public DeltaBotPosts(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }

        public class DeltaBotComments
        {
            private readonly IConfigurationRoot _configuration;

            public string PostStickyDeltas => _configuration["comments:post_sticky_deltas"];
            public string PostStickyWATT => _configuration["comments:post_sticky_watt"];
            public string DeltaAwarded => _configuration["comments:delta_awarded"];
            public string CommentTooShort => _configuration["comments:comment_too_short"];
            public string CannotAwardOP => _configuration["comments:cannot_award_op"];
            public string CannotAwardDeltaBot => _configuration["comments:cannot_award_deltabot"];
            public string CannotAwardSelf => _configuration["comments:cannot_award_self"];
            public string ModeratorAdded => _configuration["comments:moderator_added"];
            public string ModeratorRemoved => _configuration["comments:moderator_removed"];
            public List<Tuple<string, DB4CommentType>> AllComments { get; }

            public DeltaBotComments(IConfigurationRoot configuration)
            {
                _configuration = configuration;

                AllComments = new List<Tuple<string, DB4CommentType>>
                {
                    new Tuple<string, DB4CommentType>(PostStickyDeltas, DB4CommentType.PostSticky),
                    new Tuple<string, DB4CommentType>(DeltaAwarded, DB4CommentType.SuccessDeltaAwarded),
                    new Tuple<string, DB4CommentType>(CommentTooShort, DB4CommentType.FailCommentTooShort),
                    new Tuple<string, DB4CommentType>(CannotAwardOP, DB4CommentType.FailCannotAwardOP),
                    new Tuple<string, DB4CommentType>(CannotAwardDeltaBot, DB4CommentType.FailCannotAwardDeltaBot),
                    new Tuple<string, DB4CommentType>(CannotAwardSelf, DB4CommentType.FailCannotAwardSelf),
                    new Tuple<string, DB4CommentType>(ModeratorAdded, DB4CommentType.ModeratorAdded),
                    new Tuple<string, DB4CommentType>(ModeratorRemoved, DB4CommentType.ModeratorRemoved)
                };
            }
        }

        public class DeltaBotValidationValues
        {
            private readonly IConfigurationRoot _configuration;

            public int CommentTooShortLength => int.Parse(_configuration["validation_values:comment_too_short_length"]);

            public DeltaBotValidationValues(IConfigurationRoot configuration)
            {
                _configuration = configuration;
            }
        }
    }
}