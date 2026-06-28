namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal enum CsvRowIssueKind
    {
        DuplicateSpecies,
        UnknownSpecies,
    }

    internal readonly struct CsvRowIssue
    {
        public CsvRowIssue(CsvRowIssueKind kind, int lineNumber, string species)
        {
            Kind = kind;
            LineNumber = lineNumber;
            Species = species;
        }

        public CsvRowIssueKind Kind { get; }
        public int LineNumber { get; }
        public string Species { get; }
    }
}
