using System.Collections.Generic;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using Newtonsoft.Json;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaboardEditor : IDeltaboardEditor
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IDeltaboardRepository _deltaDeltaboardRepository;
        private readonly IWikiEditor _wikiEditor;

        public DeltaboardEditor(AppConfiguration appConfiguration, 
            IDeltaboardRepository deltaDeltaboardRepository, 
            IWikiEditor wikiEditor)
        {
            _appConfiguration = appConfiguration;
            _deltaDeltaboardRepository = deltaDeltaboardRepository;
            _wikiEditor = wikiEditor;
        }

        public void AddDelta(string username)
        {

        }

        public void RemoveDelta(string username)
        {
            
        }

        private List<Deltaboard> getDeltaboards()
        {
            return _deltaDeltaboardRepository.GetCurrentDeltaboards();
            //string deltaboardsUrl = _wikiEditor.GetPage(_appConfiguration.WikiUrlDeltaboards);

            //// Get page content
            //string pageContent = _wikiEditor.GetPage(deltaboardsUrl);

            //// Find and deserialize hidden params (the actual deltaboards)
            //var hiddenParamsMatch = _appConfiguration.HiddenParamsRegex.Match(pageContent);

            //return JsonConvert.DeserializeObject<List<Deltaboard>>(hiddenParamsMatch.Groups[1].Value);
        }
    }
}
