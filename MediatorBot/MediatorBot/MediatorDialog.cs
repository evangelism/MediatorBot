using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Text;
using MediatorLib;

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
            ConversationState.RegisterMessage(msg.From.Name,msg.Text);
            if (msg.Text=="!users")
            {
                await context.PostAsync(BuildReply(
                    sb =>
                    {
                        ConversationState.Users.ForEach(x => sb.AppendLine(x.name));
                    }));
            }
            else if (msg.Text=="!stats")
            {
                await context.PostAsync(BuildReply(
                    sb =>
                    {
                        foreach(var x in ConversationState.Users)
                        { sb.AppendLine($"{x.name}: msgs={x.MessageCount}, sentiment={x.Sentiment}"); }
                    }));
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