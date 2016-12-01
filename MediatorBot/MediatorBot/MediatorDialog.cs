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
            //Stats and graph
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
                var uri = await ConversationState.GetGraph();
                var repl = context.MakeMessage();
                repl.Text = "Please find current graph of sentiments";
                repl.Attachments = new Attachment[] {
                    new Attachment(contentType: "image/jpeg", 
                    contentUrl: uri, thumbnailUrl: uri) };
                await context.PostAsync(repl);
            }
            else
            {
                //await context.PostAsync("");
            }
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