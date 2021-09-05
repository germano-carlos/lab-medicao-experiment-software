using System;

namespace lab_02.Entities
{
    public class CSVFileResult
    {
        public int RepositoryAge { get; set; }
        public DateTime RepositoyCreatedAt { get; set; }
        public long PullRequestCount { get; set; }
        public long ReleasesCount { get; set; }
        public double LastUpdate { get; set; }
        public string PrimaryLanguage { get; set; }
        public long IssuesCount { get; set; }
        public long ClosedIssuesCount { get; set; }
        public long OpenIssuesCount { get; set; }
        public double IssuesResolved { get; set; }
        public string RepositoryUrl { get; set; } 
    }
}