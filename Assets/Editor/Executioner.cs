using Boo.Lang;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ContinuousIntegration
{
    public static class BuildPipeline
    {
        
        private class BuildSettings
        {
            public bool Development;
            public BuildSettings()
            {
                Development = EditorUserBuildSettings.development;
            }

            public void SetSettings()
            {
                if( EditorUserBuildSettings.development != Development )
                    EditorUserBuildSettings.development = Development;
            }
        }

        private static BuildSettings SetupBuildArguments()
        {
            BuildSettings preBuildValues = new BuildSettings();
            int releaseMode = 0;
            
            // check for development / non-development
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if( args[i] == "-development" )
                    releaseMode = -1;
                else if( args[i] == "-release" )
                    releaseMode = 1;
            }

            if( releaseMode != 0 )
                EditorUserBuildSettings.development = releaseMode == -1;

            return preBuildValues;
        }





        private enum ExitCode
        {
            Success = 0,
            UnknownError,
            PlayerBuildCanceled,
            PlayerBuildUnknownError,
            PlayerBuildFailed
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
            }

            return ExitCode.Success;
        }

        private static BuildPlayerOptions InitialOptions()
        {
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.target = EditorUserBuildSettings.activeBuildTarget;
            options.targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            List<string> scenes = new List<string>();
            foreach( EditorBuildSettingsScene settingsScene in EditorBuildSettings.scenes )
            {
                if( settingsScene.enabled )
                    scenes.Add( settingsScene.path );
            }
            options.scenes = scenes.ToArray();
            options.options = BuildOptions.None;
            options.locationPathName = "MyBuild";
            return options;
        }
        
        public static void BuildPlayer()
        {
            ExitCode code = ExitCode.UnknownError;
            BuildPlayerOptions options = InitialOptions();
            
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if( args[i] == "-development" )
                    options.options |= BuildOptions.Development;
            }

            BuildReport r = UnityEditor.BuildPipeline.BuildPlayer( options );
            code = GetExitCode( r.summary.result );
            EditorApplication.Exit( (int)code );
        }
    }
}

public static class Executioner
{
    public static void ExitWithCodeThree()
    {
        string[] args = System.Environment.GetCommandLineArgs();
         for (int i = 0; i < args.Length; i++)
         {
             Debug.Log ("ARG " + i + ": " + args [i]);
             if (args [i] == "-error") {
                 EditorApplication.Exit( 3 );
             }
         }
        EditorApplication.Exit( 0 );
    }
}
