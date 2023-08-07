using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using System.Diagnostics;

namespace vsc.Fluent.Plugin
{
    public sealed class VSCodeWorkspacesSearchResult : SearchResultBase
    {
        public VSCodeWorkspacesSearchResult(string name,string path,ProcessStartInfo process, string searchAppName, string resultName, string searchedText,
            string resultType, double score, IList<ISearchOperation> supportedOperations, 
            ProcessInfo processInfo = null) : base(searchAppName,
            resultName, searchedText, resultType, score,
            supportedOperations, new List<SearchTag> { new() { Name = "vsc", IconGlyph = "\uE943" } })
        {
            DisplayedName = name;
            AdditionalInformation = path;
            Process= process;
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public ProcessStartInfo Process { get; set; }
        protected override void OnSelectedSearchResultChanged()
        {
            // Leave this method empty for now.
        }
    }
}
