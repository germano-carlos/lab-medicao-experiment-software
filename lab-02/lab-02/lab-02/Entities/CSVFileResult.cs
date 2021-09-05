using System;

namespace lab_02.Entities
{
    public class CSVFileResult
    {
        public int RepositoryAge { get; set; }
        public DateTime RepositoyCreatedAt { get; set; }
        public long ReleasesCount { get; set; }
        public string PrimaryLanguage { get; set; }
        public string RepositoryUrl { get; set; } 
        public string RepositoryClone { get; set; } 
        public string RepositoryOwner { get; set; }
        public long StarsCount { get; set; }
        public long? SourceLinesOfCode { get; set; }
    }
}