using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Estandartes.Core.Mods
{
    /// <summary>Configuración JSON común: propiedades en camelCase, enums como texto, claves de diccionario intactas.</summary>
    public static class Json
    {
        public static readonly JsonSerializerSettings Settings = Create();

        private static JsonSerializerSettings Create()
        {
            var s = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false }
                },
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Error, // un campo mal escrito es un error, no se ignora
                MaxDepth = 64,
                Formatting = Formatting.Indented,
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        }

        public static JsonSerializer Serializer => JsonSerializer.Create(Settings);

        public static T Deserialize<T>(string text) => JsonConvert.DeserializeObject<T>(text, Settings);
        public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
    }
}
