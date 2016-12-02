using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Text;
using MediatorLib;
using System.IO;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace MediatorBot
{
    [LuisModel("f38fbeda-63a5-4a86-b00b-5e3bcfa08d55", "719a7a71cd7f458682f8ca473401f4e0")]
    [Serializable]
    public class MediatorDialog : LuisDialog<string>
    {

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            //Walkaround to get activity
            msg = (Activity)await item;
            await base.MessageReceived(context, item);
        }

        [field: NonSerialized()]
        private Activity msg;

        [LuisIntent("")]
        private async Task ProcessMessage(IDialogContext context, LuisResult result)
        {
            
            if (msg.Text != "!users" && msg.Text != "!stats")
            {
                await ConversationState.RegisterMessage(msg.From.Name, msg.Text);
            }

            var badSentimenCheck = ConversationState.Users.Where((u => u.Sentiment < 0.4 && u.Sentiment != 0));
            if (badSentimenCheck.Any())
            {
                ConversationState.TextAnalysisDocumentStore phrasesDoc = await ConversationState.GetPhrasesforConversation();
               
                double score = 0;
                var k = 0;
                var i = 0;
                foreach(var doc in phrasesDoc.documents)
                {
                    if(doc.score > score)
                    {
                        score = doc.score;
                        k = i;
                    }
                    i++;
                }

                var reply = await BuildBingReply(phrasesDoc.documents[k].keyPhrases[0]);
                await context.PostAsync(reply);
                

                //await context.PostAsync(BuildReply(
                //sb =>
                //{
                //    foreach (ConversationState.TextAnalysisDocument doc in phrasesDoc.documents)
                //    {
                //        foreach (string x in doc.keyPhrases)
                //        {
                //            sb.AppendLine($"Phrase: {x}");
                //        }
                //    }
                //   }));
              
            }

            if (msg.Text == "!users")
            {
                await context.PostAsync(BuildReply(
                    sb =>
                    {
                        ConversationState.Users.ForEach(x => sb.AppendLine(x.name));
                    }));
            }
            //Stats and graph, put into separate LUIS intent, but to be sure, we leave it also here
            else if (msg.Text == "!stats")
            {
                await context.PostAsync(BuildReply(
                    sb =>
                    {
                        foreach (var x in ConversationState.Users)
                        { sb.AppendLine($"{x.name}: msgs={x.MessageCount}, sentiment={x.Sentiment}"); }
                    }));
            }
            else if (msg.Text == "!graph")
            {
                IMessageActivity repl = await CreateGraphReply(context);
                await context.PostAsync(repl);
            }
            else
            {
                //await context.PostAsync("");
            }
            context.Wait(MessageReceived);
        }

        private static async Task<IMessageActivity> CreateGraphReply(IDialogContext context, string name = "")
        {

            var uri = await ConversationState.GetGraph(name);
            var repl = context.MakeMessage();
            repl.Text = "Please find current graph of sentiments";
            repl.Attachments = new Attachment[] {
                    new Attachment(contentType: "image/jpeg",
                    contentUrl: uri, thumbnailUrl: uri) };
            return repl;
        }

        [LuisIntent("Graph")]
        public async Task ShowGraph(IDialogContext context, LuisResult result)
        {
            
            bool wrongName = false;
            string name = "";
            if(result.Entities != null && result.Entities.Count > 0)
                name = GetNameFromPersonEntity(new List<EntityRecommendation>(result.Entities));
            if (name != "" && !ConversationState.Users.Any(user => user.name.ToLower() == name))
            {
                //Wrong name
                name = "";
                wrongName = true;
            }

            IMessageActivity repl = await CreateGraphReply(context, name);
            if (wrongName)
                repl.Text = "User with provided user name does not exist. I am sending graph for all the users.";
            await context.PostAsync(repl);
            context.Wait(MessageReceived);
        }

        private static string GetNameFromPersonEntity(List<EntityRecommendation> entities)
        {
            var name = "";
            if (entities != null && entities.Any((entity) => entity.Type == "Person"))
            {
                name = entities.Where(e => e.Type == "Person").FirstOrDefault().Entity;
            }
            return name;
        }

        [LuisIntent("Stats")]
        public async Task ShowStats(IDialogContext context, LuisResult result)
        {
            string name = "";
            if(result.Entities.Count != 0)
                name = GetNameFromPersonEntity(new List<EntityRecommendation>(result.Entities));

            ConversationState.User u = null;
            if (name != "" && ConversationState.Users.Any(user => user.name.ToLower() == name))
                u = ConversationState.Users.Where(user => user.name.ToLower() == name.ToLower()).FirstOrDefault();


            if (name == "" || u == null)
            {
                if (name != "")
                    await context.PostAsync(BuildReply(
                        sb =>
                        {
                            sb.AppendLine("User does not exist. I am showing stats for all users");
                            
                        }));

                await context.PostAsync(BuildReply(
                        sb =>
                        {
                            
                            foreach (var x in ConversationState.Users)
                            {
                                sb.AppendLine($"{x.name}: msgs={x.MessageCount}, sentiment={x.Sentiment}");
                            }
                        }));
            }
            else
            {
                await context.PostAsync(BuildReply(
                       sb =>
                       {
                           { sb.AppendLine($"{u.name}: msgs={u.MessageCount}, sentiment={u.Sentiment}"); }
                       }));
            }
            context.Wait(MessageReceived);
        }

        [LuisIntent("MostPositive")]
        public async Task ShowMostPositive(IDialogContext context, LuisResult result)
        {
            double sentiment = 0;
            ConversationState.User mostPositiveU = ConversationState.Users.First();
            foreach(var u in ConversationState.Users)
            {
                if(u.Sentiment > sentiment)
                {
                    mostPositiveU = u;
                    sentiment = u.Sentiment;
                }
            }

            await context.PostAsync(BuildReply(
                       sb =>
                       {
                           {
                               sb.AppendLine("The most positive user is:");
                               sb.AppendLine($"{mostPositiveU.name}: msgs={mostPositiveU.MessageCount}, sentiment={mostPositiveU.Sentiment}");
                           }
                       }));
            context.Wait(MessageReceived);
        }

        [LuisIntent("LeastPositive")]
        public async Task ShowLeastPositive(IDialogContext context, LuisResult result)
        {
            double sentiment = 1;
            ConversationState.User leastPositive = ConversationState.Users.First();
            foreach (var u in ConversationState.Users)
            {
                if (u.Sentiment < sentiment)
                {
                    leastPositive = u;
                    sentiment = u.Sentiment;
                }
            }

            await context.PostAsync(BuildReply(
                       sb =>
                       {
                           {
                               sb.AppendLine("The least positive user is:");
                               sb.AppendLine($"{leastPositive.name}: msgs={leastPositive.MessageCount}, sentiment={leastPositive.Sentiment}");
                           }
                       }));
            context.Wait(MessageReceived);
        }

        [LuisIntent("MostActive")]
        public async Task ShowMostActive(IDialogContext context, LuisResult result)
        {
            int msgCount = 0;
            ConversationState.User mostActiveU = null;
            
            foreach (var u in ConversationState.Users)
            {
                if (u.MessageCount >= msgCount)
                {
                    mostActiveU = u; ;
                    msgCount = u.MessageCount;
                }
            }

            await context.PostAsync(BuildReply(
                       sb =>
                       {
                           {
                               sb.AppendLine("The most active user is:");
                               sb.AppendLine($"{mostActiveU.name}: msgs={mostActiveU.MessageCount}, sentiment={mostActiveU.Sentiment}");
                           }
                       }));
            context.Wait(MessageReceived);
        }

        [LuisIntent("LeastActive")]
        public async Task ShowLeastActive(IDialogContext context, LuisResult result)
        {
            int msgCount = int.MaxValue;
            ConversationState.User leastActiveU = null;

            foreach (var u in ConversationState.Users)
            {
                if (u.MessageCount <= msgCount)
                {
                    leastActiveU = u; ;
                    msgCount = u.MessageCount;
                }
            }

            await context.PostAsync(BuildReply(
                       sb =>
                       {
                           {
                               sb.AppendLine("The least active user is:");
                               sb.AppendLine($"{leastActiveU.name}: msgs={leastActiveU.MessageCount}, sentiment={leastActiveU.Sentiment}");
                           }
                       }));
            context.Wait(MessageReceived);
        }

        [LuisIntent("Positive")]
        public async Task Positive(IDialogContext context, LuisResult result)
        {
            //Register Message
            if (msg.Text != "!users" && msg.Text != "!stats")
            {
                await ConversationState.RegisterMessage(msg.From.Name, msg.Text);
            }

            dynamic res = await BingSearch.CallBingImageSearch(result.Query);


            Activity replyToConversation = msg.CreateReply();
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<CardImage> cardImages = new List<CardImage>();
            var imgUrl = res.contentUrl.ToString();
            cardImages.Add(new CardImage(imgUrl.ToString(), "img", null));
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction plButton = new CardAction()
            {
                Value = res.contentUrl,
                Type = "openUrl",
                Title = ""
            };
            cardButtons.Add(plButton);
            ThumbnailCard plCard = new ThumbnailCard()
            {
                Title = "",
                Subtitle = "",
                Images = cardImages

            };
            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);

            await context.PostAsync(replyToConversation);
            context.Wait(MessageReceived);
        }

        protected string BuildReply(Action<StringBuilder> body)
        {
            var sb = new StringBuilder();
            body(sb);
            return sb.ToString();
        }

        protected async Task<Activity> BuildBingReply(string text)
        {
            dynamic res = await BingSearch.CallBingSearch(text);

            Activity replyToConversation = msg.CreateReply();
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction plButton = new CardAction()
            {
                Value = res.displayUrl,
                Type = "openUrl",
                Title = text
            };
            cardButtons.Add(plButton);
            ThumbnailCard plCard = new ThumbnailCard()
            {
                Title = res.name,
                Subtitle = res.snippet,
                //Images = cardImages,
                Buttons = cardButtons
            };
            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            return replyToConversation;
        }
    }
}