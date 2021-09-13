using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TerminPlannerBOT
{
    [Serializable]
    public class Termin
    {
        private int id = 0;
        public int Id 
        {
            get { return id; }
            set
            {
                if (id != 0)
                    return;

                id = value;
            }
        }

        public ulong MessageChannelId { get; set; }
        public ulong MessageId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }

        static private Emoji acceptEmoji = new Emoji("✅");
        static private Emoji declinedEmoji = new Emoji("❌");

        public List<string> AcceptedUserList { get; set; }
        public List<string> DeclinedUserList { get; set; }

        public Termin()
        {
            AcceptedUserList = new List<string>();
            DeclinedUserList = new List<string>();
        }
        
        public bool ConvertToDateTime(string date, string time)
        {
            if (String.IsNullOrEmpty(date) && String.IsNullOrEmpty(time))
            {
                return false;
            }
            try
            {
                if (!String.IsNullOrEmpty(date) && String.IsNullOrEmpty(time))
                    Date = Convert.ToDateTime(date + Date.ToString(" HH:mm:ss"));

                else if (!String.IsNullOrEmpty(time) && String.IsNullOrEmpty(date))
                    Date = Convert.ToDateTime(Date.ToString("dd.MM.yyyy ") + time);

                else
                    Date = Convert.ToDateTime($"{date} {time}");

                return true;
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
            return false;
        }
        
        private Embed GetTerminMessageContent()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Color = Program.defaultColor;
            embedBuilder.Title = $":calendar_spiral: `{Id}` {Name}";
            embedBuilder.AddField($"{Description ?? "No description Available"}", $"__**Date:**__ {Date.ToShortDateString()}\n__**Time:**__ {Date.ToShortTimeString()}");
            string acceptedUsers = "\u200B";
            string declinedUsers = "\u200B";


            
            foreach(string user in AcceptedUserList)
            {
                acceptedUsers += user + "\n";
            }
            
            foreach(string user in DeclinedUserList)
            {
                declinedUsers += user + "\n";
            }

            embedBuilder.AddField("__Accepted__", acceptedUsers, true);
            embedBuilder.AddField("__Declined__", declinedUsers, true);

            return embedBuilder.Build();
        }
        
        private async Task<IUserMessage> GetTerminMessage()
        {
            ISocketMessageChannel messageChannel = Program._client.GetChannel(MessageChannelId) as ISocketMessageChannel;
            IUserMessage message = await messageChannel.GetMessageAsync(MessageId, CacheMode.AllowDownload, new RequestOptions() { RetryMode = RetryMode.RetryRatelimit }) as IUserMessage;
            return message;
        }

        public async void UpdateTerminQuery(Server server, SocketTextChannel secondaryChannel)
        {
            try
            {
                IUserMessage message = await GetTerminMessage();
                
                if(message.Embeds.Count != 0)
                {
                    message.ModifyAsync(msg => msg.Embed = Optional.Create<Embed>(GetTerminMessageContent()));
                    
                }
                else
                {
                    Program.Log(message.IsSuppressed.ToString());
                    message.ModifySuppressionAsync(false);
                    message.ModifyAsync(msg => msg.Embed = Optional.Create<Embed>(GetTerminMessageContent()));
                }
                
                return;
            }
            catch (NullReferenceException ex)
            {
                Program.Log(ex);
            }

            AcceptedUserList = new List<string>();
            DeclinedUserList = new List<string>();
            Discord.Rest.RestUserMessage restMessage;
            SocketTextChannel primaryChannel = server.GetTerminChannel();
            if (primaryChannel != null)
                restMessage = await primaryChannel.SendMessageAsync(embed: GetTerminMessageContent());
            else if (secondaryChannel != null)
                restMessage = await secondaryChannel.SendMessageAsync(embed: GetTerminMessageContent());
            else
                return;
            restMessage.AddReactionAsync(acceptEmoji, new RequestOptions());
            restMessage.AddReactionAsync(declinedEmoji, new RequestOptions());

            MessageChannelId = restMessage.Channel.Id;
            MessageId = restMessage.Id;
            ServerHandler.SaveServer(server);
        }

        
        static public Task MessageUpdated(Cacheable<IMessage, ulong> cacheable, SocketMessage message, ISocketMessageChannel channel)
        {
            Server server = ServerHandler.LoadServer((channel as SocketGuildChannel).Guild.Id);

            foreach (Termin termin in server.Termins)
            {
                if (termin.MessageId == message.Id && message.IsSuppressed)
                {
                    termin.UpdateTerminQuery(server, channel as SocketTextChannel);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
        
        //Reactions
        static private Termin GetTerminOfReaction(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, out Server server)
        {
            server = ServerHandler.LoadServer((channel as SocketGuildChannel).Guild.Id);
            foreach (Termin termin in server.Termins)
            {
                if (cacheableMessage.Id == termin.MessageId)
                {
                    return termin;
                }
            }
            return null;
        }

        static async public Task AddedReaction(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Server server;
            Termin termin = GetTerminOfReaction(cacheableMessage, channel, out server);
            Discord.Rest.RestUser user = await Program.restClient.GetUserAsync(reaction.UserId);

            if (termin == null || user == null || user.Id == Program._client.CurrentUser.Id)
                return;
            
            if (reaction.Emote.Name == acceptEmoji.Name)
            {
                foreach(string userName in termin.AcceptedUserList)
                {
                    if (userName == user.ToString())
                        return;
                }
                termin.AcceptedUserList.Add(user.ToString());
            }
            else if (reaction.Emote.Name == declinedEmoji.Name)
            {
                foreach (string userName in termin.DeclinedUserList)
                {
                    if (userName == user.ToString())
                        return;
                }
                termin.DeclinedUserList.Add(user.ToString());
            }
            else
            {
                return;
            }

            termin.UpdateTerminQuery(server, channel as SocketTextChannel);
            ServerHandler.SaveServer(server);
            return;
        }
        
        static async public Task RemovedReaction(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Server server;
            Termin termin = GetTerminOfReaction(cacheableMessage, channel, out server);
            string userName = (await Program.restClient.GetUserAsync(reaction.UserId)).ToString();
            

            if (termin == null || string.IsNullOrEmpty(userName))
                return;

            if (reaction.Emote.Name == acceptEmoji.Name)
            {
                termin.AcceptedUserList.Remove(userName);
            }
            else if (reaction.Emote.Name == declinedEmoji.Name)
            {
                termin.DeclinedUserList.Remove(userName);
            }
            else
            {
                return;
            }

            termin.UpdateTerminQuery(server, channel as SocketTextChannel);
            ServerHandler.SaveServer(server);
            return;
        }

        public async Task RemoveReactions()
        {
            AcceptedUserList = new List<string>();
            DeclinedUserList = new List<string>();
            IUserMessage message = await GetTerminMessage();
            await message.RemoveAllReactionsAsync();
            await message.AddReactionAsync(acceptEmoji, new RequestOptions());
            await message.AddReactionAsync(declinedEmoji, new RequestOptions());
        }
    }
}
