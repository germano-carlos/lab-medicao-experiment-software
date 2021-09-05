using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using lab_01.Entities;
using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace lab_01
{
    public class Lab01BL
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
            // Montagem do box-plot: Maior Valor, Menor Valor, Terceiro Quartil, Mediana, Primeiro Quartil
            // Quartil -> (%Definida * Quantidade Elementos / 100)
            
            // Faz a leitura do arquivo CSV com os repositórios já coletados.
            var repositorios = LerCSV();
            var taskList = new List<Task<string>>
            {
                // RQ01 - Análise de idades dos repositórios
                Task.Run(() =>
                {
                    var elements = repositorios.OrderBy(s => s.RepositoryAge).Select(s => s.RepositoryAge).ToList();
                    var maiorValor = elements.ElementAt(repositorios.Count - 1);
                    var menorValor = elements.ElementAt(0);
                    var TerceiroQuartil = elements.ElementAt((repositorios.Count * 75) / 100);
                    var mediana = elements.ElementAt((repositorios.Count * 50) / 100);
                    var PrimeiroQuartil = elements.ElementAt((repositorios.Count * 25) / 100);

                    return 
                        $@"Quantidade de Elementos: {repositorios.Count} \n
                       Maior Valor: {maiorValor} \n
                       Menor Valor: {menorValor} \n
                       TerceiroQuartil: {TerceiroQuartil}  \n
                       Mediana: {mediana}  \n
                       PrimeiroQuartil: {PrimeiroQuartil}  \n
                       Elements: {JsonConvert.SerializeObject(elements, Formatting.Indented)}";
                }),
                // RQ02 - Análise de total de pull requests dos repositórios
                Task.Run(() =>
                {
                    var elements = repositorios.OrderBy(s => s.PullRequestCount).Select(s => s.PullRequestCount).ToList();
                    var maiorValor = elements.ElementAt(repositorios.Count - 1);
                    var menorValor = elements.ElementAt(0);
                    var TerceiroQuartil = elements.ElementAt((repositorios.Count * 75) / 100);
                    var mediana = elements.ElementAt((repositorios.Count * 50) / 100);
                    var PrimeiroQuartil = elements.ElementAt((repositorios.Count * 25) / 100);
                
                    return 
                        $@"Quantidade de Elementos: {repositorios.Count} \n
                        Maior Valor: {maiorValor} \n
                        Menor Valor: {menorValor} \n
                        TerceiroQuartil: {TerceiroQuartil}  \n
                        Mediana: {mediana}  \n
                        PrimeiroQuartil: {PrimeiroQuartil}  \n
                        Elements: {JsonConvert.SerializeObject(elements, Formatting.Indented)}";
                }),
                // RQ03 - Análise de total de releases dos repositórios
                Task.Run(() =>
                {
                    var elements = repositorios.OrderBy(s => s.ReleasesCount).Select(s => s.ReleasesCount).ToList();
                    var maiorValor = elements.ElementAt(repositorios.Count - 1);
                    var menorValor = elements.ElementAt(0);
                    var TerceiroQuartil = elements.ElementAt((repositorios.Count * 75) / 100);
                    var mediana = elements.ElementAt((repositorios.Count * 50) / 100);
                    var PrimeiroQuartil = elements.ElementAt((repositorios.Count * 25) / 100);
                
                    return 
                        $@"Quantidade de Elementos: {repositorios.Count} \n
                        Maior Valor: {maiorValor} \n
                        Menor Valor: {menorValor} \n
                        TerceiroQuartil: {TerceiroQuartil}  \n
                        Mediana: {mediana}  \n
                        PrimeiroQuartil: {PrimeiroQuartil}  \n
                        Elements: {JsonConvert.SerializeObject(elements, Formatting.Indented)}";
                }),
                // RQ04 - Análise de tempo até a última atualização dos repositórios
                Task.Run(() =>
                {
                    var elements = repositorios.OrderBy(s => s.LastUpdate).Select(s => s.LastUpdate).ToList();
                    var maiorValor = elements.ElementAt(repositorios.Count - 1);
                    var menorValor = elements.ElementAt(0);
                    var TerceiroQuartil = elements.ElementAt((repositorios.Count * 75) / 100);
                    var mediana = elements.ElementAt((repositorios.Count * 50) / 100);
                    var PrimeiroQuartil = elements.ElementAt((repositorios.Count * 25) / 100);

                    return $@"Quantidade de Elementos: {repositorios.Count} \n
                        Maior Valor: {maiorValor} \n
                        Menor Valor: {menorValor} \n
                        TerceiroQuartil: {TerceiroQuartil}  \n
                        Mediana: {mediana}  \n
                        PrimeiroQuartil: {PrimeiroQuartil}  \n
                        Elements: {JsonConvert.SerializeObject(elements, Formatting.Indented)}";
                }),
                // RQ05 - Análise de linguagem primária  dos repositórios
                Task.Run(() =>
                {
                    var elements = repositorios.OrderBy(s => s.PrimaryLanguage)
                        .GroupBy(s => new {s.PrimaryLanguage})
                        .Select(s => new SumaryResult { name = s.Key.PrimaryLanguage, contador = s.Count()})
                        .OrderBy(s => s.contador)
                        .ToList();

                    var listInt = new List<int>();
                    foreach (var element in elements)
                        for (int i = 0; i < element.contador; i++)
                            listInt.Add(elements.IndexOf(element));

                    var cont = listInt.Count;
                    return 
                        $@"Elements: {JsonConvert.SerializeObject(elements, Formatting.Indented)}
                            Elements Box-Plot: {JsonConvert.SerializeObject(listInt, Formatting.Indented)}";
                }),
                // RQ06 - Análise de razão entre número de issues fechadas pelo total de issues dos repositórios
                Task.Run(() =>
                {
                    var elements = repositorios.OrderBy(s => s.IssuesResolved).Select(s => s.IssuesResolved).ToList();
                    var maiorValor = elements.ElementAt(repositorios.Count - 1);
                    var menorValor = elements.ElementAt(0);
                    var TerceiroQuartil = elements.ElementAt((repositorios.Count * 75) / 100);
                    var mediana = elements.ElementAt((repositorios.Count * 50) / 100);
                    var PrimeiroQuartil = elements.ElementAt((repositorios.Count * 25) / 100);

                    return 
                        $@"Quantidade de Elementos: {repositorios.Count} \n
                        Maior Valor: {maiorValor} \n
                        Menor Valor: {menorValor} \n
                        TerceiroQuartil: {TerceiroQuartil}  \n
                        Mediana: {mediana}  \n
                        PrimeiroQuartil: {PrimeiroQuartil}  \n
                        Elements: {JsonConvert.SerializeObject(elements, Formatting.Indented)}";
                })
            };

            try
            {
               await Task.WhenAll(taskList);
               var contador = 0;
               foreach (var task in taskList)
               {
                   Debug.WriteLine($"TASK ID: {contador}");
                   Debug.WriteLine(task.Result);
                   ++contador;
               }
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
}