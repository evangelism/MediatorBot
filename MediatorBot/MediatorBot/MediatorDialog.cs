using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MediatorBot
{
    [Serializable]
    public class MediatorDialog : IDialog<string>
    {

        public float SentimentScore = 0;
        public List<string> transcript = new List<string>();

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            await context.PostAsync($"You said:{message.Text}");
            if (message.Text=="!list")
            {
                var b = new StringBuilder();
                transcript.ForEach(x => b.AppendLine(x));
                await context.PostAsync(b.ToString());
                transcript.Clear();
            }
        }
    }
}