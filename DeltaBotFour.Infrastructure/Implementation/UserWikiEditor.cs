using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using Newtonsoft.Json;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class UserWikiEditor : IUserWikiEditor
    {
        private const string DB3SpaceToken = "-s---"; // Not sure where this comes from in DB3
        private const string ParenOpenToken = "ZZDK9vhFALCkjXPmwvSB"; // DB3 didn't do open parens, but being safe
        private const string ParenCloseToken = "AXDK9vhFALCkjXPmwvSB"; // Taken from DB3 - this is the token it used
        private const string GiveEditReason = "Added a delta given";
        private const string ReceiveEditReason = "Added a delta received";

        private string _userWikiTemplate;
        private string _userWikiRowTemplate;

        private readonly AppConfiguration _appConfiguration;
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;

        public UserWikiEditor(AppConfiguration appConfiguration, IRedditService redditService, ISubredditService subredditService)
        {
            _appConfiguration = appConfiguration;
            _redditService = redditService;
            _subredditService = subredditService;
        }

        public int GetCurrentDeltaCount(string userName)
        {
            string userUrl = getUserWikiUrl(userName);
            var wikiHiddenParams = getHiddenParams(userUrl, userName);
            return wikiHiddenParams.DeltasReceived.Count;
        }

        public int UpdateUserWikiEntryAward(DB4Thing comment)
        {
            return performWikiPageUpdate(comment, true);
        }

        public int UpdateUserWikiEntryUnaward(DB4Thing comment)
        {
            return performWikiPageUpdate(comment, false);
        }

        private int performWikiPageUpdate(DB4Thing comment, bool isAward)
        {
            if (string.IsNullOrEmpty(_userWikiTemplate))
            {
                _userWikiTemplate = File.ReadAllText(_appConfiguration.TemplateFiles.UserWikiTemplateFile);
            }

            if (string.IsNullOrEmpty(_userWikiRowTemplate))
            {
                _userWikiRowTemplate = File.ReadAllText(_appConfiguration.TemplateFiles.UserWikiRowTemplateFile);
            }

            string givingUserUrl = getUserWikiUrl(comment.AuthorName);
            string receivingUserUrl = getUserWikiUrl(comment.ParentThing.AuthorName);

            // Get content for the user giving
            var givingInfo = buildUserPageContent(givingUserUrl, comment.AuthorName, comment.ParentThing.AuthorName, comment, true, isAward);

            // Get content for the user receiving
            var receivngInfo = buildUserPageContent(receivingUserUrl, comment.ParentThing.AuthorName, comment.AuthorName, comment, false, isAward);

            // Update content
            _subredditService.EditWikiPage(givingUserUrl, givingInfo.Item1, GiveEditReason);
            _subredditService.EditWikiPage(receivingUserUrl, receivngInfo.Item1, ReceiveEditReason);

            // We only care about the receiving count for updated flair
            return receivngInfo.Item2;
        }

        private (string, int) buildUserPageContent(string userUrl, string username, string toUsername, DB4Thing commentToBuildLinkFor, bool giving, bool isAward)
        {
            // Load hidden params from the wiki page. This will create hiddenparams for a new page
            var wikiHiddenParams = getHiddenParams(userUrl, toUsername);

            if (wikiHiddenParams.DeltasGiven == null) { wikiHiddenParams.DeltasGiven = new List<UserWikiDeltaInfo>(); }
            if (wikiHiddenParams.DeltasReceived == null) { wikiHiddenParams.DeltasReceived = new List<UserWikiDeltaInfo>(); }

            // Add new info to hidden params
            if(giving)
            {
                if (isAward)
                {
                    // Award delta given
                    wikiHiddenParams.DeltasGiven.Add(getUserWikiDeltaInfo(commentToBuildLinkFor, commentToBuildLinkFor.ParentPost, toUsername));
                }
                else
                {
                    // Unaward delta given
                    UserWikiDeltaInfo deltaInfo = wikiHiddenParams.DeltasGiven.First(d => d.CommentId == commentToBuildLinkFor.Id);
                    wikiHiddenParams.DeltasGiven.Remove(deltaInfo);
                }
            }
            else
            {
                if (isAward)
                {
                    // Award delta received
                    wikiHiddenParams.DeltasReceived.Add(getUserWikiDeltaInfo(commentToBuildLinkFor, commentToBuildLinkFor.ParentPost, toUsername));
                }
                else
                {
                    // Unaward delta received
                    UserWikiDeltaInfo deltaInfo = wikiHiddenParams.DeltasReceived.First(d => d.CommentId == commentToBuildLinkFor.Id);
                    wikiHiddenParams.DeltasReceived.Remove(deltaInfo);
                }
            }

            // Reconstruct content based on hidden params
            string updatedContent = _userWikiTemplate;

            // Replace the hidden params token with the new serialized hidden params
            string hiddenParamsJson = JsonConvert.SerializeObject(wikiHiddenParams, Formatting.Indented)
                .Replace("(", ParenOpenToken)
                .Replace(")", ParenCloseToken);

            updatedContent = updatedContent
                .Replace(_appConfiguration.ReplaceTokens.HiddenParamsToken, hiddenParamsJson)
                .Replace(_appConfiguration.ReplaceTokens.UsernameToken, username)
                .Replace(_appConfiguration.ReplaceTokens.DeltasGivenCountToken, wikiHiddenParams.DeltasGiven.Count.ToString())
                .Replace(_appConfiguration.ReplaceTokens.DeltasReceivedCountToken, wikiHiddenParams.DeltasReceived.Count.ToString());

            // Update rows
            string givingRowsContent = getRowsContent(wikiHiddenParams.DeltasGiven, "3");
            string receivingRowsContent = getRowsContent(wikiHiddenParams.DeltasReceived, "2");

            updatedContent = updatedContent
                .Replace(_appConfiguration.ReplaceTokens.WikiRowsGivenToken, givingRowsContent)
                .Replace(_appConfiguration.ReplaceTokens.WikiRowsReceivedToken, receivingRowsContent);

            return (updatedContent, wikiHiddenParams.DeltasReceived.Count);
        }

        private UserWikiHiddenParams getHiddenParams(string userUrl, string userName)
        {
            // Get page content
            string pageContent = _subredditService.GetWikiPage(userUrl);

            // If the page wasn't found, consider it empty
            if (string.IsNullOrEmpty(pageContent))
            {
                pageContent = string.Empty;
            }
            else
            {
                // DB3 has many wiki entries with this space token in it. It's not needed here so must be replaced
                pageContent = pageContent
                    .Replace(DB3SpaceToken, " ")
                    .Replace(ParenOpenToken, "(")
                    .Replace(ParenCloseToken, ")");
            }

            // Find and deserialize hidden params
            var hiddenParamsMatch = _appConfiguration.HiddenParamsRegex.Match(pageContent);

            UserWikiHiddenParams wikiHiddenParams;

            // If a hidden params section wasn't found, make one
            // Note: the second group is actually the hidden params, so count == 1 means it wasn't found
            if (hiddenParamsMatch.Groups.Count == 1)
            {
                var givenLinks = _appConfiguration.GetWikiLinkRegex(_appConfiguration.SubredditName, "3").Matches(pageContent); // 3 = Given
                var receivedLinks = _appConfiguration.GetWikiLinkRegex(_appConfiguration.SubredditName, "2").Matches(pageContent); // 2 = Received

                // Get fullnames of given links
                List<string> givenLinkFullnames = new List<string>();
                foreach (Match match in givenLinks)
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
                List<UserWikiDeltaInfo> deltasGiven = getWikiDeltaInfoFromFullnames(givenLinkFullnames, userName);
                List<UserWikiDeltaInfo> deltasReceived = getWikiDeltaInfoFromFullnames(receivedLinkFullnames, userName);

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

            return wikiHiddenParams;
        }

        private string getRowsContent(List<UserWikiDeltaInfo> deltaInfos, string contextNumber)
        {
            // Convert hidden params into rows of text
            string rowsContent = string.Empty;

            foreach (var deltaInfo in deltaInfos)
            {
                string rowContent = _userWikiRowTemplate
                    .Replace(_appConfiguration.ReplaceTokens.DateYYYYMMDD, DateTimeOffset.FromUnixTimeSeconds(long.Parse(deltaInfo.CreatedUTC)).ToString("yyyy/MM/dd"))
                    .Replace(_appConfiguration.ReplaceTokens.PostTitle, deltaInfo.PostTitle)
                    .Replace(_appConfiguration.ReplaceTokens.PostLink, deltaInfo.PostLink)
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, $"{deltaInfo.PostLink}{deltaInfo.CommentId}?context={contextNumber}")
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, $"/u/{deltaInfo.Username}");

                rowsContent = $"{rowsContent}{rowContent}\r\n";
            }

            return rowsContent;
        }

        private List<UserWikiDeltaInfo> getWikiDeltaInfoFromFullnames(List<string> fullnames, string toUsername)
        {
            List<UserWikiDeltaInfo> deltaInfos = new List<UserWikiDeltaInfo>();

            foreach (string fullname in fullnames)
            {
                // The /r/subreddit/api/info call isn't wrapped by RedditSharp. This gets us the info
                // we need but is chatty. This is a rare edge case anyhow (where the HiddenParams don't exist)
                var unqualifiedComment = _redditService.GetThingByFullname(fullname);
                var parentPost = _redditService.GetThingByFullname(unqualifiedComment.LinkId);
                deltaInfos.Add(getUserWikiDeltaInfo(unqualifiedComment, parentPost, toUsername));
            }

            return deltaInfos;
        }

        private UserWikiDeltaInfo getUserWikiDeltaInfo(DB4Thing comment, DB4Thing parentPost, string toUsername)
        {
            string postLink = $"{_appConfiguration.RedditBaseUrl}{parentPost.Permalink}";
            string postTitle = WebUtility.HtmlDecode(parentPost.Title);
            string createdUTC = new DateTimeOffset(comment.CreatedUtc).ToUnixTimeSeconds().ToString();

            // Create new hidden param entry from comment
            return new UserWikiDeltaInfo
            {
                PostLink = postLink,
                PostTitle = postTitle,
                CommentId = comment.Id,
                Username = toUsername,
                CreatedUTC = createdUTC
            };
        }

        private string getUserWikiUrl(string username)
        {
            return _appConfiguration.WikiUrlUser.Replace(_appConfiguration.ReplaceTokens.UsernameToken, username);
        }
    }
}
