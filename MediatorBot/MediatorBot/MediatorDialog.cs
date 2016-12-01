using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

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
            await context.PostAsync($"You said {msg.Text}");
            context.Wait(ProcessMessage);
        }
    }
}