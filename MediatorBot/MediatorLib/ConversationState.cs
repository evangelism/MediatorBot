using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace MediatorLib
{

    [Serializable]
    public static class ConversationState
    {

        public static int GlobalTime { get; set; } = 0;

        public static List<User> Users { get; set; } = new List<User>();

        public static TextAnalysisDocumentStore fullConvHistory = new TextAnalysisDocumentStore();

        public static void AddUser(string uname)
        {
            if (!Users.Exists(u => u.name==uname))
            {
                Users.Add(new User(uname));
            }
        }

        public static async Task<string> GetGraph()
        {
            var db = new DB();
            var ms = new MemoryStream();
            using (var ch = new Chart())
            {
                ch.ChartAreas.Add(new ChartArea());
                foreach (var u in Users)
                {
                    var s = new Series();
                    s.ChartType = SeriesChartType.Line;
                    s.Label = u.name;
                    foreach (var pnt in u.Graph) s.Points.Add(new DataPoint(pnt.Key,pnt.Value));
                    ch.Series.Add(s);
                }
                ch.SaveImage(ms, ChartImageFormat.Jpeg);
            }
            ms.Position = 0L;
            var b = await db.Upload("charts", $"{Guid.NewGuid().ToString()}.jpg", ms);
            return b.Uri.AbsoluteUri;
        }

        public static async Task<TextAnalysisDocumentStore> GetPhrasesforConversation()
        {
            TextAnalysisClient client = new TextAnalysisClient("d75051af54634a9e809edf8b2bf4e262");
            TextAnalysisDocumentStore SentResponse = await client.ExtractKeyphrases(fullConvHistory);
            return SentResponse;
        }

        public static async Task RegisterMessage(string uname, string msg)
        {
            AddUser(uname);
            var u = Users.Find(x => x.name == uname);
            await u.AddSentance(msg, fullConvHistory);
        }

        public class User
        {
            public static int id_count = 0;

            IList<string> Sentances = new List<string>();

            public User() { }

            public User(string uname)
            {
                this.name = uname;
                this.id = id_count++;
            }

            public Dictionary<int, double> Graph { get; set; } = new Dictionary<int, double>();

            public int MessageCount = 0;

            public double Sentiment
            {
                get;
                set;
            }

            public int id
            {
                get;
                set;
            }

            public string name
            {
                get;
                set;
            }

            public async Task AddSentance(string text, TextAnalysisDocumentStore fullhistory)
            {
                MessageCount++;
                Sentances.Add(text);
                if (Sentances.Count > 2)
                {
                    string merged = string.Empty;
                    foreach (string item in Sentances)
                    {
                        merged += " " + item;
                    }

                    TextAnalysisDocumentStore localStore = new TextAnalysisDocumentStore();
                    TextAnalysisDocument doc = new TextAnalysisDocument() { id = (localStore.documents.Count + 1).ToString(), text = merged };
                    localStore.documents.Add(doc);
                    TextAnalysisClient client = new TextAnalysisClient("d75051af54634a9e809edf8b2bf4e262");
                    Sentances.Clear();
                    TextAnalysisDocumentStore SentResponse = await client.AnalyzeSentiment(localStore);
                    this.Sentiment = SentResponse.documents[0].score;
                    Graph.Add(ConversationState.GlobalTime++, Sentiment);
                    doc.id = (Convert.ToInt64 (fullhistory.documents.Count) + 1).ToString();
                    fullhistory.documents.Add(doc);
                }
            }


        }

        public class TextAnalysisDocument
        {
            public TextAnalysisDocument() { }
            public TextAnalysisDocument(string id, string lang, string text)
            {
                this.id = id;
                this.language = lang;
                this.text = text;
            }
            public string language { get; set; }
            public string id { get; set; }
            public string text { get; set; }
            public double score { get; set; }
            public string[] keyPhrases { get; set; }
            public string errMessage { get; set; }
        }

        public class TextAnalysisError
        {
            public string id { get; set; }
            public string message { get; set; }
        }

        public class TextAnalysisDocumentStore
        {
            public List<TextAnalysisDocument> documents { get; set; }
            public TextAnalysisError[] errors { get; set; }
            public TextAnalysisDocumentStore()
            {
                documents = new List<TextAnalysisDocument>();
            }

            public TextAnalysisDocumentStore(TextAnalysisDocument doc)
            {
                documents = new List<TextAnalysisDocument>();
                documents.Add(doc);
            }
        }

        public class TextAnalysisClient
        {
            protected string api_key = string.Empty;
            protected string api_uri_sentiment = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";
            protected string api_uri_keyphrases = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";


            public TextAnalysisClient(string API_Key)
            {
                api_key = API_Key;
            }

            public async Task<double> AnalyzeSentiment(string text, string lang = "en")
            {
                var T = new TextAnalysisDocumentStore(new TextAnalysisDocument("id", lang, text));
                var R = await AnalyzeSentimentRaw(T);
                return R.documents[0].score;
            }

            public async Task<TextAnalysisDocumentStore> AnalyzeSentimentRaw(TextAnalysisDocumentStore S)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", api_key);

                HttpResponseMessage response;
                var s = Newtonsoft.Json.JsonConvert.SerializeObject(S);
                byte[] byteData = Encoding.UTF8.GetBytes(s);

                TextAnalysisDocumentStore res;

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(api_uri_sentiment, content);
                    var rstr = await response.Content.ReadAsStringAsync();
                    res = Newtonsoft.Json.JsonConvert.DeserializeObject<TextAnalysisDocumentStore>(rstr);
                }
                return res;
            }

            public async Task<TextAnalysisDocumentStore> AnalyzeSentiment(TextAnalysisDocumentStore S)
            {
                var R = await AnalyzeSentimentRaw(S);
                CopyDocumentInfo(S, R);
                return R;
            }

            public async Task<TextAnalysisDocumentStore> ExtractKeyphrases(TextAnalysisDocumentStore S)
            {
                var R = await ExtractKeyPhrasesRaw(S);
                CopyDocumentInfo(S, R);
                return R;
            }

            private static void CopyDocumentInfo(TextAnalysisDocumentStore S, TextAnalysisDocumentStore R)
            {
                for (int i = 0; i < R.documents.Count; i++)
                {
                    var t = (from x in S.documents
                             where x.id == R.documents[i].id
                             select x).FirstOrDefault();
                    if (t != null)
                    {
                        R.documents[i].text = t.text;
                        R.documents[i].language = t.language;
                    }
                }
            }

            public async Task<TextAnalysisDocumentStore> ExtractKeyPhrasesRaw(TextAnalysisDocumentStore S)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", api_key);

                HttpResponseMessage response;
                var s = Newtonsoft.Json.JsonConvert.SerializeObject(S);
                byte[] byteData = Encoding.UTF8.GetBytes(s);

                TextAnalysisDocumentStore res;

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(api_uri_keyphrases, content);
                    var rstr = await response.Content.ReadAsStringAsync();
                    res = Newtonsoft.Json.JsonConvert.DeserializeObject<TextAnalysisDocumentStore>(rstr);
                }
                return res;
            }

        }

    }
}

