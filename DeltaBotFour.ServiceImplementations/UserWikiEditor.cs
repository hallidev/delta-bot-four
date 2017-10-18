using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DeltaBotFour.ServiceImplementations
{
    public class UserWikiEditor : IUserWikiEditor
    {
        private string _userWikiTemplate;
        private string _userWikiRowTemplate;

        private readonly AppConfiguration _appConfiguration;
        private readonly Reddit _reddit;
        private readonly Subreddit _subreddit;

        public UserWikiEditor(AppConfiguration appConfiguration, Reddit reddit, Subreddit subreddit)
        {
            _appConfiguration = appConfiguration;
            _reddit = reddit;
            _subreddit = subreddit;
        }

        public void UpdateUserWikiEntryAward(Comment comment, Comment parentComment)
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

            // Get content for the user giving
            string givingUserPageContent = buildUserPageContent(givingUserUrl, comment.AuthorName, parentComment.AuthorName, comment, true);

            // Get content for the user receiving
            string receivngUserPageContent = buildUserPageContent(receivingUserUrl, parentComment.AuthorName, comment.AuthorName, comment, false);

            // Update content
            _subreddit.GetWiki.EditPageAsync(givingUserUrl, givingUserPageContent);
            _subreddit.GetWiki.EditPageAsync(receivingUserUrl, receivngUserPageContent);
        }

        public void UpdateUserWikiEntryUnaward(Comment comment, Comment parentComment)
        {
            
        }

        private string buildUserPageContent(string userUrl, string username, string toUsername, Comment commentToBuildLinkFor, bool giving)
        {
            // Get page content
            var wiki = _subreddit.GetWiki;
            var userPage = wiki.GetPageAsync(userUrl).Result;

            // Find and deserialize hidden params
            var hiddenParamsMatch = _appConfiguration.HiddenParamsRegex.Match(userPage.MarkdownContent);

            UserWikiHiddenParams wikiHiddenParams = null;

            // If a hidden params section wasn't found, make one
            // Note: the second group is actually the hidden params, so count == 1 means it wasn't found
            if(hiddenParamsMatch.Groups.Count == 1)
            {
                var givenLinks = _appConfiguration.GetWikiLinkRegex(_appConfiguration.SubredditName, "3").Matches(userPage.MarkdownContent); // 3 = Given
                var receivedLinks = _appConfiguration.GetWikiLinkRegex(_appConfiguration.SubredditName, "2").Matches(userPage.MarkdownContent); // 2 = Received

                // Get fullnames of given links
                List<string> givenLinkFullnames = new List<string>();
                foreach(Match match in givenLinks)
                {
                    givenLinkFullnames.Add("t1_" + match.Value.Substring(match.Value.LastIndexOf('/') + 1, (match.Value.Length - match.Value.LastIndexOf('/')) - 1).Replace("?context=3", string.Empty));
                }

                // Get fullnames of received links
                List<string> receivedLinkFullnames = new List<string>();
                foreach (Match match in receivedLinks)
                {
                    receivedLinkFullnames.Add("t1_" + match.Value.Substring(match.Value.LastIndexOf('/') + 1, (match.Value.Length - match.Value.LastIndexOf('/')) - 1).Replace("?context=2", string.Empty));
                }

                // Create a DeltaGiven / DeltaReceived for each link
                List<UserWikiDeltaInfo> deltasGiven = getWikiDeltaInfoFromFullnames(givenLinkFullnames, toUsername);
                List<UserWikiDeltaInfo> deltasReceived = getWikiDeltaInfoFromFullnames(receivedLinkFullnames, toUsername);

                wikiHiddenParams = new UserWikiHiddenParams
                {
                    Comment = _appConfiguration.DefaultHiddenParamsComment,
                    DeltasGiven = deltasGiven,
                    DeltasReceived = deltasReceived
                };
            }
            else
            {
                // Take hidden params from hidden params section
                wikiHiddenParams = JsonConvert.DeserializeObject<UserWikiHiddenParams>(hiddenParamsMatch.Groups[1].Value);
            }
            
            if(wikiHiddenParams.DeltasGiven == null) { wikiHiddenParams.DeltasGiven = new List<UserWikiDeltaInfo>(); }
            if (wikiHiddenParams.DeltasReceived == null) { wikiHiddenParams.DeltasReceived = new List<UserWikiDeltaInfo>(); }

            // Add new info to hidden params
            if(giving)
            {
                wikiHiddenParams.DeltasGiven.Add(getUserWikiDeltaInfo(commentToBuildLinkFor, (Post)commentToBuildLinkFor.Parent, toUsername));
            }
            else
            {
                wikiHiddenParams.DeltasReceived.Add(getUserWikiDeltaInfo(commentToBuildLinkFor, (Post)commentToBuildLinkFor.Parent, toUsername));
            }

            // Reconstruct content based on hidden params
            string updatedContent = _userWikiTemplate;

            // Replace the hidden params token with the new serialized hidden params
            updatedContent = updatedContent
                .Replace(_appConfiguration.ReplaceTokens.HiddenParamsToken, JsonConvert.SerializeObject(wikiHiddenParams, Formatting.Indented))
                .Replace(_appConfiguration.ReplaceTokens.UsernameToken, username)
                .Replace(_appConfiguration.ReplaceTokens.DeltasGivenCountToken, wikiHiddenParams.DeltasGiven.Count.ToString())
                .Replace(_appConfiguration.ReplaceTokens.DeltasReceivedCountToken, wikiHiddenParams.DeltasReceived.Count.ToString());

            // Update rows
            string givingRowsContent = getRowsContent(wikiHiddenParams.DeltasGiven, toUsername, "3");
            string receivingRowsContent = getRowsContent(wikiHiddenParams.DeltasReceived, toUsername, "2");

            updatedContent = updatedContent
                .Replace(_appConfiguration.ReplaceTokens.WikiRowsGivenToken, givingRowsContent)
                .Replace(_appConfiguration.ReplaceTokens.WikiRowsReceivedToken, receivingRowsContent);

            return updatedContent;
        }

        private string getRowsContent(List<UserWikiDeltaInfo> deltaInfos, string toUsername, string contextNumber)
        {
            string rowsContent = string.Empty;

            foreach (var deltaInfo in deltaInfos)
            {
                string rowContent = _userWikiRowTemplate
                    .Replace(_appConfiguration.ReplaceTokens.DateMMDYYYY, DateTimeOffset.FromUnixTimeSeconds(long.Parse(deltaInfo.CreatedUTC)).ToString("M/d/yyyy"))
                    .Replace(_appConfiguration.ReplaceTokens.PostTitle, deltaInfo.PostTitle)
                    .Replace(_appConfiguration.ReplaceTokens.PostLink, deltaInfo.PostLink)
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, $"{deltaInfo.PostLink}{deltaInfo.CommentId}?context={contextNumber}")
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, $"/u/{toUsername}");

                rowsContent = $"{rowsContent}{rowContent}\r\n";
            }

            return rowsContent;
        }

        private List<UserWikiDeltaInfo> getWikiDeltaInfoFromFullnames(List<string> fullnames, string toUsername)
        {
            List<UserWikiDeltaInfo> deltaInfos = new List<UserWikiDeltaInfo>();

            foreach (string fullname in fullnames)
            {
                Comment unqualifiedComment = (Comment)_reddit.GetThingByFullnameAsync(fullname).Result;
                Post parentPost = (Post)_reddit.GetThingByFullnameAsync(unqualifiedComment.LinkId).Result;
                deltaInfos.Add(getUserWikiDeltaInfo(unqualifiedComment, parentPost, toUsername));
            }

            return deltaInfos;
        }

        private UserWikiDeltaInfo getUserWikiDeltaInfo(Comment comment, Post parentPost, string toUsername)
        {
            string postLink = $"{_appConfiguration.RedditBaseUrl}{parentPost.Permalink.OriginalString}";
            string postTitle = parentPost.Title;
            string createdUTC = new DateTimeOffset(comment.CreatedUTC).ToUnixTimeSeconds().ToString();

            // Create new hidden param entry from comment
            return new UserWikiDeltaInfo
            {
                PostLink = postLink, PostTitle = postTitle, CommentId = comment.Id, Username = toUsername, CreatedUTC = createdUTC
            };
        }

        private string getUserWikiUrl(string username)
        {
            return _appConfiguration.WikiUrlUser.Replace(_appConfiguration.ReplaceTokens.UsernameToken, username);
        }
    }
}
