using System;

namespace lab_02.Entities
{
    public class Nodes
    {
        public string nameWithOwner { get; set; }
        public string url { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public PullRequest pullRequests { get; set; }
        public Release releases { get; set; }
        public Star stargazers { get; set; }
        public ProgrammingLanguage primaryLanguage { get; set; }
        public Issue open { get; set; }
        public Issue closed { get; set; }
    }
}