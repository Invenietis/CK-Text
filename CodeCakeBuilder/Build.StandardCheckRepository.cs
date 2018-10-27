using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.Solution;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.List;
using Cake.Core;
using CK.Text;
using NuGet.Protocol.Core.Types;
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeCake
{
    public partial class Build
    {
        /// <summary>
        /// Exposes global state information for the build script.
        /// </summary>
        class CheckRepositoryInfo
        {
            /// <summary>
            /// Gets or sets the build configuration: either "Debug" or "Release".
            /// Defaults to "Debug".
            /// </summary>
            public string BuildConfiguration { get; set; } = "Debug";

            /// <summary>
            /// Gets or sets the version of the packages.
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// Gets the version of the packages, without any build meta information (if any).
            /// </summary>
            public string FilePartVersion => CSemVer.SVersion.Parse( Version ).NormalizedText;

            /// <summary>
            /// Gets or sets the local feed path to which <see cref="LocalFeedPackagesToCopy"/> should be copied.
            /// Can be null if no local feed exists or if no push to local feed should be done.
            /// </summary>
            public string LocalFeedPath { get; set; }

            /// <summary>
            /// Gets whether this is a blank build.
            /// </summary>
            public bool IsLocalCIRelease { get; set; }

            /// <summary>
            /// Gets a mutable list of SolutionProject for which packages should be created and copied
            /// to the <see cref="LocalFeedPath"/>.
            /// </summary>
            public List<SolutionProject> LocalFeedPackagesToCopy { get; } = new List<SolutionProject>();

            /// <summary>
            /// Gets the mutable list of remote feeds to which packages should be pushed.
            /// </summary>
            public List<NuGetHelper.Feed> Feeds { get; } = new List<NuGetHelper.Feed>();

            /// <summary>
            /// Gets the union of <see cref="LocalFeedPackagesToCopy"/> and <see cref="Feeds"/>'s
            /// <see cref="NuGetHelper.Feed.PackagesToPublish"/> without duplicates.
            /// </summary>
            public IEnumerable<SolutionProject> ActualPackagesToPublish => LocalFeedPackagesToCopy.Concat( Feeds.SelectMany( f => f.PackagesToPublish ) ).Distinct();

            /// <summary>
            /// Gets whether it is useless to continue. By default if <see cref="NoPackagesToProduce"/> is true, this is true,
            /// but if <see cref="IgnoreNoPackagesToProduce"/> is set, then we should continue.
            /// </summary>
            public bool ShouldStop => NoPackagesToProduce && !IgnoreNoPackagesToProduce;

            /// <summary>
            /// Gets or sets whether <see cref="NoPackagesToProduce"/> should be ignored.
            /// Defaults to false: by default if there is no packages to produce <see cref="ShouldStop"/> is true.
            /// </summary>
            public bool IgnoreNoPackagesToProduce { get; set; }

            /// <summary>
            /// Gets whether there is at least one package to produce and push.
            /// </summary>
            public bool NoPackagesToProduce => (LocalFeedPath == null || LocalFeedPackagesToCopy.Count == 0)
                                               &&
                                               !Feeds.SelectMany( f => f.PackagesToPublish ).Any();
        }

        /// <summary>
        /// Creates a new <see cref="CheckRepositoryInfo"/>. This selects the feeds (a local and/or e remote one)
        /// and checks the packages that sould actually be produced for them.
        /// When running on Appveyor, the build number is set.
        /// </summary>
        /// <param name="projectsToPublish">The projects to publish.</param>
        /// <param name="gitInfo">The git info.</param>
        /// <returns>A new info object.</returns>
        CheckRepositoryInfo StandardCheckRepository( IEnumerable<SolutionProject> projectsToPublish, SimpleRepositoryInfo gitInfo )
        {
            var result = new CheckRepositoryInfo { Version = gitInfo.SafeNuGetVersion };

            // We build in Debug for any prerelease except "rc": the last prerelease step is in "Release".
            result.BuildConfiguration = gitInfo.IsValidRelease
                                        && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc")
                                        ? "Release"
                                        : "Debug";

            if( !gitInfo.IsValid )
            {
                if( Cake.InteractiveMode() != InteractiveMode.NoInteraction
                    && Cake.ReadInteractiveOption( "PublishDirtyRepo", "Repository is not ready to be published. Proceed anyway?", 'Y', 'N' ) == 'Y' )
                {
                    Cake.Warning( "GitInfo is not valid, but you choose to continue..." );
                    result.IgnoreNoPackagesToProduce = true;
                }
                else
                {
                    // On Appveyor, we let the build run: this gracefully handles Pull Requests.
                    if( Cake.AppVeyor().IsRunningOnAppVeyor )
                    {
                        result.IgnoreNoPackagesToProduce = true;
                    }
                    else Cake.TerminateWithError( "Repository is not ready to be published." );
                }
                // When the gitInfo is not valid, we do not try to push any packages, even if the build continues
                // (either because the user choose to continue or if we are on the CI server).
                // We don't need to worry about feeds here.
            }
            else
            {
                // gitInfo is valid: it is either ci or a release build. 
                // Local releases must not be pushed on any remote and are copied to LocalFeed/Local
                // feed (if LocalFeed/ directory above exists).
                bool isLocalCIRelease = gitInfo.Info.FinalSemVersion.Prerelease.EndsWith( ".local" );
                var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                if( localFeed != null && isLocalCIRelease )
                {
                    localFeed = System.IO.Path.Combine( localFeed, "Local" );
                    System.IO.Directory.CreateDirectory( localFeed );
                }
                result.IsLocalCIRelease = isLocalCIRelease;

                if( localFeed != null )
                {
                    result.Feeds.Add( new LocalFeed( localFeed ) );
                    result.LocalFeedPath = localFeed;
                }

                // Creating the right NuGetRemoteFeed according to the release level.
                if( !isLocalCIRelease )
                {
                    if( gitInfo.IsValidRelease )
                    {
                        if( gitInfo.PreReleaseName == "" )
                        {
                            result.Feeds.Add( new SignatureOpenSourcePublicFeed( "Stable" ) );
                        }
                        else if( gitInfo.PreReleaseName == "prerelease"
                                || gitInfo.PreReleaseName == "rc" )
                        {
                            result.Feeds.Add( new SignatureOpenSourcePublicFeed( "Latest" ) );
                        }
                        else
                        {
                            // An alpha, beta, delta, epsilon, gamma, kappa goes to preview feed.
                            result.Feeds.Add( new SignatureOpenSourcePublicFeed( "Preview" ) );
                        }
                    }
                    else
                    {
                        Debug.Assert( gitInfo.IsValidCIBuild );
                        result.Feeds.Add( new SignatureOpenSourcePublicFeed( "CI" ) );
                    }
                }
            }

            // Now that Local/RemoteFeeds are selected, we can check the packages that already exist
            // in those feeds.
            var all = result.Feeds.Select( f => f.InitializePackagesToPublishAsync( Cake, projectsToPublish, gitInfo.SafeNuGetVersion ) );
            System.Threading.Tasks.Task.WaitAll( all.ToArray() );
            foreach( var feed in result.Feeds )
            {
                feed.Information( Cake, projectsToPublish );
            }

            //if( result.LocalFeedPath != null )
            //{
            //    var lookup = projectsToPublish
            //                    .Select( p => new
            //                    {
            //                        Project = p,
            //                        Path = System.IO.Path.Combine( result.LocalFeedPath, $"{p.Name}.{gitInfo.SafeNuGetVersion}.nupkg" )
            //                    } )
            //                    .Select( x => new
            //                    {
            //                        x.Project,
            //                        Exists = System.IO.File.Exists( x.Path )
            //                    } )
            //                    .ToList();
            //    var notOk = lookup.Where( r => !r.Exists ).Select( r => r.Project );
            //    result.LocalFeedPackagesToCopy.AddRange( notOk );
            //}

            int nbPackagesToPublish = result.ActualPackagesToPublish.Count();
            if( nbPackagesToPublish == 0 )
            {
                Cake.Information( $"No packages out of {projectsToPublish.Count()} projects to publish." );
                if( Cake.Argument( "IgnoreNoPackagesToProduce", 'N' ) == 'Y' )
                {
                    result.IgnoreNoPackagesToProduce = true;
                }
            }
            else
            {
                Cake.Information( $"Should actually publish {nbPackagesToPublish} out of {projectsToPublish.Count()} projects with version={gitInfo.SafeNuGetVersion} and configuration={result.BuildConfiguration}: {result.ActualPackagesToPublish.Select( p => p.Name ).Concatenate()}" );
            }
            var appVeyor = Cake.AppVeyor();
            if( appVeyor.IsRunningOnAppVeyor )
            {
                if( result.ShouldStop )
                {
                    appVeyor.UpdateBuildVersion( $"{gitInfo.SafeNuGetVersion} - Skipped ({appVeyor.Environment.Build.Number})" );
                }
                else
                {
                    try
                    {
                        appVeyor.UpdateBuildVersion( gitInfo.SafeNuGetVersion );
                    }
                    catch
                    {
                        appVeyor.UpdateBuildVersion( $"{gitInfo.SafeNuGetVersion} ({appVeyor.Environment.Build.Number})" );
                    }
                }
            }
            return result;
        }

    }
}
