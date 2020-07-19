using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Activities.aZaaS
{
    public sealed class JdbcUrlHelper
    {

        public static string GetDatabase(string jdbcUrl)
        {
            /* jdbc:sqlserver://sqlserver:1433;database=MsDemo_ProductManagement */
            if (string.IsNullOrWhiteSpace(jdbcUrl))
                throw new ArgumentNullException(nameof(jdbcUrl));
            if (!jdbcUrl.StartsWith("jdbc:"))
                throw new ArgumentException("Invalid jdbc url format");

            return jdbcUrl.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        public static JdbcPart Extract(string jdbcUrl)
        {
            /* jdbc:sqlserver://sqlserver:1433;database=MsDemo_ProductManagement;user=sa;password=K2pass!123 */

            if (string.IsNullOrWhiteSpace(jdbcUrl))
                throw new ArgumentNullException(nameof(jdbcUrl));
            if (!jdbcUrl.StartsWith("jdbc:"))
                throw new ArgumentException("Invalid jdbc url format");

            var jdbcPart = new JdbcPart();
            var parts = jdbcUrl.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

            var leadParts = parts.First().Replace("//", string.Empty).Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            jdbcPart.Driver = leadParts[1];
            jdbcPart.Server = leadParts[2];
            jdbcPart.Port = int.Parse(leadParts[3]);

            var propDict = parts.Skip(1).ToList().Select(item =>
             {
                 var itemParts = item.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                 return new KeyValuePair<string, string>(itemParts.First(), itemParts.Last());
             }).ToDictionary(item => item.Key, item => item.Value);

            string database = string.Empty, username = string.Empty, password = string.Empty;
            if (propDict.TryGetValue("database", out database))
                jdbcPart.Database = database;
            if (propDict.TryGetValue("user", out username))
                jdbcPart.Username = username;
            if (propDict.TryGetValue("password", out password))
                jdbcPart.Password = password;

            return jdbcPart;
        }
    }

    public class JdbcPart
    {
        public string Driver { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
