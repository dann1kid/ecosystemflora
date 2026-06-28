using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesCsvLoadWarnings
    {
        public static void LogIssues(ICoreAPI api, string path, IList<CsvRowIssue> issues, bool userFile)
        {
            if (api?.Logger == null || issues == null || issues.Count == 0) return;

            string fileLabel = userFile ? "user CSV" : "CSV";
            string fileName = Path.GetFileName(path);

            for (int i = 0; i < issues.Count; i++)
            {
                CsvRowIssue issue = issues[i];
                switch (issue.Kind)
                {
                    case CsvRowIssueKind.DuplicateSpecies:
                        api.Logger.Warning(
                            "[ecosystemflora] Duplicate species '{0}' in {1} {2} line {3} (last row wins).",
                            issue.Species,
                            fileLabel,
                            fileName,
                            issue.LineNumber);
                        break;
                    case CsvRowIssueKind.UnknownSpecies:
                        api.Logger.Warning(
                            "[ecosystemflora] Unknown contract species '{0}' in {1} {2} line {3} (row merged but not in catalog; fix typo or remove).",
                            issue.Species,
                            fileLabel,
                            fileName,
                            issue.LineNumber);
                        break;
                }
            }
        }
    }
}
