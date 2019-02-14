using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ValentineBot
{
    public class Config
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("api_secret")]
        public string ApiSecret { get; set; }
        [JsonProperty("debug_channel_id")]
        public ulong DebugChannelId { get; set; }
        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }
        [JsonProperty("faction_id")]
        public ulong FactionId { get; set; }
        [JsonProperty("file_url")]
        public string FileUrl { get; set; }
        [JsonProperty("embed_color")]
        public string EmbedColor { get; set; }
        [JsonProperty("debug")]
        public bool Debug { get; set; }
        [JsonProperty("simulate")]
        public bool Simulate { get; set; }
        [JsonProperty("debug_role_ids")]
        public List<ulong> DebugRoleIds { get; set; }
        [JsonProperty("debug_single_send_id")]
        public ulong DebugSingleSendId { get; set; }
    }
}
