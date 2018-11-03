using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Core;
using CK.Text;
using CSemVer;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCake
{
    public partial class Build
    {
        struct SimplePackageId
        {
            /// <summary>
            /// Gets the NuGet PackageIdentity object.
            /// </summary>
            public readonly PackageIdentity PackageIdentity;

            /// <summary>
            /// Gets the SVersion of the package.
            /// </summary>
            public readonly SVersion Version;

            /// <summary>
            /// Gets the name of the package.
            /// </summary>
            public string PackageId => PackageIdentity.Id;

            public SimplePackageId( string packageId, SVersion v )
            {
                PackageIdentity = new PackageIdentity( packageId, NuGetVersion.Parse( v.ToString() ) );
                Version = v;
            }

            public override string ToString() => PackageId + '.' + Version.ToNuGetPackageString();
        }

        static class NuGetHelper
        {
            static readonly SourceCacheContext _sourceCache;
            static readonly List<Lazy<INuGetResourceProvider>> _providers;
            static readonly ISettings _settings;
            static readonly PackageProviderProxy _sourceProvider;
            static ILogger _logger;

            /// <summary>
            /// Shared http client.
            /// See: https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
            /// Do not add any default on it.
            /// </summary>
            public static readonly HttpClient SharedHttpClient;

            class PackageProviderProxy : IPackageSourceProvider
            {
                readonly IPackageSourceProvider _fromSettings;
                readonly Lazy<List<PackageSource>> _sources;
                int _definedSourceCount;

                public PackageProviderProxy( ISettings settings )
                {
                    _fromSettings = new PackageSourceProvider( settings );
                    _sources = new Lazy<List<PackageSource>>( () => new List<PackageSource>( _fromSettings.LoadPackageSources() ) );
                }

                public PackageSource FindOrCreateFromUrl( string name, string urlV3 )
                {
                    if( String.IsNullOrEmpty( urlV3 ) || !urlV3.EndsWith( "/v3/index.json" ) )
                    {
                        throw new ArgumentException( "Feed requires a /v3/index.json url.", nameof( urlV3 ) );
                    }
                    if( String.IsNullOrWhiteSpace( name ) )
                    {
                        throw new ArgumentNullException( nameof( name ) );
                    }
                    var exists = _sources.Value.FirstOrDefault( s => !s.IsLocal && s.Source == urlV3 );
                    if( exists != null ) return exists;
                    exists = new PackageSource( urlV3, "CCB-" + name );
                    _sources.Value.Insert( _definedSourceCount++, exists );
                    return exists;
                }

                public PackageSource FindOrCreateFromLocalPath( string localPath )
                {
                    if( String.IsNullOrWhiteSpace( localPath ) ) throw new ArgumentNullException( nameof( localPath ) );
                    NormalizedPath path = System.IO.Path.GetFullPath( localPath );
                    var exists = _sources.Value.FirstOrDefault( s => s.IsLocal && new NormalizedPath( s.Source ) == path );
                    if( exists != null ) return exists;
                    exists = new PackageSource( path, "CCB-" + path.LastPart );
                    _sources.Value.Insert( _definedSourceCount++, exists );
                    return exists;
                }

                string IPackageSourceProvider.ActivePackageSourceName => _fromSettings.ActivePackageSourceName;

                string IPackageSourceProvider.DefaultPushSource => _fromSettings.DefaultPushSource;

                event EventHandler IPackageSourceProvider.PackageSourcesChanged { add { } remove { } }

                public IEnumerable<PackageSource> LoadPackageSources()
                {
                    return _sources.Value;
                }

                bool IPackageSourceProvider.IsPackageSourceEnabled( PackageSource source ) => true;

                void IPackageSourceProvider.DisablePackageSource( PackageSource source )
                {
                    throw new NotSupportedException( "Should not be called in this scenario." );
                }

                void IPackageSourceProvider.SaveActivePackageSource( PackageSource source )
                {
                    throw new NotSupportedException( "Should not be called in this scenario." );
                }

                void IPackageSourceProvider.SavePackageSources( IEnumerable<PackageSource> sources )
                {
                    throw new NotSupportedException( "Should not be called in this scenario." );
                }
            }

            static NuGetHelper()
            {
                // Workaround for dev/NuGet.Client\src\NuGet.Core\NuGet.Protocol\Plugins\PluginFactory.cs line 161:
                // FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH"),
                // This line should be:
                // FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
                //
                // Issue: https://github.com/NuGet/Home/issues/7438
                //
                Environment.SetEnvironmentVariable( "DOTNET_HOST_PATH", "dotnet" );

                _settings = Settings.LoadDefaultSettings( Environment.CurrentDirectory );
                _sourceProvider = new PackageProviderProxy( _settings );

                _sourceCache = new SourceCacheContext();
                _providers = new List<Lazy<INuGetResourceProvider>>();
                _providers.AddRange( Repository.Provider.GetCoreV3() );
                SharedHttpClient = new HttpClient();
            }

            class Logger : NuGet.Common.ILogger
            {
                readonly ICakeContext _ctx;
                readonly object _lock;

                public Logger( ICakeContext ctx )
                {
                    _ctx = ctx;
                    _lock = new object();
                }

                public void LogDebug( string data ) { lock( _lock ) _ctx.Debug( $"NuGet: {data}" ); }
                public void LogVerbose( string data ) { lock( _lock ) _ctx.Verbose( $"NuGet: {data}" ); }
                public void LogInformation( string data ) { lock( _lock ) _ctx.Information( $"NuGet: {data}" ); }
                public void LogMinimal( string data ) { lock( _lock ) _ctx.Information( $"NuGet: {data}" ); }
                public void LogWarning( string data ) { lock( _lock ) _ctx.Warning( $"NuGet: {data}" ); }
                public void LogError( string data ) { lock( _lock ) _ctx.Error( $"NuGet: {data}" ); }
                public void LogSummary( string data ) { lock( _lock ) _ctx.Information( $"NuGet: {data}" ); }
                public void LogInformationSummary( string data ) { lock( _lock ) _ctx.Information( $"NuGet: {data}" ); }
                public void Log( NuGet.Common.LogLevel level, string data ) { lock( _lock ) _ctx.Information( $"NuGet ({level}): {data}" ); }
                public Task LogAsync( NuGet.Common.LogLevel level, string data )
                {
                    Log( level, data );
                    return System.Threading.Tasks.Task.CompletedTask;
                }

                public void Log( NuGet.Common.ILogMessage message )
                {
                    lock( _lock ) _ctx.Information( $"NuGet ({message.Level}) - Code: {message.Code} - Project: {message.ProjectPath} - {message.Message}" );
                }

                public Task LogAsync( NuGet.Common.ILogMessage message )
                {
                    Log( message );
                    return System.Threading.Tasks.Task.CompletedTask;
                }
            }

            static NuGet.Common.ILogger InitializeAndGetLogger( ICakeContext ctx )
            {
                if( _logger == null )
                {
                    ctx.Information( $"Initializing with sources:" );
                    foreach( var s in _sourceProvider.LoadPackageSources() )
                    {
                        ctx.Information( $"{s.Name} => {s.Source}" );
                    }
                    _logger = new Logger( ctx );
                    var credProviders = new AsyncLazy<IEnumerable<ICredentialProvider>>( async () => await GetCredentialProvidersAsync( _sourceProvider, _logger ) );
                    HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(
                        () => new CredentialService(
                            providers: credProviders,
                            nonInteractive: true,
                            handlesDefaultCredentials: true ) );
                }
                return _logger;
            }
            static async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync( IPackageSourceProvider sourceProvider, ILogger logger )
            {
                var providers = new List<ICredentialProvider>();
                var securePluginProviders = await new SecurePluginCredentialProviderBuilder( pluginManager: PluginManager.Instance, canShowDialog: false, logger: logger ).BuildAllAsync();
                providers.AddRange( securePluginProviders );
                return providers;
            }

            /// <summary>
            /// Base class for NuGet feeds.
            /// </summary>
            public abstract class Feed
            {
                readonly PackageSource _packageSource;
                readonly SourceRepository _sourceRepository;
                readonly AsyncLazy<PackageUpdateResource> _updater;
                List<SimplePackageId> _packagesToPublish;

                /// <summary>
                /// Initialize a new remote feed.
                /// Its final <see cref="Name"/> is the one of the existing feed if it appears in the existing
                /// sources (from NuGet configuration files) or "CCB-<paramref name="name"/>" if this is
                /// an unexisting source (CCB is for CodeCakeBuilder). 
                /// </summary>
                /// <param name="name">Name of the feed.</param>
                /// <param name="urlV3">Must be a v3/index.json url otherwise an argument exception is thrown.</param>
                protected Feed( string name, string urlV3 )
                    : this( _sourceProvider.FindOrCreateFromUrl( name, urlV3 ) )
                {
                }

                /// <summary>
                /// Initialize a new local feed.
                /// Its final <see cref="Name"/> is the one of the existing feed if it appears in the existing
                /// sources (from NuGet configuration files) or "CCB-GetDirectoryName(localPath)" if this is
                /// an unexisting source (CCB is for CodeCakeBuilder). 
                /// </summary>
                /// <param name="localPath">Local path.</param>
                protected Feed( string localPath )
                    : this( _sourceProvider.FindOrCreateFromLocalPath( localPath ) )
                {
                }

                Feed( PackageSource s )
                {
                    _packageSource = s;
                    _sourceRepository = new SourceRepository( _packageSource, _providers );
                    _updater = new AsyncLazy<PackageUpdateResource>( async () =>
                    {
                        var r = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
                        // TODO: Update for next NuGet version?
                        // r.Settings = _settings;
                        return r;
                    } );
                }

                /// <summary>
                /// The url of the source. Can be a local path.
                /// </summary>
                public string Url => _packageSource.Source;

                /// <summary>
                /// Gets whether this is a local feed (a directory).
                /// </summary>
                public bool IsLocal => _packageSource.IsLocal;

                /// <summary>
                /// Gets the source name.
                /// If the source appears in NuGet configuration files, it is the configured name of the source, otherwise
                /// it is prefixed with "CCB-" (CCB is for CodeCakeBuilder). 
                /// </summary>
                public string Name => _packageSource.Name;

                /// <summary>
                /// Gets the list of packages that must be published (ie. they don't already exist in the feed).
                /// </summary>
                public IReadOnlyList<SimplePackageId> PackagesToPublish => _packagesToPublish;

                /// <summary>
                /// Pushes a set of packages (this is typically <see cref="PackagesToPublish"/>) from .nupkg files
                /// that must exist in <paramref name="path"/>.
                /// </summary>
                /// <param name="ctx">The Cake context.</param>
                /// <param name="path">The path where the .nupkg mus be found.</param>
                /// <param name="packages">The set of packages to push.</param>
                /// <param name="timeoutSeconds">Timeout in seconds.</param>
                /// <returns>The awaitable.</returns>
                public async Task PushPackagesAsync( ICakeContext ctx, string path, IEnumerable<SimplePackageId> packages, int timeoutSeconds = 20 )
                {
                    string apiKey = null;
                    if( !_packageSource.IsLocal )
                    {
                        apiKey = ResolveAPIKey( ctx );
                        if( string.IsNullOrEmpty( apiKey ) )
                        {
                            ctx.Information( $"Could not resolve API key. Push to '{Name}' => '{Url}' is skipped." );
                            return;
                        }
                    }
                    var logger = InitializeAndGetLogger( ctx );
                    var updater = await _updater;
                    foreach( var package in packages )
                    {
                        var fullPath = System.IO.Path.Combine( path, package.ToString() + ".nupkg" );
                        await updater.Push(
                            fullPath,
                            String.Empty, // no Symbol source.
                            timeoutSeconds,
                            disableBuffering: false,
                            getApiKey: endpoint => apiKey,
                            getSymbolApiKey: symbolsEndpoint => null,
                            noServiceEndpoint: false,
                            log: logger );
                        await OnPackagePushed( ctx, path, package );
                    }
                    await OnAllPackagesPushed( ctx, path, packages );
                }

                /// <summary>
                /// Called for each package pushed.
                /// Does nothing at this level.
                /// </summary>
                /// <param name="ctx">The Cake context.</param>
                /// <param name="path">The path where the .nupkg mus be found.</param>
                /// <param name="packages">The set of packages to push.</param>
                /// <returns>The awaitable.</returns>
                protected virtual Task OnPackagePushed( ICakeContext ctx, string path, SimplePackageId package )
                {
                    return System.Threading.Tasks.Task.CompletedTask;
                }

                /// <summary>
                /// Called once all the packages are pushed.
                /// Does nothing at this level.
                /// </summary>
                /// <param name="ctx">The Cake context.</param>
                /// <param name="path">The path where the .nupkg mus be found.</param>
                /// <param name="packages">The set of packages to push.</param>
                /// <returns>The awaitable.</returns>
                protected virtual Task OnAllPackagesPushed( ICakeContext ctx, string path, IEnumerable<SimplePackageId> packages )
                {
                    return System.Threading.Tasks.Task.CompletedTask;
                }

                /// <summary>
                /// Must resolve the API key required to push the package.
                /// </summary>
                /// <param name="ctx"></param>
                /// <returns></returns>
                protected abstract string ResolveAPIKey( ICakeContext ctx );

                /// <summary>
                /// Gets the number of packages that exist in the feed.
                /// This is computed by <see cref="InitializePackagesToPublishAsync"/>.
                /// </summary>
                public int PackagesAlreadyPublishedCount { get; private set; }

                /// <summary>
                /// Initializes the <see cref="PackagesToPublish"/> and <see cref="PackagesAlreadyPublishedCount"/>.
                /// This can be called multiple times.
                /// </summary>
                /// <param name="ctx">The Cake context.</param>
                /// <param name="allPackagesToPublish">The set of packages </param>
                /// <returns>The awaitable.</returns>
                public async Task InitializePackagesToPublishAsync( ICakeContext ctx, IEnumerable<SimplePackageId> allPackagesToPublish )
                {
                    if( _packagesToPublish != null )
                    {
                        _packagesToPublish.Clear();
                        PackagesAlreadyPublishedCount = 0;
                    }
                    else _packagesToPublish = new List<SimplePackageId>();
                    var logger = InitializeAndGetLogger( ctx );
                    MetadataResource meta = await _sourceRepository.GetResourceAsync<MetadataResource>();
                    foreach( var p in allPackagesToPublish )
                    {
                        if( await meta.Exists( p.PackageIdentity, _sourceCache, logger, CancellationToken.None ) )
                        {
                            ++PackagesAlreadyPublishedCount;
                        }
                        else
                        {
                            ctx.Debug( $"Package {p.PackageId} must be published to remote feed '{Name}'." );
                            _packagesToPublish.Add( p );
                        }
                    }
                    ctx.Debug( $" ==> {_packagesToPublish.Count} package(s) must be published to remote feed '{Name}'." );
                }

                /// <summary>
                /// Dumps information about <see cref="PackagesToPublish"/>.
                /// </summary>
                /// <param name="ctx">The Cake context.</param>
                /// <param name="allPackagesToPublish">The set of all packages to publish.</param>
                public void Information( ICakeContext ctx, IEnumerable<SimplePackageId> allPackagesToPublish )
                {
                    if( PackagesToPublish.Count == 0 )
                    {
                        ctx.Information( $"Feed '{Name}': No packages must be pushed ({PackagesAlreadyPublishedCount} packages already available)." );
                    }
                    else if( PackagesAlreadyPublishedCount == 0 )
                    {
                        ctx.Information( $"Feed '{Name}': All {PackagesAlreadyPublishedCount} packages must be pushed." );
                    }
                    else
                    {
                        ctx.Information( $"Feed '{Name}': {PackagesToPublish.Count} packages must be pushed: {PackagesToPublish.Select( p => p.PackageId ).Concatenate()}." );
                        ctx.Information( $"               => {PackagesAlreadyPublishedCount} packages already pushed: {allPackagesToPublish.Except( PackagesToPublish ).Select( p => p.PackageId ).Concatenate()}." );
                    }
                }
            }
        }


        /// <summary>
        /// A VSTS feed uses "VSTS" for the API key.
        /// </summary>
        class VSTSFeed : NuGetHelper.Feed
        {
            /// <summary>
            /// Initialize a new remote VSTS feed.
            /// </summary>
            /// <param name="name">Name of the feed.</param>
            /// <param name="urlV3">Must be a v3/index.json url otherwise an argument exception is thrown.</param>
            public VSTSFeed( string name, string urlV3 )
                : base( name, urlV3 )
            {
            }

            protected override string ResolveAPIKey( ICakeContext ctx ) => "VSTS";
        }

        /// <summary>
        /// A SignatureVSTSFeed handles Stable, Latest, Preview and CI Azure feed views with
        /// package promotion based on the published version.
        /// To handle package promotion, a Personal Access Token, "AZURE_FEED_PAT" environment variable
        /// must be defined and contains the token.
        /// If this "AZURE_FEED_PAT" is not defined or empty, push is skipped.
        /// </summary>
        class SignatureVSTSFeed : VSTSFeed
        {
            string _azureFeedPAT;

            /// <summary>
            /// Initialize a new SignatureVSTSFeed.
            /// Its <see cref="NuGetHelper.Feed.Name"/> is set to "<paramref name="organization"/>-<paramref name="feedId"/>".
            /// </summary>
            /// <param name="organization">Name of the organization.</param>
            /// <param name="feedId">Identifier of the feed in Azure, inside the organization.</param>
            public SignatureVSTSFeed( string organization = "Signature-OpenSource", string feedId = "Default" )
                : base( organization+"-"+feedId, $"https://pkgs.dev.azure.com/{organization}/_packaging/{feedId}/nuget/v3/index.json" )
            {
                Organization = organization;
                FeedId = feedId;
            }

            /// <summary>
            /// Gets the organization name.
            /// </summary>
            public string Organization { get; }

            /// <summary>
            /// Gets the feed identifier.
            /// </summary>
            public string FeedId { get; }

            /// <summary>
            /// Gets the Azure Feed Personal Access Token obtained from the "AZURE_FEED_PAT" environment variable.
            /// When null, push is disabled.
            /// </summary>
            protected string AzureFeedPersonalAccessToken => _azureFeedPAT;

            protected override string ResolveAPIKey( ICakeContext ctx )
            {
                _azureFeedPAT = ctx.InteractiveEnvironmentVariable( "AZURE_FEED_PAT" );
                if( String.IsNullOrWhiteSpace( _azureFeedPAT ) )
                {
                    ctx.Warning( "No AZURE_FEED_PAT environment variable found." );
                    _azureFeedPAT = null;
                }
                return _azureFeedPAT;
                // The API key for the Credential Provider must be "VSTS".
                return _azureFeedPAT != null ? "VSTS" : null;
            }

            protected override async Task OnAllPackagesPushed( ICakeContext ctx, string path, IEnumerable<SimplePackageId> packages )
            {
                foreach( var p in packages )
                {
                    foreach( var view in GetViewNames( p.Version ) )
                    {
                        using( HttpRequestMessage req = new HttpRequestMessage( HttpMethod.Post, $"https://pkgs.dev.azure.com/{Organization}/_apis/packaging/feeds/{FeedId}/nuget/packagesBatch?api-version=5.0-preview.1" ) )
                        {

                            var basicAuth = Convert.ToBase64String( ASCIIEncoding.ASCII.GetBytes( ":" + AzureFeedPersonalAccessToken ) );
                            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue( "Basic", basicAuth );
                            var body = GetPromotionJSONBody( p.PackageId, p.PackageIdentity.Version.ToString(), view );
                            req.Content = new StringContent( body, Encoding.UTF8, "application/json" );
                            using( var m = await NuGetHelper.SharedHttpClient.SendAsync( req ) )
                            {
                                if( m.IsSuccessStatusCode )
                                {
                                    ctx.Information( $"Package '{p}' promoted to view '@{view}'." );
                                }
                                else 
                                {
                                    ctx.Error( $"Package '{p}' promotion to view '@{view}' failed." );
                                    m.EnsureSuccessStatusCode();
                                }
                            }
                        }
                    }
                }
            }

            IEnumerable<string> GetViewNames( SVersion v )
            {
                yield return "CI";
                if( v.IsLatestLabel )
                {
                    yield return "Latest";
                    yield return "Preview";
                    if( v.IsStableLabel ) yield return "Stable";
                }
                else if( v.IsPreviewLabel ) yield return "Preview";
            }

            string GetPromotionJSONBody( string packageName, string packageVersion, string viewId, bool npm = false )
            {
                var bodyFormat = @"{
 ""data"": {
    ""viewId"": ""{viewId}""
  },
  ""operation"": 0,
  ""packages"": [{
    ""id"": ""{packageName}"",
    ""version"": ""{packageVersion}"",
    ""protocolType"": ""{NuGetOrNpm}""
  }]
}";
                return bodyFormat.Replace( "{NuGetOrNpm}", npm ? "Npm" : "NuGet" )
                                 .Replace( "{viewId}", viewId )
                                 .Replace( "{packageName}", packageName )
                                 .Replace( "{packageVersion}", packageVersion );
            }

        }

        /// <summary>
        /// A remote feed where push is controlled by its <see cref="APIKeyName"/>.
        /// </summary>
        class RemoteFeed : NuGetHelper.Feed
        {
            /// <summary>
            /// Initialize a new remote feed.
            /// The push is controlled by an API key name that is the name of an environment variable
            /// that must contain the actual API key to push packages.
            /// </summary>
            /// <param name="name">Name of the feed.</param>
            /// <param name="urlV3">Must be a v3/index.json url otherwise an argument exception is thrown.</param>
            public RemoteFeed( string name, string urlV3 )
                : base( name, urlV3 )
            {
            }

            /// <summary>
            /// Gets or sets the push API key name.
            /// This is the environment variable name that must contain the NuGet API key required to push.
            /// </summary>
            public string APIKeyName { get; set; }

            protected override string ResolveAPIKey( ICakeContext ctx )
            {
                if( String.IsNullOrEmpty( APIKeyName ) )
                {
                    ctx.Information( $"Remote feed '{Name}' APIKeyName is null or empty." );
                    return null;
                }
                return ctx.InteractiveEnvironmentVariable( APIKeyName );
            }

        }

        /// <summary>
        /// Local feed. Pushes are always possible.
        /// </summary>
        class LocalFeed : NuGetHelper.Feed
        {
            public LocalFeed( string path )
                : base( path )
            {
            }

            protected override string ResolveAPIKey( ICakeContext ctx ) => null;
        }


    }
}

