using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab_01
{
    class GitHubAPI
    {
        protected static string _url = "https://api.github.com/graphql";
        protected static string _token = "bearer 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";

        public static T Request<T>(string query)
        {
            JObject j = new JObject();
            j["query"] = query;

            var client = new RestClient(_url);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", _token);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", j.ToString(), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return JsonConvert.DeserializeObject<T>(response.Content);
        }
    }
}
