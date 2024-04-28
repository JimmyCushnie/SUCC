using JECS.ParsingLogic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JECS
{
    /// <summary>
    /// Provides various helpful functions for working with JECS files.
    /// </summary>
    public static class Utilities
    {
        private static string _DefaultPath = GetDefaultDefaultPath();

        /// <summary>
        /// The path that DataFile locations will be relative to if you assign them a non-absolute path. By default this is the same folder your game executable is in, or [project folder]/Game/ in the editor. You can change it if you like.
        /// </summary>
        public static string DefaultPath
        {
            get => _DefaultPath;
            set
            {
                if (!Path.IsPathRooted(value))
                    throw new Exception($"When setting a custom default path, you must set an absolute path. The path {value} is not absolute.");
                _DefaultPath = value;
            }
        }

        private static string GetDefaultDefaultPath()
        {
            // https://docs.unity3d.com/ScriptReference/Application-dataPath.html
#if UNITY_EDITOR
            string ProjectFolder = new DirectoryInfo(Application.dataPath).Parent.FullName;
            return Path.Combine(ProjectFolder, "Game");
#elif UNITY_STANDALONE_OSX
            return new DirectoryInfo(Application.dataPath).Parent.Parent.FullName;
#else
            return new DirectoryInfo(Application.dataPath).Parent.FullName;
#endif
        }

        /// <summary> All JECS files have this file extension. </summary>
        public const string FileExtension = ".jecs";

        /// <summary> detects whether a file path is relative or absolute, and returns the absolute path </summary>
        public static string AbsolutePath(string relativeOrAbsolutePath)
        {
            if (Path.IsPathRooted(relativeOrAbsolutePath)) 
                return relativeOrAbsolutePath;

            if (DefaultPath == null)
                throw new InvalidOperationException($"You can't use relative paths unless you've set a {nameof(DefaultPath)}. Path {relativeOrAbsolutePath} was not absolute.");

            return Path.Combine(DefaultPath, relativeOrAbsolutePath);
        }

        /// <summary> Takes a path and turns it into the path of a JECS file, with extension. </summary>
        public static string AbsoluteJecsPath(string relativeOrAbsolutePath)
        {
            var path = AbsolutePath(relativeOrAbsolutePath);
            if (Path.HasExtension(path) && Path.GetExtension(path).Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
                return path;

            return path + FileExtension;
        }

        /// <summary> Does a JECS file exist at the path? </summary>
        public static bool JecsFileExists(string relativeOrAbsolutePath)
        {
            var path = AbsoluteJecsPath(relativeOrAbsolutePath);
            return File.Exists(path);
        }


        internal static bool IsValidKey(string potentialKey) => IsValidKey(potentialKey, out _);
        internal static bool IsValidKey(string potentialKey, out string whyNot)
        {
            whyNot = null;

            if (potentialKey.IsNullOrEmpty())
                whyNot = "JECS keys must contain at least one character";
            else if (potentialKey[0] == '-')
                whyNot = "JECS keys may not begin with the character '-'";
            else if (potentialKey.Contains(':'))
                whyNot = "JECS keys may not contain the character ':'";
            else if (potentialKey.Contains('#'))
                whyNot = "JECS keys may not contain the character '#'";
            else if (potentialKey.ContainsNewLine())
                whyNot = "JECS keys cannot contain a newline";
            else if (potentialKey[0] == ' ' || potentialKey[potentialKey.Length - 1] == ' ')
                whyNot = "JECS keys may not start or end with a space";

            return whyNot == null;
        }

        /// <summary> Controls how JECS saves line endings. </summary>
        public static LineEndingStyle LineEndingStyle { get; set; } = LineEndingStyle.PlatformDefault;
        internal static string NewLine
        {
            get
            {
                switch (LineEndingStyle)
                {
                    case LineEndingStyle.Unix:
                        return "\n";
                    case LineEndingStyle.Windows:
                        return "\r\n";
                    case LineEndingStyle.PlatformDefault: default:
                        return Environment.NewLine;
                }
            }
        }

        internal static string NullIndicator { get; } = "null";
    }

    /// <summary>
    /// Different ways of saving line endings.
    /// </summary>
    public enum LineEndingStyle
    {
        /// <summary> Line endings are Windows style (CR LF) if on Windows and Unix style (LF) everywhere else. </summary>
        PlatformDefault = 0,

        /// <summary> Line endings are Unix style (LF) </summary>
        Unix,

        /// <summary> Line endings are Windows style (CR LF) </summary>
        Windows,
    }
}