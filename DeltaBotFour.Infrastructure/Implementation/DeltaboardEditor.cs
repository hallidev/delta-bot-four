using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Foundation.Extensions;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Logging;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaboardEditor : IDeltaboardEditor
    {
        private const int RanksToShow = 10;
        private const string UpdateDeltaboarsdReason = "Update deltaboards";

        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;
        private readonly ISubredditService _subredditService;
        private readonly ILogger _logger;
        private readonly string _deltaboardsTemplate;
        private readonly string _deltaboardTemplate;
        private readonly string _deltaboardRowTemplate;
        private readonly string _deltaboardSidebarTemplate;

        public DeltaboardEditor(AppConfiguration appConfiguration, 
            IDB4Repository db4Repository,
            ISubredditService subredditService,
            ILogger logger)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
            _subredditService = subredditService;
            _logger = logger;

            _deltaboardsTemplate = File.ReadAllText(appConfiguration.TemplateFiles.DeltaboardsTemplateFile);
            _deltaboardTemplate = File.ReadAllText(appConfiguration.TemplateFiles.DeltaboardTemplateFile);
            _deltaboardRowTemplate = File.ReadAllText(appConfiguration.TemplateFiles.DeltaboardRowTemplateFile);
            _deltaboardSidebarTemplate = File.ReadAllText(appConfiguration.TemplateFiles.DeltaboardSidebarTemplateFile);
        }

        public void AddDelta(string username)
        {
            // Add an entry for this user to the local db
            _db4Repository.AddDeltaboardEntry(username);

            // Build and update wiki
            buildAndUpdateDeltaboards();
        }

        public void RemoveDelta(string username)
        {
            // Remove an entry for this user from the local db
            _db4Repository.RemoveDeltaboardEntry(username);

            // Build and update wiki
            buildAndUpdateDeltaboards();
        }

        public void RefreshDeltaboards()
        {
            buildAndUpdateDeltaboards();
        }

        private List<Deltaboard> getDeltaboards()
        {
            return _db4Repository.GetCurrentDeltaboards();
        }

        private void buildAndUpdateDeltaboards()
        {
            try
            {
                // Get the updated deltaboards
                var deltaboards = getDeltaboards();

                // Build the actual string content
                string updatedDeltaboards = buildDeltaboardsContent(deltaboards);

                // Update the wiki page
                _subredditService.EditWikiPage(_appConfiguration.WikiUrlDeltaboards, updatedDeltaboards, UpdateDeltaboarsdReason);

                // Get sidebar
                string sidebar = _subredditService.GetSidebar();

                var sidebarDeltaboardMatch = _appConfiguration.DeltaboardSidebarRegex.Match(sidebar);

                // Build just the monthly
                string monthlyDeltaboardContent =
                    buildSidebarDeltaboard(deltaboards.First(db => db.DeltaboardType == DeltaboardType.Monthly));

                // Note: the second group is the content we're interested in
                if (sidebarDeltaboardMatch.Groups.Count == 2)
                {
                    string currentSidebarDeltaboard = sidebarDeltaboardMatch.Groups[1].Value;
                    string updatedSidebarDeltaboard =
                        $"\r\n{monthlyDeltaboardContent}";

                    // Update sidebar with new content
                    string updatedSidebar = sidebar.Replace(currentSidebarDeltaboard, updatedSidebarDeltaboard);

                    _subredditService.UpdateSidebar(updatedSidebar);
                }

                // Update monthly deltaboard sidebar widget for reddit redesigned site
                // HACK: The sidebar widget doesn't need the header, so get rid of it
                string monthlyNoHeader = monthlyDeltaboardContent
                    .Replace("###### Monthly Deltaboard", string.Empty)
                    .TrimStart(Environment.NewLine.ToCharArray());

                _subredditService.UpdateSidebarWidget(_appConfiguration.DeltaboardSidebarWidgetName, monthlyNoHeader);
            }
            catch (Exception ex)
            {
                // Shouldn't crash if Deltaboards can't be updated. Swallow / log exception
                _logger.Error(ex, "Error while updating Deltaboards.");
            }
        }

        private string buildDeltaboardsContent(List<Deltaboard> deltaboards)
        {
            string deltaboardsContent = _deltaboardsTemplate;

            // Build daily deltaboard
            deltaboardsContent = deltaboardsContent.Replace(_appConfiguration.ReplaceTokens.DailyDeltaboardToken,
                buildDeltaboard(deltaboards.First(db => db.DeltaboardType == DeltaboardType.Daily)));

            // Build weekly deltaboard
            deltaboardsContent = deltaboardsContent.Replace(_appConfiguration.ReplaceTokens.WeeklyDeltaboardToken,
                buildDeltaboard(deltaboards.First(db => db.DeltaboardType == DeltaboardType.Weekly)));

            // Build montly deltaboard
            deltaboardsContent = deltaboardsContent.Replace(_appConfiguration.ReplaceTokens.MonthlyDeltaboardToken,
                buildDeltaboard(deltaboards.First(db => db.DeltaboardType == DeltaboardType.Monthly)));

            // Build yearly deltaboard
            deltaboardsContent = deltaboardsContent.Replace(_appConfiguration.ReplaceTokens.YearlyDeltaboardToken,
                buildDeltaboard(deltaboards.First(db => db.DeltaboardType == DeltaboardType.Yearly)));

            // Build all time deltaboard
            deltaboardsContent = deltaboardsContent.Replace(_appConfiguration.ReplaceTokens.AllTimeDeltaboardToken,
                buildDeltaboard(deltaboards.First(db => db.DeltaboardType == DeltaboardType.AllTime)));

            return deltaboardsContent;
        }

        private string buildDeltaboard(Deltaboard deltaboard)
        {
            string deltaboardContent = _deltaboardTemplate;

            deltaboardContent = deltaboardContent
                .Replace(_appConfiguration.ReplaceTokens.DeltaboardTypeToken, deltaboard.DeltaboardType.GetDescription())
                .Replace(_appConfiguration.ReplaceTokens.DeltaboardRowsToken,
                    buildDeltaboardRows(deltaboard.Entries.OrderBy(e => e.Rank).Take(RanksToShow).ToList()))
                .Replace(_appConfiguration.ReplaceTokens.DateToken, DateTime.UtcNow.ToString("M/d/yyyy HH:mm:ss UTC"));

            return deltaboardContent;
        }

        private string buildDeltaboardRows(List<DeltaboardEntry> entries)
        {
            string rowsContent = string.Empty;

            foreach (var entry in entries)
            {
                string rowContent = _deltaboardRowTemplate
                    .Replace(_appConfiguration.ReplaceTokens.RankToken, entry.Rank.ToString())
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, entry.Username)
                    .Replace(_appConfiguration.ReplaceTokens.UserWikiLinkToken, getUserWikiUrl(entry.Username))
                    .Replace(_appConfiguration.ReplaceTokens.CountToken, entry.Count.ToString());

                rowsContent = $"{rowsContent}{rowContent}\r\n";
            }

            return rowsContent.TrimEnd("\r\n".ToCharArray());
        }

        private string buildSidebarDeltaboard(Deltaboard deltaboard)
        {
            string deltaboardContent = _deltaboardSidebarTemplate;

            deltaboardContent = deltaboardContent
                .Replace(_appConfiguration.ReplaceTokens.DeltaboardRowsToken,
                    buildDeltaboardRows(deltaboard.Entries.OrderBy(e => e.Rank).Take(RanksToShow).ToList()))
                .Replace(_appConfiguration.ReplaceTokens.DateToken, DateTime.UtcNow.ToString("M/d/yyyy HH:mm:ss UTC"))
                .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName);

            return deltaboardContent;
        }

        private string getUserWikiUrl(string username)
        {
            string userUrl = _appConfiguration.WikiUrlUser.Replace(_appConfiguration.ReplaceTokens.UsernameToken, username);
            return $"{_appConfiguration.RedditBaseUrl}{_subredditService.GetWikiUrl()}{userUrl}";
        }
    }
}
