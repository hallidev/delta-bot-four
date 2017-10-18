using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using Newtonsoft.Json;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DeltaBotFour.ServiceImplementations
{
    public class UserWikiEditor : IUserWikiEditor
    {
        private string _userWikiTemplate;
        private string _userWikiRowTemplate;

        private readonly AppConfiguration _appConfiguration;
        private readonly Subreddit _subreddit;

        public UserWikiEditor(AppConfiguration appConfiguration, Subreddit subreddit)
        {
            _appConfiguration = appConfiguration;
            _subreddit = subreddit;
        }

        public async void UpdateUserWikiEntryAward(Comment comment, Comment parentComment)
        {
            if(string.IsNullOrEmpty(_userWikiTemplate))
            {
                _userWikiTemplate = File.ReadAllText(_appConfiguration.TemplateFiles.UserWikiTemplateFile);
            }

            if (string.IsNullOrEmpty(_userWikiRowTemplate))
            {
                _userWikiRowTemplate = File.ReadAllText(_appConfiguration.TemplateFiles.UserWikiRowTemplateFile);
            }

            string givingUserUrl = getUserWikiUrl(comment.AuthorName);
            string receivingUserUrl = getUserWikiUrl(parentComment.AuthorName);

            var givingUserPage = await buildUserPageContent(givingUserUrl, comment, parentComment.AuthorName, true);
            var receivngUserPage = await buildUserPageContent(receivingUserUrl, comment, parentComment.AuthorName, false);
        }

        public void UpdateUserWikiEntryUnaward(Comment comment, Comment parentComment)
        {
            
        }

        private async Task<string> buildUserPageContent(string userUrl, Comment comment, string awardeeUser, bool giving)
        {
            // Get page content
            var wiki = _subreddit.GetWiki;
            var userPage = await wiki.GetPageAsync(userUrl);

            // Find and deserialize hidden params
            var hiddenParamsMatch = _appConfiguration.HiddenParamsRegex.Match(userPage.MarkdownContent);

            // TODO: Handle no match
            var wikiHiddenParams = JsonConvert.DeserializeObject<UserWikiHiddenParams>(hiddenParamsMatch.Groups[1].Value);
            
            if(wikiHiddenParams.DeltasGiven == null) { wikiHiddenParams.DeltasGiven = new List<UserWikiDeltaInfo>(); }
            if (wikiHiddenParams.DeltasReceived == null) { wikiHiddenParams.DeltasReceived = new List<UserWikiDeltaInfo>(); }

            string postLink = $"{_appConfiguration.RedditBaseUrl}{((Post)comment.Parent).Permalink.OriginalString}";
            string postTitle = ((Post)comment.Parent).Title;
            string createdUTC = new DateTimeOffset(comment.CreatedUTC).ToUnixTimeSeconds().ToString();

            // Create new hidden param entry from comment
            var newDeltaInfo = new UserWikiDeltaInfo { PostLink = postLink, PostTitle = postTitle, ThingShortId = comment.Id, Username = awardeeUser, CreatedUTC = createdUTC };

            // Add new info to hidden params
            if(giving)
            {
                wikiHiddenParams.DeltasGiven.Add(newDeltaInfo);
            }
            else
            {
                wikiHiddenParams.DeltasReceived.Add(newDeltaInfo);
            }

            // Reconstruct content based on hidden params
            string updatedContent = _userWikiTemplate;

            // Replace the hidden params token with the new serialized hidden params
            updatedContent = updatedContent.Replace(_appConfiguration.ReplaceTokens.HiddenParamsToken, JsonConvert.SerializeObject(wikiHiddenParams, Formatting.Indented));

            // Update rows
            // TODO: rows
            // https://www.reddit.com/r/changemyviewDB3Dev2/comments/75crhd/cms_deltas_given_test_2/do5aux6?context=2
            //string commentLink = $"{postLink}{}"

            return updatedContent;
        }

        private string getUserWikiUrl(string username)
        {
            return _appConfiguration.WikiUrlUser.Replace(_appConfiguration.ReplaceTokens.UsernameToken, username);
        }
    }
}
