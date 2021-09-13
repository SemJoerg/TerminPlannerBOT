using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TerminPlannerBOT.Commands
{
    [NamedArgumentType]
    public class OptionalTerminAddArguments
    {
        public string Description { get; set; }
    }

    [NamedArgumentType]
    public class OptionalTerminModifyArguments
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }

    [Group("termin")]
    public class TerminModule : ModuleBase<SocketCommandContext>
    {
        readonly Discord.WebSocket.DiscordSocketClient _client;
        readonly CommandService _commandService;
        Server _server;

        public TerminModule(Discord.WebSocket.DiscordSocketClient client, CommandService commandService, Server server)
        {
            _client = client;
            _commandService = commandService;
            _server = server;
        }

        private void GetTerminField(EmbedBuilder embedBuilder, Termin termin)
        {
            
            string fieldContent = $">>> **Description:** `{termin.Description ?? "No termin description available."}`\n" +
                $"**Date:** `{termin.Date.ToString("dd.MM.yyyy")}`\n**Time:** `{termin.Date.ToString("HH:mm")}`";
            embedBuilder.AddField($":calendar_spiral:  `{termin.Id}`  **{termin.Name}**", fieldContent);
        }
        
        [Command("list")]
        [Summary("Lists all termins of the server")]
        public Task List()
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Program.defaultColor;

            EmbedFooterBuilder foot = new EmbedFooterBuilder();
            foot.Text = $"{_server.Termins.Count}/20 termins";
            embed.Footer = foot;
            foreach (Termin termin in _server.Termins)
            {
                GetTerminField(embed, termin);
            }
            ReplyAsync(embed: embed.Build());
            return Task.CompletedTask;
        }

        [Command("add")]
        [Summary("<name> <date&time> [__optional parameter__]|*description: <description>*\n\nAdds a termin")]
        public Task AddTermin(string name, string date, string time, OptionalTerminAddArguments namedArgs = null)
        {   
            Termin termin = new Termin();

            termin.Name = name;
            if(!termin.ConvertToDateTime(date, time))
            {
                ReplyAsync(embed: Program.BuildSimpleEmbed(":x: Error", "Invalid date or time format."));
                return Task.CompletedTask;
            }

            if(namedArgs != null)
            {
                termin.Description = namedArgs.Description;
            }

            _server.AddTermin(termin);
            ServerHandler.SaveServer(_server);

            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Program.defaultColor;
            GetTerminField(embed, termin);

            ReplyAsync(embed: embed.Build());
            termin.UpdateTerminQuery(_server, Context.Channel as SocketTextChannel);
            return Task.CompletedTask;
        }

        [Command("modify")]
        [Summary("<id> [__optional parameter__]|*name: <name>\ndescription: <description>\ndateandtime: <date&time>*\n\nModifys a existing termin")]
        public Task ModifyTermin(int id, OptionalTerminModifyArguments namedArgs)
        {
            string replyMessageText = null;
            Termin termin;
            if(!_server.GetTermin(id, out termin))
            {
                ReplyAsync($"There is no termin with id **{id}**");
                return Task.CompletedTask;
            }

            termin.Name = namedArgs.Name ?? termin.Name;
            termin.Description = namedArgs.Description ?? termin.Description;

            if (!String.IsNullOrEmpty(namedArgs.Date) || !String.IsNullOrEmpty(namedArgs.Time))
            {
                if (!termin.ConvertToDateTime(namedArgs.Date, namedArgs.Time))
                {
                    replyMessageText = "Incorrect date or time format!";
                }
            }

            ServerHandler.SaveServer(_server);

            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Program.defaultColor;
            GetTerminField(embed, termin);
            ReplyAsync(replyMessageText, embed: embed.Build());
            termin.UpdateTerminQuery(_server, Context.Channel as SocketTextChannel);
            return Task.CompletedTask;
        }

        [Command("remove")]
        [Summary("<terminId>|removes a termin")]
        public Task RemoveTermin(int terminId)
        {

            if(_server.RemoveTermin(terminId))
            {
                ServerHandler.SaveServer(_server);
                ReplyAsync(embed: Program.BuildSimpleEmbed($"Deleted termin with id **{terminId}**"));
            }
            else
            {
                ReplyAsync(embed: Program.BuildSimpleEmbed($"There is no termin with id `{terminId}`"));
            }
            return Task.CompletedTask;
        }

        [Command("reset")]
        [Summary("<terminId>|resets the reactions of a termin")]
        public Task ResetTermin(int terminId)
        {
            Termin termin;
            if(_server.GetTermin(terminId, out termin))
            {
                termin.RemoveReactions();
                termin.UpdateTerminQuery(_server, Context.Channel as SocketTextChannel);
                ServerHandler.SaveServer(_server);
                ReplyAsync(embed: Program.BuildSimpleEmbed($"Reset termin with id **{terminId}**"));
            }
            else
            {
                ReplyAsync(embed: Program.BuildSimpleEmbed($"There is no termin with id `{terminId}`"));
            }

            return Task.CompletedTask;
        }
    }
}
