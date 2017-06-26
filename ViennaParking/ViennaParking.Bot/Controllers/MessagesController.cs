using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using ViennaParking.Bot.Dialogs;
using Autofac;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using ViennaParking.Bot.Helper;

namespace ViennaParking.Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        internal static IDialog<ShopSearch> MakeRootFormDialog()
        {
            return Chain.From(() => FormDialog.FromForm(new ParkingZoneCheck().BuildForm))
                .ContinueWith<ParkingZoneCheck, ShopSearch>(async (ctx, parkingZoneCheck) =>
                    {
                        var res = await parkingZoneCheck;
                        var shopSearch = new ShopSearch();
                        shopSearch.Address = res.Address;
                        shopSearch.VerifiedAddress = res.VerifiedAddress;
                        return FormDialog.FromForm(shopSearch.BuildForm);
                    })
                .Do(async (context, order) =>
                    {
                        try
                        {
                            var completed = await order;

                            await context.PostAsync("Bye, bye");
                        }
                        catch (FormCanceledException<ShopSearch> e)
                        {
                            string reply;
                            if (e.InnerException == null)
                            {
                                reply = $"You quit on {e.Last}--maybe you can finish next time!";
                            }
                            else
                            {
                                reply = "Sorry, I've had a short circuit.  Please try again.";
                            }
                            await context.PostAsync(reply);
                        }
                    }).Loop();
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                return await Conversation.SendAsync(message, () =>
                {
                    return ViennaParkingDialog.BotDialog
                        .Do(async (context, order) =>
                        {
                            try
                            {
                                var completed = await order;

                                await context.PostAsync("Bye, bye");
                            }
                            catch (FormCanceledException<ViennaParkingDialog> e)
                            {
                                string reply;
                                if (e.InnerException == null)
                                {
                                    reply = $"You quit on {e.Last}--maybe you can finish next time!";
                                }
                                else
                                {
                                    reply = "Sorry, I've had a short circuit.  Please try again.";
                                }
                                await context.PostAsync(reply);
                            }
                        });
                });

                //return await Conversation.SendAsync(message, MakeRootFormDialog);
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}