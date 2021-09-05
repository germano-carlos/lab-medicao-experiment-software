using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using lab_02.Entities;
using lab_02.Utils;
using Newtonsoft.Json;

namespace lab_02
{
    public class Lab02BL
    {
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
            if (pageSize > 100)
                throw new Exception("Você deve especificar um total de 100 repositorios por busca no máximo !");

            List<Nodes> repositorios = new List<Nodes>();
            List<CSVFileResult> sumary = new List<CSVFileResult>();
            int contador = 1;

            var x = BuscaRepositorios(false, pageSize);
            repositorios.AddRange(x.nodes);
            sumary.AddRange(ProcessarDados(x.nodes));
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
                sumary.AddRange(ProcessarDados(x.nodes));
                ++contador;
            }

            CriaCSV(sumary);
            return repositorios;
        }

        private static void CriaCSV(List<CSVFileResult> repositorios)
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;
            using (var writer = new StreamWriter($"{directory}\\csv-repository-files.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(repositorios);
            }
        }

        private static List<CSVFileResult> ProcessarDados(List<Nodes> repositorios)
        {
            var results = new List<CSVFileResult>();
            foreach (var repositorio in repositorios)
            {
                double issuesResolved = 0.0;
                if (repositorio.open.totalCount + repositorio.closed.totalCount != 0)
                    issuesResolved = (repositorio.closed.totalCount * 1.0) /
                                     (repositorio.open.totalCount + repositorio.closed.totalCount);

                results.Add(new CSVFileResult()
                {
                    RepositoryAge = DateTime.Now.Year - repositorio.createdAt.Year,
                    RepositoyCreatedAt = repositorio.createdAt,
                    PullRequestCount = repositorio.pullRequests.totalCount,
                    ReleasesCount = repositorio.releases.totalCount,
                    LastUpdate = DateTime.Now.Subtract(repositorio.updatedAt).TotalMinutes,
                    PrimaryLanguage = repositorio.primaryLanguage?.name,
                    IssuesCount = repositorio.closed.totalCount + repositorio.open.totalCount,
                    ClosedIssuesCount = repositorio.closed.totalCount,
                    OpenIssuesCount = repositorio.open.totalCount,
                    IssuesResolved = issuesResolved,
                    RepositoryUrl = repositorio.url
                });
            }

            return results;
        }

        public static List<CSVFileResult> LerCSV()
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;
            using var reader = new StreamReader($"{directory}\\csv-repository-files.csv");
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            return csv.GetRecords<CSVFileResult>().ToList();
        }
        public static async Task SumarizacaoFinal()
        {
        }
    }
}