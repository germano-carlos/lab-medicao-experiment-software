using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
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
            string query = @"
            {
              java: search(query: ""language:java"", type: REPOSITORY, first:" + quantidade + cursor + @") {
                        pageInfo {
                            hasNextPage
                                endCursor
                        }
                        ...SearchResult
                    }
                }

                fragment SearchResult on SearchResultItemConnection {
                  repositoryCount
                  nodes {
                      ... on Repository {
                        nameWithOwner
                        url
                        createdAt
                        updatedAt
                        pullRequests(states: MERGED) {
                          totalCount
                        }
                        releases(first: 1) {
                          totalCount
                        }
                        stargazers(orderBy: {field: STARRED_AT, direction: DESC}) {
                          totalCount
                        }
                        primaryLanguage {
                          name
                        }
                        open: issues(states: OPEN) {
                          totalCount
                        }
                        closed: issues(states: CLOSED) {
                          totalCount
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
            int contador = 1;

            var x = BuscaRepositorios(false, pageSize);
            repositorios.AddRange(x.nodes);
            CriaCSV(ProcessarDados(x.nodes), true);
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
                CriaCSV(ProcessarDados(x.nodes), false);
                ++contador;
            }
            
            return repositorios;
        }

        private static void CriaCSV(List<CSVFileResult> repositorios, bool isFirst)
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;
            string file = $"{directory}\\csv-repository-files.csv";

            if (isFirst)
            {
                using var writer = new StreamWriter(file);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(repositorios);

                return;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using var stream = File.Open(file, FileMode.Append);
            {
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, config);
                csv.WriteRecords(repositorios);
            }
        }

        private static List<CSVFileResult> ProcessarDados(List<Nodes> repositorios)
        {
            var results = new List<CSVFileResult>();
            foreach (var repositorio in repositorios)
            {
                results.Add(new CSVFileResult()
                {
                    RepositoryAge = DateTime.Now.Year - repositorio.createdAt.Year,
                    RepositoyCreatedAt = repositorio.createdAt,
                    ReleasesCount = repositorio.releases.totalCount,
                    PrimaryLanguage = repositorio.primaryLanguage?.name,
                    RepositoryUrl = repositorio.url,
                    RepositoryOwner = repositorio.nameWithOwner,
                    StarsCount = repositorio.stargazers.totalCount,
                    SourceLinesOfCode = null
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

        public static void Sumarizacao()
        {
            var repositorios = LerCSV();
        }
    }
}