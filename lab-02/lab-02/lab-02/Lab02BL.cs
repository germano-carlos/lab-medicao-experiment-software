using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
                    RepositoryClone = repositorio.url + ".git",
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

        public static async Task Sumarizacao(int? indexOf = null, int? specificElement = null, bool tryAgain = false)
        {
            var repositorios = LerCSV();
            var tasks = new List<Task<List<SumaryResult>>>();
            var data = new List<List<CSVFileResult>>();
            ValidaDuplicatas();
            
            if (indexOf.HasValue)
            {
                repositorios = repositorios.GetRange(indexOf.Value, repositorios.Count - indexOf.Value);
                tasks.Add(Task.Run(() => GenerateMetricsAsync(repositorios)));
            }
            else if (specificElement.HasValue)
            {
                var repositorios1 = new List<CSVFileResult> { repositorios.ElementAt(specificElement.Value)};
                tasks.Add(Task.Run(() => GenerateMetricsAsync(repositorios1,1)));
            }
            else
            {
                if(repositorios.Count is < 1000 or > 1000)
                    throw new Exception("É necessário possuir exatamente 1000 repositórios para realização da análise");

                data.Add(repositorios.GetRange(62,142));
                data.Add(repositorios.GetRange(950,50));
                data.Add(repositorios.GetRange(537,115));
                data.Add(repositorios.GetRange(786,136));
                data.Add(repositorios.GetRange(896,56));
        
                tasks.Add(Task.Run(() => GenerateMetricsAsync(data.ElementAt(0), 1, tryAgain)));
                tasks.Add(Task.Run(() => GenerateMetricsAsync(data.ElementAt(1), 2, tryAgain)));
                tasks.Add(Task.Run(() => GenerateMetricsAsync(data.ElementAt(2), 3, tryAgain)));
                tasks.Add(Task.Run(() => GenerateMetricsAsync(data.ElementAt(3), 4, tryAgain)));
                tasks.Add(Task.Run(() => GenerateMetricsAsync(data.ElementAt(4), 5, tryAgain)));
            }

            await Task.WhenAll(tasks);
            foreach (var task in tasks.Where(task => task.IsFaulted))
                Debug.WriteLine(task.Exception);
        }

        private static List<SumaryResult> GenerateMetricsAsync(List<CSVFileResult> repositorios, int? taskId = null, bool tryAgain = false)
        {
            try
            {
                var listRepositorySumarized = new List<SumaryResult>(); 
                var contador = 0;
                foreach (var repo in repositorios)
                {
                    // Clone Repository
                    if (!GitClone(repo))
                        throw new Exception($"Não foi possível Clonar repositório: [{repo.RepositoryOwner} - {contador}]");
                    //Execute Jar File
                    if (!ExecuteJarFile(repo, taskId))
                        throw new Exception($"Não foi possível Gerar as Métricas para este repositório: [{repo.RepositoryOwner} - {contador}]");
                    // Delete Folder
                    DeleteFolder(repo);
                
                    // Generate Data from CK - CSV
                    List<DataSumarized> csvGenerated = ReadCSVGenerated(taskId);
                    var isFirst = contador == 0 && !tryAgain;
                    var result = ProcessCSVData(csvGenerated, repo.RepositoryOwner);
                    listRepositorySumarized.Add(result);
                    CreateCSVResult(result, isFirst, taskId);
                    ++contador;
                }

                return listRepositorySumarized;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"A Thread [{taskId ?? -1}] não foi concluída com sucesso !");
                Debug.WriteLine(e.Message);
            }

            return null;
        }
        private static bool GitClone(CSVFileResult repositorio)
        {
            try
            {
                if (String.IsNullOrEmpty(repositorio.RepositoryClone) || !repositorio.RepositoryClone.EndsWith(".git"))
                    throw new Exception("É Necessário possuir um endereço de clone valido");

                var parent = Directory.GetParent(Directory.GetCurrentDirectory());
                var directory = parent?.Parent?.Parent?.FullName;

                var pathToSave = $"{directory}\\repositorios\\{repositorio.RepositoryOwner.Split('/').Last()}";

                var process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = $"clone {repositorio.RepositoryClone} {pathToSave}";

                process.Start();
                process.WaitForExit();

                return process.ExitCode <= 0;
            }
            catch (Exception ex)
            {
                if (ex.StackTrace is not null)
                    Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private static bool ExecuteJarFile(CSVFileResult repositorio, int? taskId)
        {
            try
            {
                var parent = Directory.GetParent(Directory.GetCurrentDirectory());
                var directory = parent?.Parent?.Parent?.FullName;

                var jarFile = $"{directory}\\ck-0.6.5-SNAPSHOT-jar-with-dependencies.jar";
                var repository = $"{directory}\\repositorios\\{repositorio.RepositoryOwner.Split('/').Last()}";
                var destination = $"{directory}\\metrics\\metrics{(taskId.HasValue ? $"-{taskId}" : "")}";

                var process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "java";
                process.StartInfo.Arguments = $"-jar {jarFile} {repository} true 0 false {destination}";

                process.Start();
                process.WaitForExit();
         
                return process.ExitCode <= 0;
            }
            catch (Exception ex)
            {
                if (ex.StackTrace is not null)
                    Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private static void DeleteFolder(CSVFileResult repositorio)
        {
            try
            {
                var parent = Directory.GetParent(Directory.GetCurrentDirectory());
                var directory = parent?.Parent?.Parent?.FullName;

                var dir = new DirectoryInfo($"{directory}\\repositorios\\{repositorio.RepositoryOwner.Split('/').Last()}") { Attributes = FileAttributes.Normal };;
                foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                dir.Delete(true);
                System.Threading.Thread.Sleep(new TimeSpan(0,0,10));
            }
            catch (IOException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static List<DataSumarized> ReadCSVGenerated(int? taskId)
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;

            var destination = $"{directory}\\metrics\\metrics{(taskId.HasValue ? $"-{taskId}" : "")}class";
            using var reader = new StreamReader($"{destination}.csv");
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<DataSumarized>().ToList();
        }
        
        private static List<SumaryResult> ReadFinalCSVGenerated(int? taskId)
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;

            var destination = $"{directory}\\csv-final-{(taskId.HasValue ? $"-{taskId}" : "")}.csv";
            using var reader = new StreamReader($"{destination}");
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<SumaryResult>().ToList();
        }

        private static void ValidaDuplicatas()
        {
            var csvGenerated2 = ReadFinalCSVGenerated(1);
            var csvGenerated3 = ReadFinalCSVGenerated(2);
            var csvGenerated4 = ReadFinalCSVGenerated(3);
            var csvGenerated5 = ReadFinalCSVGenerated(4);
            var csvGenerated6 = ReadFinalCSVGenerated(5);

            var x = new List<SumaryResult>();
            x.AddRange(csvGenerated2);
            x.AddRange(csvGenerated3);
            x.AddRange(csvGenerated4);
            x.AddRange(csvGenerated5);
            x.AddRange(csvGenerated6);

            var dictionary = new Dictionary<string, int>();
            foreach (var repo in x)
            {
                if (!dictionary.ContainsKey(repo.RepositoryName))
                    dictionary.Add(repo.RepositoryName, 1);
                else
                    dictionary[repo.RepositoryName]++;
            }

            var list = dictionary.Where(s => s.Value > 1);
            Debug.WriteLine(JsonConvert.SerializeObject(list, Formatting.Indented));
            Debug.WriteLine($"Possui um total de {list.Count()} elementos repetidos");
        }

        private static SumaryResult ProcessCSVData(List<DataSumarized> csvGenerated, string repositoryName)
        {
            var cbo = new List<double>(); 
            var cboModified = new List<double>(); 
            var dit = new List<double>();
            var lcom = new List<double>(); 
            var lcom2 = new List<double>(); 
            long loc = 0;

            foreach (var data in csvGenerated)
            {
                cbo.Add(data.Cbo);
                cboModified.Add(data.CboModified);
                dit.Add(data.Dit);
                lcom.Add(data.Lcom);
                lcom2.Add(data.Lcom2);
                loc += data.Loc;
            }

            var result = new SumaryResult()
            {
                RepositoryName = repositoryName,
                Cbo = cbo.Count > 0 ? MathHelper.Median(cbo.ToArray()) : 0,
                CboModified = cboModified.Count > 0 ? MathHelper.Median(cboModified.ToArray()) : 0,
                Dit = dit.Count > 0 ? MathHelper.Median(dit.ToArray()) : 0,
                Lcom = lcom.Count > 0 ? MathHelper.Median(lcom.ToArray()) : 0,
                Lcom2 = lcom2.Count > 0 ? MathHelper.Median(lcom2.ToArray()) : 0,
                Loc = loc
            };
            
            return result;
        }
        
        private static void CreateCSVResult(SumaryResult register, bool isFirst, int? taskId)
        {
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            var directory = parent?.Parent?.Parent?.FullName;
            var file = $"{directory}\\csv-final-{(taskId.HasValue ? $"-{taskId}" : "")}.csv";

            if (isFirst)
            {
                
                using var writer = new StreamWriter(file);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteHeader<SumaryResult>();
                csv.NextRecord();
                csv.WriteRecord(register);
                csv.NextRecord();

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
                csv.WriteRecord(register);
                csv.NextRecord();
            }
        }
    }
}