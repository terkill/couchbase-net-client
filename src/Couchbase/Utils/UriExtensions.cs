using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Couchbase.Core.Configuration.Server;

namespace Couchbase.Utils
{
    internal static class UriExtensions
    {
        public static string Http = "http";
        public static string Https = "https";

        public static string QueryPath = "/query";
        public const string AnalyticsPath = "/analytics/service";
        public static string BaseUriFormat = "{0}://{1}:{2}/pools";

        internal static Uri GetQueryUri(this NodeAdapter nodeAdapter, ClusterOptions clusterOptions)
        {
            if (nodeAdapter.IsQueryNode)
            {
                return new UriBuilder
                {
                    Scheme = clusterOptions.EnableTls ? Https : Http,
                    Host = nodeAdapter.Hostname,
                    Port = clusterOptions.EnableTls ? nodeAdapter.N1QlSsl : nodeAdapter.N1Ql,
                    Path = QueryPath
                }.Uri;
            }

            return new UriBuilder
            {
                Scheme = clusterOptions.EnableTls ? Https : Http,
                Host = nodeAdapter.Hostname,
            }.Uri;
        }

        internal static Uri GetAnalyticsUri(this NodeAdapter nodesAdapter, ClusterOptions clusterOptions)
        {
            if (nodesAdapter.IsAnalyticsNode)
            {
                return new UriBuilder
                {
                    Scheme = clusterOptions.EnableTls ? Https : Http,
                    Host = nodesAdapter.Hostname,
                    Port = clusterOptions.EnableTls ? nodesAdapter.AnalyticsSsl : nodesAdapter.Analytics,
                    Path = AnalyticsPath
                }.Uri;
            }
            return new UriBuilder
            {
                Scheme = clusterOptions.EnableTls ? Https : Http,
                Host = nodesAdapter.Hostname,
            }.Uri;

        }

        internal static Uri GetSearchUri(this NodeAdapter nodeAdapter, ClusterOptions clusterOptions)
        {
            if (nodeAdapter.IsSearchNode)
            {
                return new UriBuilder
                {
                    Scheme = clusterOptions.EnableTls ? Https : Http,
                    Host = nodeAdapter.Hostname,
                    Port = clusterOptions.EnableTls ? nodeAdapter.FtsSsl : nodeAdapter.Fts
                }.Uri;
            }

            return new UriBuilder
            {
                Scheme = clusterOptions.EnableTls ? Https : Http,
                Host = nodeAdapter.Hostname,
            }.Uri;
        }

        internal static Uri GetViewsUri(this NodeAdapter nodesAdapter, ClusterOptions clusterOptions)
        {
            if (nodesAdapter.IsKvNode)
            {
                return new UriBuilder
                {
                    Scheme = clusterOptions.EnableTls ? Https : Http,
                    Host = nodesAdapter.Hostname,
                    Port = clusterOptions.EnableTls ? nodesAdapter.ViewsSsl : nodesAdapter.Views
                }.Uri;
            }
            return new UriBuilder
            {
                Scheme = clusterOptions.EnableTls ? Https : Http,
                Host = nodesAdapter.Hostname,
                Port = clusterOptions.EnableTls ? nodesAdapter.ViewsSsl : nodesAdapter.Views
            }.Uri;
        }

        internal static Uri GetManagementUri(this NodeAdapter nodesAdapter, ClusterOptions clusterOptions)
        {
            return new UriBuilder
            {
                Scheme = clusterOptions.EnableTls ? Https : Http,
                Host = nodesAdapter.Hostname,
                Port = clusterOptions.EnableTls ? nodesAdapter.MgmtApiSsl : nodesAdapter.MgmtApi
            }.Uri;
        }

        /*public static Uri ReplaceCouchbaseSchemeWithHttp(this Uri uri, IConfiguration clusterOptions, string bucketName)
        {
            if (uri.Scheme == "couchbase")
            {
                var useSsl = clusterOptions.EnableTLS ? Https : Http;
                var newUri = new UriBuilder(uri) {Scheme = useSsl ? "https" : "http", Port = clusterOptions.MgmtPort};
                return newUri.Uri;
            }
            return uri;
        }*/

        public static IPAddress GetIpAddress(this Uri uri, bool useInterNetworkV6Addresses)
        {
            if (!IPAddress.TryParse(uri.Host, out var ipAddress))
            {
                try
                {
                    var hostEntry = Dns.GetHostEntry(uri.DnsSafeHost);

                    //use ip6 addresses only if configured
                    var hosts = useInterNetworkV6Addresses
                        ? hostEntry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6)
                        : hostEntry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork);

                    foreach (var host in hosts)
                    {
                        ipAddress = host;
                        break;
                    }

                    //default back to IPv4 addresses if no IPv6 can be resolved
                    if (useInterNetworkV6Addresses && ipAddress == null)
                    {
                        hosts = hostEntry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork);
                        foreach (var host in hosts)
                        {
                            ipAddress = host;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not resolve hostname to IP", e);
                }
            }
            if (ipAddress == null)
            {
                throw new Exception(uri.OriginalString);
            }
            return ipAddress;
        }
    }
}
