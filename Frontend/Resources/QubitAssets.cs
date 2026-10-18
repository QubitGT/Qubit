using System;
using System.IO;
using UnityEngine;

namespace Qubit.Frontend.Resources
{
   
    public static class QubitAssets
    {
        private static AssetBundle bundle;

        public static T Load<T>(string name) where T : UnityEngine.Object
        {
            if (bundle == null)
            {
                using var stream = typeof(QubitAssets).Assembly.GetManifestResourceStream( $"{PluginInfo.ClientResourcePath}.qubitui");
                if (stream == null) throw new InvalidOperationException("The Qubit UI bundle is missing. Run QubitInterfaceBuilder.Build in Unity.");
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                bundle = AssetBundle.LoadFromMemory(memory.ToArray());
                if (bundle == null) throw new InvalidOperationException("The Qubit UI bundle could not be loaded.");
            }

            var asset = bundle.LoadAsset<T>(name);
            if (asset == null) throw new InvalidOperationException("Missing Qubit UI asset: " + name);
            return asset;
        }
    }
}
