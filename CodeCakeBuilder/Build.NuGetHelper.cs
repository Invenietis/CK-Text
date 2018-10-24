using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Core;
using CK.Text;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCake
{
    public partial class Build
    {
        static class NuGetHelper
        {
            static SourceCacheContext _sourceCache;
            static List<Lazy<INuGetResourceProvider>> _providers;
            static NuGet.Common.ILogger _logger;

            static NuGetHelper()
            {
                _sourceCache = new SourceCacheContext();
                _providers = new List<Lazy<INuGetResourceProvider>>();
                _providers.AddRange( Repository.Provider.GetCoreV3() );
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

            static NuGet.Common.ILogger GetLogger( ICakeContext ctx ) => _logger ?? (_logger = new Logger( ctx ));

            public class Feed
            {
                readonly PackageSource _packageSource;
                readonly SourceRepository _sourceRepository;
                List<SolutionProject> _packagesToPublish;

                public Feed( string name, string urlV3 )
                {
                    Name = name;
                    _packageSource = new PackageSource( urlV3 );
                    _sourceRepository = new SourceRepository( _packageSource, _providers );
                }

                public string Url => _packageSource.Source;

                public string Name { get; }

                public IReadOnlyList<SolutionProject> PackagesToPublish => _packagesToPublish;

                public int PackagesAlreadyPublishedCount { get; private set; }

                public async Task InitializePackagesToPublishAsync( ICakeContext ctx, IEnumerable<SolutionProject> projectsToPublish, string nuGetVersion )
                {
                    if( _packagesToPublish == null )
                    {
                        _packagesToPublish = new List<SolutionProject>();
                        var targetVersion = NuGetVersion.Parse( nuGetVersion );
                        MetadataResource meta = await _sourceRepository.GetResourceAsync<MetadataResource>();
                        foreach( var p in projectsToPublish )
                        {
                            var id = new PackageIdentity( p.Name, targetVersion );
                            if( await meta.Exists( id, _sourceCache, GetLogger( ctx ), CancellationToken.None ) )
                            {
                                ++PackagesAlreadyPublishedCount;
                            }
                            else
                            {
                                ctx.Debug( $"Package {p.Name} must be published to remote feed '{Name}'." );
                                _packagesToPublish.Add( p );
                            }
                        }
                    }
                    ctx.Debug( $" ==> {_packagesToPublish.Count} package(s) must be published to remote feed '{Name}'." );
                }

                public void Information( ICakeContext ctx, IEnumerable<SolutionProject> projectsToPublish )
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
                        ctx.Information( $"Feed '{Name}': {PackagesToPublish.Count} packages must be pushed: {PackagesToPublish.Select( p => p.Name ).Concatenate()}." );
                        ctx.Information( $"               => {PackagesAlreadyPublishedCount} packages already pushed: {projectsToPublish.Except( PackagesToPublish ).Select( p => p.Name ).Concatenate()}." );
                    }
                }
            }
        }

        class SignatureOpenSourcePublicFeed : NuGetHelper.Feed
        {
            public SignatureOpenSourcePublicFeed( string feedName )
                : base( feedName, $"https://pkgs.dev.azure.com/Signature-OpenSource/_packaging/{feedName}/nuget/v3/index.json" )
            {
            }
        }

    }
}

