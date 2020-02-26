using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ContinuousIntegration
{
    internal enum ExitCode
    {
        Success = 0,
        UnknownError,
        PlayerBuildCanceled,
        PlayerBuildUnknownError,
        PlayerBuildFailed
    }
    
    public static class BuildPipeline
    {
        private static string[] args;
        
        private static ExitCode GetExitCode( BuildResult playerBuildResult )
        {
            switch( playerBuildResult )
            {
                case BuildResult.Cancelled:
                    return ExitCode.PlayerBuildCanceled;
                case BuildResult.Failed:
                    return ExitCode.PlayerBuildFailed;
                case BuildResult.Unknown:
                    return ExitCode.PlayerBuildUnknownError;
                case BuildResult.Succeeded:
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof(playerBuildResult), playerBuildResult, null );
            }

            return ExitCode.Success;
        }

        private static BuildPlayerOptions InitialOptions()
        {
            List<string> scenes = new List<string>();
            foreach( EditorBuildSettingsScene settingsScene in EditorBuildSettings.scenes )
            {
                if( settingsScene.enabled )
                    scenes.Add( settingsScene.path );
            }
            
            DateTime now = DateTime.Now;
            string outputPath = "Builds/" + EditorUserBuildSettings.activeBuildTarget + "/" +
                                       now.Year + "-" + now.Month + "-" + now.Day + 
                                       "_" + now.TimeOfDay.TotalSeconds;
            
            return new BuildPlayerOptions
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup,
                scenes = scenes.ToArray(),
                options = BuildOptions.None,
                locationPathName = outputPath
            };
        }

        public static void Run()
        {
            args = System.Environment.GetCommandLineArgs();
            
            for (int i = 0; i < args.Length; i++)
            {
                if( args[i] == "-buildPlayer" )
                    BuildPlayer();
                else if( args[i] == "-buildAssetBundles" )
                    BuildBundles();
            }
        }
        
        private static void BuildPlayer()
        {
            ExitCode code = ExitCode.UnknownError;

            BuildPlayerOptions options = InitialOptions();
            for (int i = 0; i < args.Length; i++)
            {
                if( args[i] == "-development" )
                    options.options |= BuildOptions.Development;
                else if( args[i] == "-buildPath" )
                {
                    ++i;
                    options.locationPathName = args[i];
                }
            }

            BuildReport r = UnityEditor.BuildPipeline.BuildPlayer( options );
            code = GetExitCode( r.summary.result );
            EditorApplication.Exit( (int)code );
        }
        
        private static void BuildBundles()
        {
            ExitCode code = ExitCode.UnknownError;
            EditorApplication.Exit( (int)code );
        }
    }
}

