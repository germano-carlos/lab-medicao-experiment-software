﻿using System.Collections.Generic;

namespace lab_01.Entities
{
    public class Nodes
    {
        public string nameWithOwner { get; set; }
        public string url { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public PullRequest pullRequest { get; set; }
        public Release releases { get; set; }
        public Star stargazers { get; set; }
        public ProgrammingLanguage primaryLanguage { get; set; }
        public Issue open { get; set; }
        public Issue closed { get; set; }
    }
}