using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HermesProxy.Network.BattleNet.REST
{
    public static class Forms
    {
        /// <summary>
        /// Returns a <see cref="FormInputs"/> instance.
        /// </summary>
        public static FormInputs GetFormInput()
        {
            var formInputs = new FormInputs
            {
                Type = "LOGIN_FORM"
            };

            formInputs.Inputs.Add(new()
            {
                ID = "account_name",
                Type = "text",
                Label = "E-mail",
                MaxLength = 320
            });

            formInputs.Inputs.Add(new()
            {
                ID = "password",
                Type = "passowrd",
                Label = "Password",
                MaxLength = 16
            });

            formInputs.Inputs.Add(new()
            {
                ID = "log_in_submit",
                Type = "submit",
                Label = "Log In",
            });

            return formInputs;
        }
    }

    public class FormInputs
    {
        [JsonPropertyName("inputs")]
        public List<FormInput> Inputs { get; set; } = new();

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class FormInput
    {
        [JsonPropertyName("input_id")]
        public string ID { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("max_length")]
        public int MaxLength { get; set; }
    }

    public class FormInputValue
    {
        [JsonPropertyName("input_id")]
        public string ID { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class LogonData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("program_id")]
        public string Program { get; set; }

        [JsonPropertyName("platform_id")]
        public string Platform { get; set; }

        [JsonPropertyName("inputs")]
        public List<FormInputValue> Inputs { get; set; } = new();

        public string GetDataFromId(string data)
        {
            return Inputs.FirstOrDefault(x => x.ID == data)?.Value ?? string.Empty;
        }
    }

    public class LogonResult
    {
        [JsonPropertyName("authentication_state")]
        public string AuthenticationState { get; set; }

        [JsonPropertyName("login_ticket")]
        public string LoginTicket { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("support_error_code")]
        public string SupportErrorCode { get; set; }

        [JsonPropertyName("authenticator_form")]
        public FormInputs AuthenticatorForm { get; set; } = new();
    }

    public class ClientVersion
    {
        [JsonPropertyName("versionBuild")]
        public int Build { get; set; }

        [JsonPropertyName("versionMajor")]
        public int Major { get; set; }

        [JsonPropertyName("versionMinor")]
        public int Minor { get; set; }

        [JsonPropertyName("versionRevision")]
        public int Revision { get; set; }
    }

    public class RealmListTicketInformation
    {
        [JsonPropertyName("platform")]
        public int Platform { get; set; }

        [JsonPropertyName("buildVariant")]
        public string BuildVariant { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("timeZone")]
        public string Timezone { get; set; }

        [JsonPropertyName("currentTime")]
        public int CurrentTime { get; set; }

        [JsonPropertyName("textLocale")]
        public int TextLocale { get; set; }

        [JsonPropertyName("audioLocale")]
        public int AudioLocale { get; set; }

        [JsonPropertyName("versionDataBuild")]
        public int VersionDataBuild { get; set; }

        [JsonPropertyName("version")]
        public ClientVersion ClientVersion { get; set; } = new();

        [JsonPropertyName("secret")]
        public List<byte> Secret { get; set; }

        [JsonPropertyName("clientArch")]
        public int ClientArch { get; set; }

        [JsonPropertyName("systemVersion")]
        public string SystemVersion { get; set; }

        [JsonPropertyName("platformType")]
        public int PlatformType { get; set; }

        [JsonPropertyName("systemArch")]
        public int SystemArch { get; set; }
    }

    public class RealmListTicketClientInformation
    {
        [JsonPropertyName("info")]
        public RealmListTicketInformation Info { get; set; } = new();
    }

    public class RealmEntry
    {
        [JsonPropertyName("wowRealmAddress")]
        public int WowRealmAddress { get; set; }

        [JsonPropertyName("cfgTimezonesID")]
        public int CfgTimezonesID { get; set; }

        [JsonPropertyName("populationState")]
        public int PopulationState { get; set; }

        [JsonPropertyName("cfgCategoriesID")]
        public int CfgCategoriesID { get; set; }

        [JsonPropertyName("version")]
        public ClientVersion Version { get; set; } = new();

        [JsonPropertyName("cfgRealmsID")]
        public int CfgRealmsID { get; set; }

        [JsonPropertyName("flags")]
        public int Flags { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cfgConfigsID")]
        public int CfgConfigsID { get; set; }

        [JsonPropertyName("cfgLanguagesID")]
        public int CfgLanguagesID { get; set; }
    }

    public class RealmListUpdates
    {
        [JsonPropertyName("updates")]
        public IList<RealmListUpdate> Updates { get; set; } = new List<RealmListUpdate>();
    }

    public class RealmListUpdate
    {
        [JsonPropertyName("update")]
        public RealmEntry Update { get; set; } = new();

        [JsonPropertyName("deleting")]
        public bool Deleting { get; set; }
    }

    public class RealmCharacterCountList
    {
        [JsonPropertyName("counts")]
        public List<RealmCharacterCountEntry> Counts { get; set; } = new();
    }

    public class RealmCharacterCountEntry
    {
        [JsonPropertyName("wowRealmAddress")]
        public int WowRealmAddress { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class RealmListServerIPAddresses
    {
        [JsonPropertyName("families")]
        public List<AddressFamily> Families { get; set; } = new();
    }

    public class AddressFamily
    {
        [JsonPropertyName("family")]
        public int Id { get; set; }

        [JsonPropertyName("addresses")]
        public List<JsonAddress> Addresses { get; set; } = new();
    }

    public class JsonAddress
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("port")]
        public int Port { get; set; }
    }
}
