using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using mitoSoft.Workflows.Editor.Helpers.Enums;

namespace mitoSoft.Workflows.Editor
{
    public class AppSettings
    {
        public class AppearanceSettings
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public Themes Theme { get; set; }
        }

        public AppearanceSettings Appearance { get; set; }
    }
}