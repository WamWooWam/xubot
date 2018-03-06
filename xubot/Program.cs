﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Discord;
using Discord.Net.WebSockets;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Audio;
using Discord.Net.Providers.WS4Net;
using RedditSharp;
using Tweetinvi;
using System.Xml.Linq;
using System.Linq;
using System.Xml;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Generic;

namespace xubot
{
    public class Program : ModuleBase
    {
        public static CommandService xuCommand;
        public static DiscordSocketClient xuClient;

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        public static string prefix = "[>";

        public static BotWebAgent webAgent;
        public static Reddit reddit;
        public static RedditSharp.Things.Subreddit subreddit;

        public string xu_token = ""; // Token
        public static dynamic keys;
        public static dynamic apiJson;
        public static JToken perserv;
        public static dynamic perserv_parsed;
        public static bool enableNSFW = false;

        public async Task Start()
        {
            Console.SetWindowSize(80, 25);

            xuClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Warning
            });
            xuCommand = new CommandService();

            //ImageTypeReader itr = new ImageTypeReader();
            //xuCommand.AddTypeReader<Image>(itr);
            string text;
            using (var sr = new StreamReader("Keys.json"))
            {
                text = sr.ReadToEnd();
            }

            keys = JObject.Parse(text);

            using (var sr = new StreamReader("API.json"))
            {
                text = sr.ReadToEnd();
            }

            apiJson = JObject.Parse(text);

            await commandInitiation();
            await readMessages();

            xuClient.Log += (message) =>
            {
                Console.Write($"{message.ToString()}\n");
                return Task.Delay(0);
            };

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;

            Console.Write("- - - - - - - - - - - - - - - - - setup stuffs - - - - - - - - - - - - - - - - -");
            Console.Write("                                                                                ");
            Console.Write("* setting up bot web agent for reddit use                                       ");
            webAgent = new BotWebAgent(keys.reddit.user.ToString(), keys.reddit.pass.ToString(), keys.reddit.key1.ToString(), keys.reddit.key2.ToString(), "https://www.reddit.com/api/v1/authorize?client_id=CLIENT_ID&response_type=TYPE& state=RANDOM_STRING&redirect_uri=URI&duration=DURATION&scope=SCOPE_STRING");

            Console.Write("* setting up reddit client                                                      ");
            reddit = new Reddit(webAgent, true);

            Console.Write("* setting up default subreddit of /r/xubot_subreddit                            ");
            subreddit = reddit.GetSubreddit("/r/xubot_subreddit");

            Console.Write("* setting up discord connection: login                                          ");
            if (keys.use.ToString() == "dev")
            {
                Console.Write("  > oh, i've been told to use the dev token, doing than then!                   ");
                prefix = "d>";
                await xuClient.LoginAsync(TokenType.Bot, keys.discord_dev.ToString());
            }
            else
            {
                await xuClient.LoginAsync(TokenType.Bot, keys.discord.ToString());
            }
            Console.Write("* setting up discord connection: starting client                                ");
            await BeginStart();

            xuClient.Ready += ClientReady;
            xuClient.UserJoined += XuClient_UserJoined;
            Console.WriteLine();

            await Task.Delay(-1);
        }

        public Task XuClient_UserJoined(SocketGuildUser arg)
        {
            return Task.CompletedTask;
        }

        private static async Task BeginStart()
        {
            await xuClient.StartAsync();
        }

        public async Task ClientReady()
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("                                                                                ");
            Console.Write("                                    ready!!!                                    ");
            Console.Write("                                                                                ");

            await xuClient.SetGameAsync("xubot is alive!");
            Console.Beep();

            RefreshPerServ();
        }

        public async Task readMessages()
        {
            xuClient.MessageReceived += (message) =>
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"[{message.Timestamp}] {message.Author}: {message.Content}");

                return Task.Delay(0);
            };

            //console logs hhheeerrrrrrrr
            xuClient.Connected += XuClient_Connected;
            xuClient.Disconnected += XuClient_Disconnected;

            xuClient.LoggedIn += XuClient_LoggedIn;
            xuClient.LoggedOut += XuClient_LoggedOut;

            xuClient.JoinedGuild += XuClient_JoinedGuild;
            xuClient.LeftGuild += XuClient_LeftGuild;
            xuClient.GuildAvailable += XuClient_GuildAvailableAsync;
        }

        private Task XuClient_GuildAvailable(SocketGuild arg)
        {
            Console.WriteLine("guild list: " + arg.Name);
            return Task.CompletedTask;
        }

        private Task XuClient_JoinedGuild(SocketGuild arg)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();
            Console.Write("                                                                                ");
            Console.Write("                               added to a guild!!                               ");
            Console.Write("                                                                                ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("the guild name: " + arg.Name);
            Console.WriteLine();
            return Task.CompletedTask;
        }

        private Task XuClient_LeftGuild(SocketGuild arg)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();
            Console.Write("                                                                                ");
            Console.Write("                               left a guild... :<                               ");
            Console.Write("                   probably got kicked/banned which is rude.                    ");
            Console.Write("                                                                                ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("the guild name: " + arg.Name);
            Console.WriteLine();
            return Task.CompletedTask;
        }

        private Task XuClient_LoggedIn()
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();
            Console.Write("                                                                                ");
            Console.Write("                                   logged in!                                   ");
            Console.Write("                                                                                ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();
            return Task.CompletedTask;
        }

        private Task XuClient_LoggedOut()
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("                                                                                ");
            Console.Write("                                  logged out.                                   ");
            Console.Write("                                                                                ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();
            return Task.CompletedTask;
        }

        private Task XuClient_Connected()
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("                                                                                ");
            Console.Write("                              connection successful!                            ");
            Console.Write("                                                                                ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();



            return Task.CompletedTask;
        }

        private Task XuClient_Disconnected(Exception arg)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("                                                                                ");
            Console.Write("                                connection lost...                              ");
            Console.Write("                               probably got dropped                             ");
            Console.Write("                                                                                ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine();
            Console.WriteLine("Exception logged at: " + Environment.CurrentDirectory + "\\Exceptions\\latest.txt");
            Console.Beep();
            Console.Beep();
            Console.Beep();

            System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "\\Exceptions\\");
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\Exceptions\\latest.txt", arg.ToString());

            Thread.Sleep(2500);

            //Console.Read();

            //await BeginStart();

            return Task.CompletedTask;
        }

        public async Task XuClient_GuildAvailableAsync(Discord.WebSocket.SocketGuild arg)
        {
            Console.WriteLine("guild list: " + arg.Name);

            var xdoc = XDocument.Load("PerServTrigg.xml");

            var items = from i in xdoc.Descendants("server")
                        select new
                        {
                            guildid = i.Attribute("id"),
                            onwake = i.Attribute("onwake")
                        };

            foreach (var item in items)
            {
                if (item.guildid.Value == arg.Id.ToString())
                {
                    if (item.onwake.Value != "")
                    {
                        await arg.DefaultChannel.SendMessageAsync(item.onwake.Value);
                    }
                }
            }

        }

        public async Task commandInitiation()
        {
            xuClient.MessageReceived += handleCommands;
            await xuCommand.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task handleCommands(SocketMessage messageParameters)
        {
            var message = messageParameters as SocketUserMessage;
            if (message == null) return;

            int argumentPosition = 0;

            if (!(message.HasStringPrefix(prefix, ref argumentPosition) || message.HasMentionPrefix(xuClient.CurrentUser, ref argumentPosition)))
                return;

            var context = new CommandContext(xuClient, message);

            var result = await xuCommand.ExecuteAsync(context, argumentPosition);
            if (!result.IsSuccess)
            {
                EmbedBuilder embedd = new EmbedBuilder
                {
                    Title = "Error!",
                    Color = Discord.Color.Red,
                    Description = "***That's a bad!!!***",

                    Footer = new EmbedFooterBuilder
                    {
                        Text = "xubot :p"
                    },
                    Timestamp = DateTime.UtcNow,
                    Fields = new List<EmbedFieldBuilder>()
                        {
                            new EmbedFieldBuilder
                            {
                                Name = "Error Reason",
                                Value = "```" + result.ErrorReason + "```",
                                IsInline = false
                            },
                            new EmbedFieldBuilder
                            {
                                Name = "What it is",
                                Value = "```" + result.Error + "```",
                                IsInline = false
                            }

                        }
                };

                await context.Channel.SendMessageAsync("", false, embedd);
            }
        }

        public static void RefreshPerServ()
        {
            string text;
            using (var sr = new StreamReader("PerServerTrigg.json"))
            {
                text = sr.ReadToEnd();
            }

            perserv = JObject.Parse(text);
            perserv_parsed = JObject.Parse(text);
            text = null;
        }

        public static bool ServerTrigger_Detect(ICommandContext Context)
        {
            RefreshPerServ();
            try
            {
                string guild = Context.Guild.Id.ToString() + "_onwake";
                return (perserv.Value<String>(guild).ToString() ?? "") == "";
            }
            catch (Exception fuck)
            {
                Context.Channel.SendMessageAsync(fuck.ToString());
                return false;
            }
        }
    }
}