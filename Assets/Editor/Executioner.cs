using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
