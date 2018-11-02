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
            static SourceCacheContext _sourceCache;
            static List<Lazy<INuGetResourceProvider>> _providers;
            static ILogger _logger;
            static ISettings _settings;
            static IPackageSourceProvider _sourceProvider;

            /// <summary>
            /// Shared http client.
            /// See: https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
            /// Do not add any default on it.
            /// </summary>
            public static readonly HttpClient SharedHttpClient;

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
                _sourceCache = new SourceCacheContext();
                _providers = new List<Lazy<INuGetResourceProvider>>();
                _providers.AddRange( Repository.Provider.GetCoreV3() );
                SharedHttpClient = new HttpClient();
            }

            public static void SetupCredentialService( IPackageSourceProvider sourceProvider, ILogger logger, bool nonInteractive )
            {
                var providers = new AsyncLazy<IEnumerable<ICredentialProvider>>( async () => await GetCredentialProvidersAsync( sourceProvider, logger ) );
                HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(
                    () => new CredentialService(
                                providers: providers,
                                nonInteractive: nonInteractive,
                                handlesDefaultCredentials: true ) );

            }

            //#region Credential provider for Credential section of nuget.config.
            //// Must be upgraded when a 4.9 or 5.0 is out.
            //// This currently only support "basic" authentication type.
            //public class SettingsCredentialProvider : ICredentialProvider
            //{
            //    private readonly IPackageSourceProvider _packageSourceProvider;

            //    public SettingsCredentialProvider( IPackageSourceProvider packageSourceProvider )
            //    {
            //        if( packageSourceProvider == null )
            //        {
            //            throw new ArgumentNullException( nameof( packageSourceProvider ) );
            //        }
            //        _packageSourceProvider = packageSourceProvider;
            //        Id = $"{typeof( SettingsCredentialProvider ).Name}_{Guid.NewGuid()}";
            //    }

            //    /// <summary>
            //    /// Unique identifier of this credential provider
            //    /// </summary>
            //    public string Id { get; }


            //    public Task<CredentialResponse> GetAsync(
            //        Uri uri,
            //        IWebProxy proxy,
            //        CredentialRequestType type,
            //        string message,
            //        bool isRetry,
            //        bool nonInteractive,
            //        CancellationToken cancellationToken )
            //    {
            //        if( uri == null ) throw new ArgumentNullException( nameof( uri ) );

            //        cancellationToken.ThrowIfCancellationRequested();

            //        ICredentials cred = null;

            //        // If we are retrying, the stored credentials must be invalid.
            //        if( !isRetry && type != CredentialRequestType.Proxy )
            //        {
            //            cred = GetCredentials( uri );
            //        }

            //        var response = cred != null
            //            ? new CredentialResponse( cred )
            //            : new CredentialResponse( CredentialStatus.ProviderNotApplicable );

            //        return System.Threading.Tasks.Task.FromResult( response );
            //    }

            //    private ICredentials GetCredentials( Uri uri )
            //    {
            //        var source = _packageSourceProvider.LoadPackageSources().FirstOrDefault( p =>
            //        {
            //            Uri sourceUri;
            //            return p.Credentials != null
            //                && p.Credentials.IsValid()
            //                && Uri.TryCreate( p.Source, UriKind.Absolute, out sourceUri )
            //                && UriEquals( sourceUri, uri );
            //        } );
            //        if( source == null )
            //        {
            //            // The source is not in the config file
            //            return null;
            //        }
            //        // In 4.8.0 version, there is not yet the ValidAuthenticationTypes nor the ToICredentials() method.
            //        // return source.Credentials.ToICredentials();
            //        return new AuthTypeFilteredCredentials( new NetworkCredential( source.Credentials.Username, source.Credentials.Password ), new[] { "basic" } );
            //    }

            //    /// <summary>
            //    /// Determines if the scheme, server and path of two Uris are identical.
            //    /// </summary>
            //    private static bool UriEquals( Uri uri1, Uri uri2 )
            //    {
            //        uri1 = CreateODataAgnosticUri( uri1.OriginalString.TrimEnd( '/' ) );
            //        uri2 = CreateODataAgnosticUri( uri2.OriginalString.TrimEnd( '/' ) );

            //        return Uri.Compare( uri1, uri2, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase ) == 0;
            //    }

            //    // Bug 2379: SettingsCredentialProvider does not work
            //    private static Uri CreateODataAgnosticUri( string uri )
            //    {
            //        if( uri.EndsWith( "$metadata", StringComparison.OrdinalIgnoreCase ) )
            //        {
            //            uri = uri.Substring( 0, uri.Length - 9 ).TrimEnd( '/' );
            //        }
            //        return new Uri( uri );
            //    }
            //}
            //#endregion

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
                    _logger = new Logger( ctx );
                    _settings = Settings.LoadDefaultSettings( Environment.CurrentDirectory );
                    _sourceProvider = new PackageSourceProvider( _settings );
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
                //providers.Add( new SettingsCredentialProvider( sourceProvider ) );
                return providers;
            }

            public abstract class Feed
            {
                readonly PackageSource _packageSource;
                readonly SourceRepository _sourceRepository;
                readonly AsyncLazy<PackageUpdateResource> _updater;
                List<SimplePackageId> _packagesToPublish;

                /// <summary>
                /// Initialize a new remote feed.
                /// </summary>
                /// <param name="name">Name of the feed.</param>
                /// <param name="urlV3">Must be a v3/index.json url otherwise an argument exception is thrown.</param>
                protected Feed( string name, string urlV3 )
                    : this( FromUrl( name, urlV3 ) )
                {
                }

                /// <summary>
                /// Initialize a new local feed.
                /// </summary>
                /// <param name="localPath">Local path.</param>
                protected Feed( string localPath )
                    : this( FromPath( localPath ) )
                {
                }

                static PackageSource FromUrl( string name, string urlV3 )
                {
                    if( String.IsNullOrEmpty( urlV3 ) || !urlV3.EndsWith( "/v3/index.json" ) )
                    {
                        throw new ArgumentException( "Feed requires a /v3/index.json url.", nameof( urlV3 ) );
                    }
                    if( String.IsNullOrWhiteSpace( name ) )
                    {
                        throw new ArgumentNullException( nameof( name ) );
                    }
                    return new PackageSource( urlV3, name );
                }

                static PackageSource FromPath( string localPath )
                {
                    if( String.IsNullOrWhiteSpace( localPath ) ) throw new ArgumentNullException( nameof( localPath ) );
                    localPath = System.IO.Path.GetFullPath( localPath );
                    var name = System.IO.Path.GetFileName( localPath );
                    return new PackageSource( localPath, name );
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

                    // Test.
                    var f = System.IO.Path.GetDirectoryName( Environment.CurrentDirectory );
                    f = System.IO.Path.Combine( f, "NuGet.config" );
                    System.IO.File.WriteAllText( f,
$@"<configuration>
  <packageSources>
    <add key=""Signature-OpenSource-FIX-For-Push"" value=""{_packageSource.Source}"" />
</configuration>" );

                }

                public string Url => _packageSource.Source;

                public bool IsLocal => _packageSource.IsLocal;

                public string Name => _packageSource.Name;

                public IReadOnlyList<SimplePackageId> PackagesToPublish => _packagesToPublish;

                public async Task PushPackages( ICakeContext ctx, string path, IEnumerable<SimplePackageId> packages, int timeoutSeconds = 20 )
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

                protected virtual Task OnPackagePushed( ICakeContext ctx, string path, SimplePackageId package )
                {
                    return System.Threading.Tasks.Task.CompletedTask;
                }

                protected virtual Task OnAllPackagesPushed( ICakeContext ctx, string path, IEnumerable<SimplePackageId> packages )
                {
                    return System.Threading.Tasks.Task.CompletedTask;
                }

                protected abstract string ResolveAPIKey( ICakeContext ctx );

                public int PackagesAlreadyPublishedCount { get; private set; }

                public async Task InitializePackagesToPublishAsync( ICakeContext ctx, IEnumerable<SimplePackageId> allPackagesToPublish )
                {
                    if( _packagesToPublish == null )
                    {
                        _packagesToPublish = new List<SimplePackageId>();
                        MetadataResource meta = await _sourceRepository.GetResourceAsync<MetadataResource>();
                        foreach( var p in allPackagesToPublish )
                        {
                            if( await meta.Exists( p.PackageIdentity, _sourceCache, InitializeAndGetLogger( ctx ), CancellationToken.None ) )
                            {
                                ++PackagesAlreadyPublishedCount;
                            }
                            else
                            {
                                ctx.Debug( $"Package {p.PackageId} must be published to remote feed '{Name}'." );
                                _packagesToPublish.Add( p );
                            }
                        }
                    }
                    ctx.Debug( $" ==> {_packagesToPublish.Count} package(s) must be published to remote feed '{Name}'." );
                }

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

