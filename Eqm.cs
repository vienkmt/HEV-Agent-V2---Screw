
namespace HEV_Agent_V2
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class Eqm
    {
        [JsonProperty("LastTime", NullValueHandling = NullValueHandling.Ignore)]
        public string LastTime { get; set; }

        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("Status", NullValueHandling = NullValueHandling.Ignore)]
        public int Status { get; set; }

        [JsonProperty("ErrorCode", NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorCode { get; set; }

        [JsonProperty("Note", NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }

        [JsonProperty("Ip", NullValueHandling = NullValueHandling.Ignore)]
        public string Ip { get; set; }
    }

    public partial class Eqm
    {
        public static List<Eqm> FromJson(string json) => JsonConvert.DeserializeObject<List<Eqm>>(json, HEV_Agent_V2.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this List<Eqm> self) => JsonConvert.SerializeObject(self, HEV_Agent_V2.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
