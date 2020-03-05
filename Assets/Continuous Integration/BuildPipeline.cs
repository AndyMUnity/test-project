using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Experimental;
using UnityEngine;

namespace ContinuousIntegration
{
    internal enum ExitCode
    {
        Success = 0,
        UnknownError,
        PlayerBuildCanceled,
        PlayerBuildUnknownError,
        PlayerBuildFailed,
        MethodNotFound,
        AddressablesBuildFailed,
        AddressablesContentFileNotFound,
    }
    
    public static class BuildPipeline
    {
        private static string[] args = null;
        private static string[] Args => args ?? (args = Environment.GetCommandLineArgs());
        
        private static bool HasArgument( string arg )
        {
            for( int i = 0; i < Args.Length; ++i )
            {
                if( Args[i] == arg )
                    return true;
            }

            return false;
        }

        [InitializeOnLoadMethod]
        public static void BuildMenuOverride()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler( BuildAddressablesBeforePlayer );
        }

        private static void BuildAddressablesBeforePlayer( BuildPlayerOptions options )
        {
            bool addressablesSuccess = true;
            // do stuff outside of the build Process here - here I build AssetBundles as an example
            BuildScript.buildCompleted += result =>
            {
                if( !string.IsNullOrEmpty( result.Error ) )
                    addressablesSuccess = false;
            };
            AddressableAssetSettings.BuildPlayerContent();
            
            if( addressablesSuccess )
            {
                BuildReport r = UnityEditor.BuildPipeline.BuildPlayer( options );
                Debug.Log( r.summary.result );
            }
            else
            {
                Debug.LogError( "Failed to Setup Addressables build" );
            }
        }

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

        [MenuItem("Test/Build All")]
        public static void TestRun()
        {
            NewAddressablesBuild();
            BuildPlayer();
        }

        public static void Run()
        {
            for (int i = 0; i < Args.Length; i++)
            {
                switch( Args[i] )
                {
                    case "-buildPlayer":
                        BuildPlayer();
                        break;
                    case "-buildAssetBundles":
                        BuildBundles();
                        break;
                    case "-addressablesReleaseBuild":
                        NewAddressablesBuild();
                        break;
                    case "-addressablesUpdateBuild":
                        UpdateAddressablesBuild();
                        break;
                }
            }
            
            EditorApplication.Exit( 0 );
        }

        private static void PreBuild()
        {
            for (int i = 0; i < Args.Length; i++)
            {
                if( Args[i] != "-executePreBuildMethod" )
                    continue;
                ++i;
                ExecuteMethod( Args[i] );
            }
        }
        
        public static void BuildPlayer()
        {
            PreBuild();
            
            // setup the build options from arguments
            BuildPlayerOptions options = InitialOptions();
            for (int i = 0; i < Args.Length; i++)
            {
                switch( Args[i] )
                {
                    case "-locationPathName":
                        ++i;
                        options.locationPathName = Args[i];
                        break;
                    case "-development":
                        options.options |= BuildOptions.Development;
                        break;
                    case "-autoRunPlayer":
                        options.options |= BuildOptions.AutoRunPlayer;
                        break;
                    case "-allowDebugging":
                        options.options |= BuildOptions.AllowDebugging;
                        break;
                }
            }

            BuildReport r = UnityEditor.BuildPipeline.BuildPlayer( options );
            PostBuild();
            ExitCode code = GetExitCode( r.summary.result );
            if( code != ExitCode.Success )
                EditorApplication.Exit( (int)code );
        }

        private static void PostBuild()
        {
            for (int i = 0; i < Args.Length; i++)
            {
                if( Args[i] == "-executePostBuildMethod" )
                {
                    ++i;
                    ExecuteMethod( Args[i] );
                }
            }
        }

        private static void ExecuteMethod( string method )
        {
            string[] path = method.Split( '.' );
            if( path.Length < 2 )
            {
                Debug.LogError( "Could not find method [" + method + "], Method path is too short" );
                if( HasArgument( "-noErrorOnMethodNotFound" ) )
                    return;
                EditorApplication.Exit( (int)ExitCode.MethodNotFound );
            }
            
            StringBuilder b = new StringBuilder(method.Length-path[path.Length-1].Length);
            for( int i = 0; i < path.Length - 1; ++i )
            {
                b.Append( path[i] );
                if( i < path.Length - 2 )
                    b.Append( '.' );
            }

            string classPath = b.ToString();
            
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach( Assembly assembly in assemblies )
            {
                foreach( Type type in assembly.GetTypes() )
                {
                    if( type.FullName.EndsWith( classPath ) )
                    {
                        MethodInfo m = type.GetMethod( path[path.Length - 1], BindingFlags.Static | BindingFlags.Public );
                        if( m == null )
                        {
                            Debug.LogError( "Could not find method [" + method + "], Method not found in Type" );
                            if( HasArgument( "-noErrorOnMethodNotFound" ) )
                                return;
                            EditorApplication.Exit( (int)ExitCode.MethodNotFound );
                        }

                        m.Invoke( null, null );
                    }
                }
            }
        }

        private static void BuildBundles()
        {
            ExitCode code = ExitCode.UnknownError;
            
            if( code != ExitCode.Success )
                EditorApplication.Exit( (int)code );
        }
        
        [MenuItem("Addressables/New Build")]
        private static void NewAddressablesBuild()
        {
            ExitCode code = ExitCode.UnknownError;

            BuildScript.buildCompleted += result =>
            {
                if( !string.IsNullOrEmpty( result.Error ) )
                    code = ExitCode.PlayerBuildFailed;
            };
            AddressableAssetSettings.BuildPlayerContent();
            
            // TODO edit our catalog name
            
            if( code != ExitCode.Success )
                EditorApplication.Exit( (int)code );
        }

        [MenuItem("Addressables/Update Build")]
        private static void UpdateAddressablesBuild()
        {
            ExitCode code = ExitCode.UnknownError;

            BuildScript.buildCompleted += result =>
            {
                if( !string.IsNullOrEmpty( result.Error ) )
                    code = ExitCode.PlayerBuildFailed;
            };
            
            // find out what version we are updating
            
            // find where the 
            var state = ContentUpdateScript.LoadContentState( "" );
            
            List<AddressableAssetEntry> entries = ContentUpdateScript.GatherModifiedEntries( AddressableAssetSettingsDefaultObject.Settings, "" );
            ContentUpdateScript.CreateContentUpdateGroup( AddressableAssetSettingsDefaultObject.Settings, entries, "Update Group" );
            ContentUpdateScript.BuildContentUpdate( AddressableAssetSettingsDefaultObject.Settings, "" );
            
            // TODO edit catalog name.
            
            if( code != ExitCode.Success )
                EditorApplication.Exit( (int)code );
        }
    }
}

