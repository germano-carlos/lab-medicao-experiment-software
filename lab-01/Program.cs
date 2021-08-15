using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace lab_01
{
    class Program
    {
        public static void Main(string[] args)
        {
            Run();
            Console.ReadKey();
        }

        public static void Run()
        {
            var list = BuscaRepositorios();
            Console.WriteLine(list);
        }

        public static JObject BuscaRepositorios(int quantidade = 100)
        {
            string query = @"{
                search (query: ""stars:>10000"", type:REPOSITORY, first:" + quantidade + @") {
                    nodes {
                        ... on Repository {
                            nameWithOwner
                            createdAt
                            pushedAt
                            stargazers {
                                totalCount
                            }
                        }
                    }
                }
            }";
            return GitHubAPI.Request<JObject>(query);
        }
    }
}
