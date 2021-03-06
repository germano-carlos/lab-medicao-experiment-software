// ReSharper disable once RedundantUsingDirective
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;

namespace lab_02.Utils
{
    public class GitHubAPI
    {
        protected static string _url = "https://api.github.com/graphql";
        protected static string _token = $"bearer {Environment.GetEnvironmentVariable("GitHubToken")}";

        public static T Request<T>(string query)
        {
            var j = new JObject
            {
                ["query"] = query
            };
            try
            {
                var client = new RestClient(_url);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", _token);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", j.ToString(), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
            
                JObject value = JsonConvert.DeserializeObject<JObject>(response.Content);
                if (value["errors"] != null)
                    throw new Exception(value["errors"].ToString());
                if (value["data"]?["java"] == null)
                    throw new Exception("Não foi localizado nenhum registro para a consulta realizada");
                return JsonConvert.DeserializeObject<T>(value["data"]?["java"].ToString());
            }
            catch (Exception e)
            {
                throw new Exception($"Houve um erro ao processar a requisição na API GITHUB [{e.Message}]");
            }
        }
    }
}