using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ValentineBot
{
    public class Bot
    {
        private Config _config;
        private DiscordSocketClient _client;
        private CancellationTokenSource _cancellationToken;
        private bool _firstLoad = true;
        private BlockingCollection<Discord.LogMessage> _loggingQueue = new BlockingCollection<LogMessage>();
        private Dictionary<string, Action<SocketMessage>> _actionsDictionary = new Dictionary<string, Action<SocketMessage>>();

        public Bot(Config config, CancellationTokenSource cancellationToken)
        {
            _config = config;
            _cancellationToken = cancellationToken;
        }

        public async Task Start()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            _actionsDictionary = new Dictionary<string, Action<SocketMessage>>()
            {
                { "!start", m => Task.Run(() => Send().ContinueWith(t => m.Channel.SendMessageAsync(t.Result.Item1 ? "Sending messages completed." : $"There were some problems while sending the messages. Sent {t.Result.Item2} messages.")))},
                { "!stop", async m => await Stop() },
                { "!toggleDebug", m => {_config.Debug = !_config.Debug; m.Channel.SendMessageAsync($"Debug status changed to {_config.Debug.ToString()}"); } },
                { "!toggleSimulate", m => {_config.Simulate = !_config.Simulate; m.Channel.SendMessageAsync($"Simulate status changed to {_config.Simulate.ToString()}"); } },
            };
            Task.Run(() => LoggingQueue(_cancellationToken));

            _client.MessageReceived += _client_MessageReceived;
            _client.Log += _client_Log;
            _client.Ready += _client_Ready;

            await _client.LoginAsync(Discord.TokenType.Bot, _config.ApiSecret);
            await _client.StartAsync();
            
            while (true)
            {
                await Task.Delay(500);
                if (_cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task _client_Ready()
        {
            if (_firstLoad)
            {
                _firstLoad = false;
                if (_client.GetChannel(_config.DebugChannelId) is SocketTextChannel debugChannel)
                {
                    await debugChannel.SendMessageAsync($"Bot started with options:\r\nDebug: {_config.Debug.ToString()}\r\nSimulate:{_config.Simulate.ToString()}");
                }
                
            }
        }

        private async Task LoggingQueue(CancellationTokenSource cancellationToken)
        {
            LogMessage message = default(LogMessage);
            while(_loggingQueue.TryTake(out message, Timeout.Infinite, cancellationToken.Token))
            {
                if (cancellationToken.IsCancellationRequested) return;
                await System.IO.File.AppendAllTextAsync($"{_config.Name}Bot.log", $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - {Enum.GetName(typeof(Discord.LogSeverity), message.Severity)}: {message.Message}{Environment.NewLine}", cancellationToken.Token);
            }
        }

        private async Task _client_Log(Discord.LogMessage arg)
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - {Enum.GetName(typeof(Discord.LogSeverity), arg.Severity)}: {arg.Message} {arg.Exception?.ToString()}");
            _loggingQueue.TryAdd(arg);
        }

        private async Task _client_MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id != _config.DebugChannelId) return;
            if (_actionsDictionary.ContainsKey(message.Content))
            {
                _actionsDictionary[message.Content](message);
            }         
        }

        public async Task Stop()
        {
            await _client.StopAsync();
            _cancellationToken.Cancel();
        }

        public async Task<(bool, int)> Send()
        {
            var success = true;
            var messagesSent = 0;
            var guild = _client.GetGuild(_config.GuildId);
            var users = guild.Users;
            Func<SocketGuildUser, bool> filter = x => x.Roles.Any(r => r.Id == _config.FactionId);
            if (_config.Debug)
            {
                if (_config.DebugSingleSendId > 0)
                {
                    filter = x => x.Id == _config.DebugSingleSendId;
                }
                else
                {
                    filter = x => x.Roles.Any(r => r.Id == _config.FactionId) && x.Status == UserStatus.Online && x.Roles.Any(r => _config.DebugRoleIds.Contains(r.Id));
                }
            }
            var usersToSend = users.Where(filter).ToList();
            foreach (var user in usersToSend)
            {
                if (await SendMessageInner(user))
                {
                    messagesSent++;
                }
                else
                {
                    success = false;
                }
            }
            return (success, messagesSent);
        }

        private async Task<bool> SendMessageInner(SocketGuildUser user)
        {
            var embedBuilder = new EmbedBuilder
            {
                Title = $"Happy Valentine's Day, {user.Username}!",
                Description = "From the r/MadeInAbyss community.",
                ImageUrl = _config.FileUrl,
                Color = new Color(Convert.ToUInt32(_config.EmbedColor, 16))
            };
            try
            {
                IUserMessage message = null;
                if (!_config.Simulate)
                {
                    message = await user.SendMessageAsync(null, false, embedBuilder.Build(), null);
                }
                else
                {
                    message = new Mocks.MockUserMessage();
                }
                if (message != null)
                {
                    await _client_Log(new LogMessage(LogSeverity.Info, "Inner", $"Message sent to {user.Username} ({user.Id})"));
                }
                else
                {
                    await _client_Log(new LogMessage(LogSeverity.Warning, "Inner", $"Failed to send message to {user.Username} ({user.Id}): null return message"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _client_Log(new LogMessage(LogSeverity.Warning, "Inner", $"Failed to send message to {user.Username} ({user.Id}): {ex.Message}"));
                return false;
            }
            return true;
        }
    }
}
