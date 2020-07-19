using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using IdentityModel.Client;

namespace Elsa.Activities.aZaaS
{
    public sealed class AccessToken
    {
        private readonly static object _syncLock = new object();
        private readonly static Dictionary<string, TokenItem> _accessTokens = new Dictionary<string, TokenItem>();


        public static Task<string> GetTokenAsync(ApiClient apiClient, string serviceName = "sparkApiService")
        {
            if (apiClient == null)
                throw new ArgumentNullException(nameof(apiClient));

            var allowedServiceNames = new string[] { "sparkApiService", "sqoopApiService" };
            if (!allowedServiceNames.Contains(serviceName))
                throw new NotSupportedException("Invalid service name");

            return GetTokenAsync(apiClient.Authority, apiClient.ClientId, apiClient.ClientSecret, serviceName);
        }



        public static async Task<string> GetTokenAsync(string authServer, string clientId = "client_app", string clientSecret = "secret", string allowedScopes = "sparkApiService", bool newToken = false)
        {
            var itemKey = $"{authServer}-{allowedScopes}";
            var tokenItem = GetTokenItem(itemKey);
            if (newToken || tokenItem == null || tokenItem.Expired)
            {
                var client = new HttpClient();
                var disco = await client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest()
                {
                    Address = authServer,
                    Policy = { RequireHttps = false }
                });
                if (disco.IsError)
                    throw new InvalidOperationException(disco.Error);

                var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,

                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = allowedScopes
                    //TODO:
                });

                if (response.IsError)
                    throw new InvalidOperationException(response.Error);

                var accessToken = response.AccessToken;
                tokenItem = CacheTokenItem(itemKey, accessToken, response.ExpiresIn);
            }

            return tokenItem.AccessToken;
        }

        private static TokenItem GetTokenItem(string itemKey)
        {
            TokenItem item = null;
            itemKey = itemKey.ToLower();

            if (_accessTokens.ContainsKey(itemKey))
                item = _accessTokens[itemKey];

            return item;
        }
        private static TokenItem CacheTokenItem(string itemKey, string accessToken, int expiration)
        {
            itemKey = itemKey.ToLower();
            var tokenItem = new TokenItem(accessToken, expiration);

            if (_accessTokens.ContainsKey(itemKey))
                _accessTokens[itemKey] = tokenItem;
            else
                _accessTokens.Add(itemKey, tokenItem);

            return tokenItem;
        }

        class TokenItem
        {
            public string AccessToken { get; set; }
            public DateTime UpdatedTime { get; set; }
            public int Expiration { get; set; }

            public bool Expired
            {
                get
                {
                    return (DateTime.UtcNow - this.UpdatedTime).Seconds > Expiration;
                }
            }

            public TokenItem(string acessToken, int expiration)
            {
                this.AccessToken = acessToken;
                this.UpdatedTime = DateTime.UtcNow;
                this.Expiration = expiration;
            }
        }
    }

    public class ApiClient
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}