using System;
using System.IO;
using UnityEngine;

namespace JECS.UnityStuff
{
    public static class ResourcesUtilities
    {
        public static string ReadTextFromFile(string filePath)
        {
            var textAsset = Resources.Load<TextAsset>(filePath);
            if (textAsset == null)
                throw new FileNotFoundException("The default file you specified doesn't exist in Resources :(");

            var text = textAsset.text;
            Resources.UnloadAsset(textAsset);

            return text;
        }
    }
}
