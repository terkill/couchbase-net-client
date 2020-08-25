using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Core.CircuitBreakers;
using Couchbase.Core.DI;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.IO.Authentication.X509;
using Couchbase.Core.IO.Serializers;
using Couchbase.Core.IO.Transcoders;
using Couchbase.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable UnusedMember.Global

#nullable enable

namespace Couchbase
{
    /// <summary>
    /// Options controlling the connection to the Couchbase cluster.
    /// </summary>
    public sealed class ClusterOptions
    {
        internal ConnectionString? ConnectionStringValue { get; set; }

        /// <summary>
        /// The connection string for the cluster.
        /// </summary>
        public string? ConnectionString
        {
            get => ConnectionStringValue?.ToString();
            set
            {
                ConnectionStringValue = value != null ? Couchbase.ConnectionString.Parse(value) : null;
                if (!string.IsNullOrWhiteSpace(ConnectionStringValue?.Username))
                {
                    UserName = ConnectionStringValue!.Username;
                }
            }
        }

        /// <summary>
        /// Set the connection string for the cluster.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithConnectionString(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            return this;
        }

        /// <summary>
        /// The buckets to be used in the cluster.
        /// </summary>
        public IList<string> Buckets { get; set; } = new List<string>();

        /// <summary>
        /// Set the buckets to be used in the cluster.
        /// </summary>
        /// <param name="bucketNames">The names of the buckets.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithBuckets(params string[] bucketNames)
        {
            if (!bucketNames?.Any() ?? true)
            {
                throw new ArgumentException($"{nameof(bucketNames)} cannot be null or empty.");
            }

            //just the name of the bucket for now - later make and actual cluster
            Buckets = new List<string>(bucketNames!);
            return this;
        }

        /// <summary>
        /// Set credentials used for authentication.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"{nameof(username)} cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException($"{nameof(password)} cannot be null or empty.");
            }

            UserName = username;
            Password = password;
            return this;
        }

        /// <summary>
        /// The <see cref="ILoggerFactory"/> to use for logging.
        /// </summary>
        public ILoggerFactory? Logging { get; set; }

        /// <summary>
        /// Set the <see cref="ILoggerFactory"/> to use for logging.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithLogging(ILoggerFactory? loggerFactory = null)
        {
            Logging = loggerFactory;

            return this;
        }

        /// <summary>
        /// Provide a custom <see cref="ITypeSerializer"/>.
        /// </summary>
        public ITypeSerializer? Serializer { get; set; }

        /// <summary>
        /// Provide a custom <see cref="ITypeSerializer"/>.
        /// </summary>
        /// <param name="serializer">Serializer to use.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithSerializer(ITypeSerializer serializer)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            return this;
        }

        /// <summary>
        /// Provide a custom <see cref="ITypeTranscoder"/>.
        /// </summary>
        public ITypeTranscoder? Transcoder { get; set; }

        /// <summary>
        /// Provide a custom <see cref="ITypeTranscoder"/>.
        /// </summary>
        /// <param name="transcoder">Transcoder to use.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithTranscoder(ITypeTranscoder transcoder)
        {
            Transcoder = transcoder ?? throw new ArgumentNullException(nameof(transcoder));

            return this;
        }

        /// <summary>
        /// Provide a custom <see cref="IDnsResolver"/> for DNS SRV resolution.
        /// </summary>
        public IDnsResolver? DnsResolver { get; set; }

        /// <summary>
        /// Provide a custom <see cref="IDnsResolver"/> for DNS SRV resolution.
        /// </summary>
        /// <param name="dnsResolver">DNS resolver to use.</param>
        /// <returns>
        /// A reference to this <see cref="ClusterOptions"/> object for method chaining.
        /// </returns>
        public ClusterOptions WithDnsResolver(IDnsResolver dnsResolver)
        {
            DnsResolver = dnsResolver ?? throw new ArgumentNullException(nameof(dnsResolver));

            return this;
        }

        [Obsolete("Use WithThresholdTracing instead.")]
        public IRequestTracer? RequestTracer { get; set; }

        [Obsolete("Use WithThresholdTracing instead.")]
        public ClusterOptions WithRequestTracer(IRequestTracer requestTracer)
        {
            RequestTracer = requestTracer;
            return this;
        }

        public ThresholdOptions? ThresholdOptions { get; set; } = new ThresholdOptions();

        public ClusterOptions WithThresholdTracing(ThresholdOptions? options = null)
        {
            ThresholdOptions = options;
            return this;
        }

        public ClusterOptions WithThresholdTracing(Action<ThresholdOptions> configure)
        {
            var opts = new ThresholdOptions();
            configure(opts);
            return WithThresholdTracing(opts);
        }

        public string? UserName { get; set; }
        public string? Password { get; set; }

        //Foundation RFC conformance
        public TimeSpan KvConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan KvTimeout { get; set; } = TimeSpan.FromSeconds(2.5);
        public TimeSpan KvDurabilityTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan ViewTimeout { get; set; } = TimeSpan.FromSeconds(75);
        public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(75);
        public TimeSpan AnalyticsTimeout { get; set; } = TimeSpan.FromSeconds(75);
        public TimeSpan SearchTimeout { get; set; } = TimeSpan.FromSeconds(75);
        public TimeSpan ManagementTimeout { get; set; } = TimeSpan.FromSeconds(75);

        /// <summary>
        /// Overrides the TLS behavior in <see cref="ConnectionString"/>, enabling or
        /// disabling TLS.
        /// </summary>
        public bool? EnableTls { get; set; }

        public bool EnableMutationTokens { get; set; } = true;
        ////public ITracer Tracer = new ThresholdLoggingTracer();
        public TimeSpan TcpKeepAliveTime { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan TcpKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(1);
        public bool ForceIPv4 { get; set; }
        public TimeSpan ConfigPollInterval { get; set; } = TimeSpan.FromSeconds(2.5);
        public TimeSpan ConfigPollFloorInterval { get; set; } = TimeSpan.FromMilliseconds(50);
        public TimeSpan ConfigIdleRedialTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Minimum number of connections per key/value node.
        /// </summary>
        public int NumKvConnections { get; set; } = 2;

        /// <summary>
        /// Maximum number of connections per key/value node.
        /// </summary>
        public int MaxKvConnections { get; set; } = 5;

        /// <summary>
        /// Amount of time with no activity before a key/value connection is considered idle.
        /// </summary>
        public TimeSpan IdleKvConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public int MaxHttpConnections { get; set; } = 0;

        [Obsolete("Not supported in .NET, uses system defaults.")]
        public TimeSpan IdleHttpConnectionTimeout { get; set; }

        public CircuitBreakerConfiguration CircuitBreakerConfiguration { get; set; } =
            CircuitBreakerConfiguration.Default;

        public bool EnableOperationDurationTracing { get; set; } = true;

        public RedactionLevel RedactionLevel { get; set; } = RedactionLevel.None;

        /// <summary>
        /// Port used for HTTP bootstrapping fallback if other bootstrap methods are not available.
        /// </summary>
        public int BootstrapHttpPort { get; set; } = 8091;

        /// <summary>
        /// Port used for TLS HTTP bootstrapping fallback if other bootstrap methods are not available.
        /// </summary>
        public int BootstrapHttpPortTls { get; set; } = 18091;

        /// <summary>
        /// Used for checking that the SDK has bootstrapped and potentially retrying if not.
        /// </summary>
        public TimeSpan BootstrapPollInterval { get; set; } = TimeSpan.FromSeconds(2.5);

        //Volatile or obsolete options
        public bool EnableExpect100Continue { get; set; }

        [Obsolete("This property is ignored; set the ClusterOptions.X509CertificateFactory property to a "
                  +" ICertificateFactory instance - Couchbase.Core.IO.Authentication.X509.CertificateStoreFactory for example.")]
        public bool EnableCertificateAuthentication { get; set; }
        public bool EnableCertificateRevocation { get; set; }

        /// <summary>
        /// Ignore CertificateNameMismatch and CertificateChainMismatch, since they happen together.
        /// </summary>
        public bool IgnoreRemoteCertificateNameMismatch { get; set; }

        private bool _enableOrphanedResponseLogging;
        public bool EnableOrphanedResponseLogging
        {
            get => _enableOrphanedResponseLogging;
            set
            {
                if (value != _enableOrphanedResponseLogging)
                {
                    _enableOrphanedResponseLogging = value;

                    if (value)
                    {
                        this.AddClusterService<IOrphanedResponseLogger, OrphanedResponseLogger>();
                    }
                    else
                    {
                        this.AddClusterService<IOrphanedResponseLogger, NullOrphanedResponseLogger>();
                    }
                }
            }
        }

        public bool EnableConfigPolling { get; set; } = true;
        public bool EnableTcpKeepAlives { get; set; } = true;
        public bool EnableDnsSrvResolution { get; set; } = true;
        public string NetworkResolution => Couchbase.NetworkResolution.Auto;

        /// <summary>
        /// Provides a default implementation of <see cref="ClusterOptions"/>.
        /// </summary>
        public static ClusterOptions Default => new ClusterOptions();

        /// <summary>
        /// Effective value for TLS, should be used instead of <see cref="EnableTls"/> internally within the SDK.
        /// </summary>
        internal bool EffectiveEnableTls => EnableTls ?? ConnectionStringValue?.Scheme == Scheme.Couchbases;

        internal bool ValidateCertificateCallback(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (IgnoreRemoteCertificateNameMismatch)
            {
                // mask out the name mismatch error, and the chain error that comes along with it
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
            }

            return sslPolicyErrors == SslPolicyErrors.None;
        }

        public ICertificateFactory? X509CertificateFactory { get; set; }

        public ClusterOptions WithX509CertificateFactory(ICertificateFactory certificateFactory)
        {
            X509CertificateFactory = certificateFactory ?? throw new NullReferenceException(nameof(certificateFactory));
            EnableTls = true;
            return this;
        }

        public bool UnorderedExecutionEnabled { get; set; } = false;

        #region DI

        private readonly IDictionary<Type, IServiceFactory> _services = DefaultServices.GetDefaultServices();

        /// <summary>
        /// Build a <see cref="IServiceProvider"/> from the currently registered services.
        /// </summary>
        /// <returns>The new <see cref="IServiceProvider"/>.</returns>
        internal IServiceProvider BuildServiceProvider()
        {
            this.AddClusterService(this);
            this.AddClusterService(Logging ?? new NullLoggerFactory());

            if (ThresholdOptions != null)
            {
                _services[typeof(IRequestTracer)] = new SingletonServiceFactory(typeof(ActivityRequestTracer));
            }

            if (Serializer != null)
            {
                this.AddClusterService(Serializer);
            }

            if (Transcoder != null)
            {
                this.AddClusterService(Transcoder);
            }

            if (DnsResolver != null)
            {
                this.AddClusterService(DnsResolver);
            }

            if (CircuitBreakerConfiguration != null)
            {
                this.AddClusterService(CircuitBreakerConfiguration);
            }

            return new CouchbaseServiceProvider(_services);
        }

        /// <summary>
        /// Register a service with the cluster's <see cref="ICluster.ClusterServices"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service which will be requested.</typeparam>
        /// <typeparam name="TImplementation">The type of the service implementation which is returned.</typeparam>
        /// <param name="factory">Factory which will create the service.</param>
        /// <param name="lifetime">Lifetime of the service.</param>
        /// <returns>The <see cref="ClusterOptions"/>.</returns>
        public ClusterOptions AddService<TService, TImplementation>(
            Func<IServiceProvider, TImplementation> factory,
            ClusterServiceLifetime lifetime)
            where TImplementation : notnull, TService
        {
            _services[typeof(TService)] = lifetime switch
            {
                ClusterServiceLifetime.Transient => new TransientServiceFactory(serviceProvider => factory(serviceProvider)),
                ClusterServiceLifetime.Cluster => new SingletonServiceFactory(serviceProvider => factory(serviceProvider)),
                _ => throw new InvalidEnumArgumentException(nameof(lifetime), (int) lifetime,
                    typeof(ClusterServiceLifetime))
            };

            return this;
        }

        /// <summary>
        /// Register a service with the cluster's <see cref="ICluster.ClusterServices"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service which will be requested.</typeparam>
        /// <typeparam name="TImplementation">The type of the service implementation which is returned.</typeparam>
        /// <param name="lifetime">Lifetime of the service.</param>
        /// <returns>The <see cref="ClusterOptions"/>.</returns>
        public ClusterOptions AddService<TService, TImplementation>(
            ClusterServiceLifetime lifetime)
            where TImplementation : TService
        {
            _services[typeof(TService)] = lifetime switch
            {
                ClusterServiceLifetime.Transient => new TransientServiceFactory(typeof(TImplementation)),
                ClusterServiceLifetime.Cluster => new SingletonServiceFactory(typeof(TImplementation)),
                _ => throw new InvalidEnumArgumentException(nameof(lifetime), (int) lifetime,
                    typeof(ClusterServiceLifetime))
            };

            return this;
        }

        #endregion
    }

    [Obsolete("Use Couchbase.NetworkResolution")]
    public static class NetworkTypes
    {
        public const string Auto = "auto";
        public const string Default = "default";
        public const string External = "external";
    }
}
