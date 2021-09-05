using System.Collections.Generic;

namespace lab_01.Entities
{
    public class GitHubResult
    {
        public List<Nodes> nodes { get; set; }
        public PageInfo pageInfo { get; set; }
    }
}