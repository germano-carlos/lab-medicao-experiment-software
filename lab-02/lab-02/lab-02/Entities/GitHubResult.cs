using System.Collections.Generic;

namespace lab_02.Entities
{
    public class GitHubResult
    {
        public List<Nodes> nodes { get; set; }
        public PageInfo pageInfo { get; set; }
    }
}