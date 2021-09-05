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
using LibGit2Sharp;
using Microsoft.CSharp.RuntimeBinder;
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

        public static void Sumarizacao(int? indexOf = null, int? specificElement = null)
        {
            var repositorios = LerCSV();
            if (indexOf.HasValue)
                repositorios = repositorios.GetRange(indexOf.Value, repositorios.Count - indexOf.Value);
            if (specificElement.HasValue)
                repositorios = new List<CSVFileResult> { repositorios.ElementAt(specificElement.Value) };

            foreach (var repo in repositorios)
            {
                // Clone
                GitClone(repo);
                //Execute Jar File
                // XXXX
                // Delete Folder
                DeleteFolder(repo);
            }
        }

        public static void GitClone(CSVFileResult repositorio)
        {
            if (String.IsNullOrEmpty(repositorio.RepositoryClone) || !repositorio.RepositoryClone.EndsWith(".git"))
                throw new Exception("É Necessário possuir um endereço de clone valido");

            string pathToSave = $"C:\\www\\temp\\repositorios\\{repositorio.RepositoryOwner.Split('/').Last()}";
            string gitCommand = "git";
            string gitCloneArgument = $"clone {repositorio.RepositoryClone} {pathToSave}";

            Process.Start(gitCommand, gitCloneArgument);
            System.Threading.Thread.Sleep(new TimeSpan(0,0,30));
        }

        public static bool ExecuteJarFile()
        {
            var validJavaAppRun = true;

            try
            {
                string JDKpath = "", jarFile = "", aParamsInput = "", aParamOutput = "", autonetCol = "";
                Process process = new Process();
                process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(JDKpath) ?? string.Empty;
                process.StartInfo.FileName = (JDKpath) + "Java";
                string aArgument = $"{jarFile} {aParamsInput} {aParamOutput} {autonetCol}";
                process.StartInfo.Arguments = string.Format(aArgument);
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
         
                int holdStatus = process.ExitCode;

                if (holdStatus > 0)
                {
                    validJavaAppRun = false;
                }
         
            }
            catch (Exception ex)
            {
                if (ex.StackTrace is not null)
                    Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace);
                validJavaAppRun = false;
            }
     
            return validJavaAppRun;
        }

        public static void DeleteFolder(CSVFileResult repositorio)
        {
            try
            {
                var dir = new DirectoryInfo($"C:\\www\\temp\\repositorios\\") { Attributes = FileAttributes.Normal };;
                foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                dir.Delete(true);
            }
            catch (IOException ex)
            {
                throw new Exception(ex.Message);
            }
        }
        
        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }
            baseDir.Delete(true);
        }
        public static void setAttributesNormal(System.IO.DirectoryInfo directory)
        {
            foreach (var subDirectoryPath in directory.GetDirectories())
            {
                var directoryInfo = new DirectoryInfo(subDirectoryPath.ToString());
                foreach (var filePath in directoryInfo.GetFiles()) 
                {
                    var file = new FileInfo(filePath.ToString());
                    file.Attributes = FileAttributes.Normal;
                }
            }
        }
    }
}