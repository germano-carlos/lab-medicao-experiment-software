using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using lab_01.Entities;
using Newtonsoft.Json;
using CsvHelper;

namespace lab_01
{
    public class Project
    {
        public string CustomerName { get; set; }
        public string Title { get; set; }
        public DateTime Deadline { get; set; }
    }
    
    class Program
    {
        public static void Main(string[] args)
        {
            Run();
            Console.ReadKey();
        }

        public static void Run()
        {
            var list = BuscaRepositoriosPaginados(100, 152);
            Console.WriteLine(list.ToString());
        }

        public static GitHubResult BuscaRepositorios(bool hasNext, int quantidade = 100, string cursorP = "")
        {
            string cursor = ", after: null";
            if (hasNext)
                cursor = $", after: \"{cursorP}\"";

            string query = @"{
                search (query: ""stars:>100"", type:REPOSITORY, first:" + quantidade + cursor + @" ) {
                    pageInfo {
                          hasNextPage
                          endCursor
                    }
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
                            stargazers(orderBy: {field: STARRED_AT, direction: DESC}) {
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

            return GitHubAPI.Request<GitHubResult>(query);
        }

        public static List<Nodes> BuscaRepositoriosPaginados(int pageSize, int? qntElements = null,
            int? pageAmount = null)
        {
            List<Nodes> repositorios = new List<Nodes>();
            int contador = 1;

            var x = BuscaRepositorios(false, pageSize);
            repositorios.AddRange(x.nodes);
            while (
                x.pageInfo.hasNextPage &&
                (!pageAmount.HasValue || contador < pageAmount.Value) &&
                (!qntElements.HasValue || repositorios.Count < qntElements.Value)
            )
            {
                if (qntElements.HasValue && repositorios.Count + pageSize > qntElements.Value)
                    pageSize = qntElements.Value - repositorios.Count;

                x = BuscaRepositorios(true, pageSize, x.pageInfo.endCursor);
                repositorios.AddRange(x.nodes);
                ++contador;
            }

            ProcessarDados(repositorios);
            CriaCSV(repositorios);
            return repositorios;
        }

        private static void CriaCSV(List<Nodes> repositorios)
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;
            using (var writer = new StreamWriter($"{directory}\\csv-repository-files.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(repositorios);
            }
        }

        private static void ProcessarDados(List<Nodes> repositorios)
        {
            
        }
    }
}
