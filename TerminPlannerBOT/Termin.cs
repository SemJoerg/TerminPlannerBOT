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

        static private Emoji acceptEmoji;
        static private Emoji declinedEmoji;

        public List<SocketUser> AcceptedUserList { get; set; }
        public List<SocketUser> DeclinedUserList { get; set; }

        public Termin()
        {
            AcceptedUserList = new List<SocketUser>();
            DeclinedUserList = new List<SocketUser>();
            acceptEmoji = new Emoji("✅");
            declinedEmoji = new Emoji("❌");
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
        
        private Embed getTerminMessage()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Color = Program.defaultColor;
            embedBuilder.Title = $":calendar_spiral: `{Id}` {Name}";
            embedBuilder.AddField($"{Description ?? "No description Available"}", "\u200B");
            string acceptedUsers = "\u200B";
            string declinedUsers = "\u200B";


            
            foreach(SocketUser user in AcceptedUserList)
            {
                acceptedUsers += user.ToString() + "\n";
            }
            
            foreach(SocketUser user in DeclinedUserList)
            {
                declinedUsers += user.ToString() + "\n";
            }

            embedBuilder.AddField("__Accepted__", acceptedUsers, true);
            embedBuilder.AddField("__Declined__", declinedUsers, true);

            return embedBuilder.Build();
        }
        
        public async void UpdateTerminQuery(Server server, SocketTextChannel secondaryChannel)
        {
            try
            {
                ISocketMessageChannel messageChannel = Program._client.GetChannel(MessageChannelId) as ISocketMessageChannel;
                IUserMessage message = await messageChannel.GetMessageAsync(MessageId, CacheMode.AllowDownload) as IUserMessage;
                
                if(message.Embeds.Count != 0)
                {
                    message.ModifyAsync(msg => msg.Embed = Optional.Create<Embed>(getTerminMessage()));
                    
                }
                else
                {
                    Program.Log(message.IsSuppressed.ToString());
                    message.ModifySuppressionAsync(false);
                    message.ModifyAsync(msg => msg.Embed = Optional.Create<Embed>(getTerminMessage()));
                }
                
                return;
            }
            catch (NullReferenceException ex)
            {
                Program.Log(ex);
            }

            AcceptedUserList = new List<SocketUser>();
            DeclinedUserList = new List<SocketUser>();
            Discord.Rest.RestUserMessage restMessage;
            SocketTextChannel primaryChannel = server.GetTerminChannel();
            if (primaryChannel != null)
                restMessage = await primaryChannel.SendMessageAsync(embed: getTerminMessage());
            else if (secondaryChannel != null)
                restMessage = await secondaryChannel.SendMessageAsync(embed: getTerminMessage());
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

        static public Task AddedReaction(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Server server;
            Termin termin = GetTerminOfReaction(cacheableMessage, channel, out server);
            SocketUser user = Program._client.GetUser(reaction.UserId);

            if (termin == null || user == null || user.Id == Program._client.CurrentUser.Id)
                return Task.CompletedTask;
            
            if (reaction.Emote.Name == acceptEmoji.Name)
            {
                termin.AcceptedUserList.Add(user);
            }
            else if (reaction.Emote.Name == declinedEmoji.Name)
            {
                termin.DeclinedUserList.Add(user);
            }
            else
            {
                return Task.CompletedTask;
            }

            termin.UpdateTerminQuery(server, channel as SocketTextChannel);
            ServerHandler.SaveServer(server);
            return Task.CompletedTask;
        }
        
        static public Task RemovedReaction(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Server server;
            Termin termin = GetTerminOfReaction(cacheableMessage, channel, out server);
            SocketUser user = Program._client.GetUser(reaction.UserId);

            if (termin == null || user == null)
                return Task.CompletedTask;


            if (reaction.Emote.Name == acceptEmoji.Name)
            {
                termin.AcceptedUserList.Remove(user);
            }
            else if (reaction.Emote.Name == declinedEmoji.Name)
            {
                termin.DeclinedUserList.Remove(user);
            }
            else
            {
                return Task.CompletedTask;
            }

            termin.UpdateTerminQuery(server, channel as SocketTextChannel);
            ServerHandler.SaveServer(server);
            return Task.CompletedTask;
        }
    }
}
