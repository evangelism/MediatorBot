using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MediatorLib
{
    public class BingSearch
    {
        static HttpClient searchClient = new HttpClient();
        static string BingSearchKey = "8abe8220d5344e58bb5ad28fdd9ac29a";

        public static async Task<JObject> CallBingSearch(string q)
        {

            searchClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", BingSearchKey);
            try
            {
                Random r = new Random();
                var url = string.Format("https://api.cognitive.microsoft.com/bing/v5.0/search?q={0}&count={1}&mkt={2}", q, "1000", "en-GB");
                var result = await searchClient.GetAsync(url);
                result.EnsureSuccessStatusCode();
                var json = await result.Content.ReadAsStringAsync();
                dynamic data = JObject.Parse(json);

                return data.webPages.value[r.Next(5)];

            }
            catch (Exception)
            {
                return new JObject();
            }
        }

        public static async Task<JObject> CallBingImageSearch(string q)
        {

            searchClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", BingSearchKey);
            try
            {
                var url = string.Format("https://api.cognitive.microsoft.com/bing/v5.0/images/search?q={0}&mkt={1}&imageType={2}", "clap hands", "en-GB", "AnimatedGif");
                var result = await searchClient.GetAsync(url);
                result.EnsureSuccessStatusCode();
                var json = await result.Content.ReadAsStringAsync();
                dynamic data = JObject.Parse(json);

                var rand = new Random();

                return data.value[rand.Next(data.value.Count - 1)];

            }
            catch (Exception)
            {
                return new JObject();
            }
        }
    }
}
