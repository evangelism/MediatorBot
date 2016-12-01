﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MediatorLib
{
    public class ConversationStore
    {

        List<User> users = new List<User>();
        public List<User> Users
        {
            get
            {
                return this.users;
            }
        }
    }
    public class User
    {
        IList<string> Sentances = new List<string>();

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

        public async void AddSentance(string text)
        {
            Sentances.Add(text);
            if (Sentances.Count > 2)
            {
                string merged = string.Empty;
                foreach (string item in Sentances)
                {
                    merged += " " + item;
                }

                TextAnalysisDocumentStore localStore = new TextAnalysisDocumentStore();
                localStore.documents.Add(new MediatorLib.TextAnalysisDocument() { id = (localStore.documents.Count + 1).ToString(), text = merged });
                MediatorLib.TextAnalysisClient client = new MediatorLib.TextAnalysisClient("d75051af54634a9e809edf8b2bf4e262");
                Sentances.Clear();
                MediatorLib.TextAnalysisDocumentStore SentResponse = await client.AnalyzeSentiment(localStore);
                this.Sentiment = SentResponse.documents[0].score;
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
            documents = new List<MediatorLib.TextAnalysisDocument>();
        }

        public TextAnalysisDocumentStore(TextAnalysisDocument doc)
        {
            documents = new List<MediatorLib.TextAnalysisDocument>();
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
