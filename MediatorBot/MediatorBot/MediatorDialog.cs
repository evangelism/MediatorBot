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

namespace MediatorBot
{
    [Serializable]
    public class MediatorDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(ProcessMessage);
        }

        private async Task ProcessMessage(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var msg = await result;
            if (msg.Text != "!users" && msg.Text != "!stats")
            {
                await ConversationState.RegisterMessage(msg.From.Name, msg.Text);
            }

            var badSentimenCheck = ConversationState.Users.Where((u => u.Sentiment < 0.4 && u.Sentiment != 0));
            if (badSentimenCheck.Any())
            {
                ConversationState.TextAnalysisDocumentStore phrasesDoc = await ConversationState.GetPhrasesforConversation();

                await context.PostAsync(BuildReply(
                sb =>
                {
                    foreach (ConversationState.TextAnalysisDocument doc in phrasesDoc.documents)
                    {
                        foreach (string x in doc.keyPhrases)
                        {
                            sb.AppendLine($"Phrase: {x}");
                        }
                    }
                   }));
            }

            if (msg.Text == "!users")
            {
                await context.PostAsync(BuildReply(
                    sb =>
                    {
                        ConversationState.Users.ForEach(x => sb.AppendLine(x.name));
                    }));
            }
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
                await context.PostAsync("");
            }
            context.Wait(ProcessMessage);
        }

        protected string BuildReply(Action<StringBuilder> body)
        {
            var sb = new StringBuilder();
            body(sb);
            return sb.ToString();
        }
    }
}