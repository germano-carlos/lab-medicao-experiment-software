using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using lab_01.Entities;
using Newtonsoft.Json;

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
            var ListaCompleta = new List<GitHubResult>();
            var list = BuscaRepositorios();

            Console.WriteLine(list.ToString());
        }

        public static List<GitHubResult> BuscaRepositorios(int quantidade = 100)
        {
            string query = @"{
                search (query: ""stars:>100"", type:REPOSITORY, first:" + quantidade + @") {
                    nodes {
                        ... on Repository {
                            nameWithOwner
                            url
                            createdAt
                            updatedAt
                            pullRequests(states:MERGED) {
                                totalCount
                            }
                            releases(first:1) {
                                totalCount
                            }
                            stargazers {
                                totalCount
                            }
                            primaryLanguage {
                                name
                            }
                            open: issues(states:OPEN) {
                                totalCount
                            }
                            closed: issues(states:CLOSED) {
                                totalCount
                            }
                        }
                    }
                }
            }";
            return GitHubAPI.Request<List<GitHubResult>>(query);
        }

        public static void BuscaRepositoriosPaginados(int pageSize)
        {
            
        }
    }
}
