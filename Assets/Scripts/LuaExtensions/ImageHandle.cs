using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UI;

namespace PathOfBuilding
{
    [MoonSharpUserData(AccessMode = InteropAccessMode.Preoptimized)]
    public class ImageHandle
    {
        public ImageHandle()
        {

        }

        ~ImageHandle()
        {
            Unload();
        }

        public void Load(string path)
        {
            string fullPath = path;
            if (Path.DirectorySeparatorChar != '/')
            {
                fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
            }
            if (Path.DirectorySeparatorChar != '\\')
            {
                fullPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);
            }
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(PobScript.basePath, path);
            }
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"ImageHandle: Could not find file {fullPath}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            textureLoadHandle = PobScript.asyncTextureLoader.Load(bytes);
        }

        public void Unload()
        {
            if (textureLoadHandle != null)
            {
                textureLoadHandle.Cancel();
                textureLoadHandle = null;
            }
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
        }

        public bool IsValid()
        {
            ApplyLoadedTexture();
            return textureLoadHandle != null || texture != null;
        }

        public bool IsLoading()
        {
            ApplyLoadedTexture();
            return (textureLoadHandle != null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Public API")]
        public void SetLoadingPriority(double priority)
        {
            // Do nothing
        }

        public DynValue ImageSize()
        {
            ApplyLoadedTexture();
            return DynValue.NewTuple(DynValue.NewNumber(width), DynValue.NewNumber(height));
        }

        private void ApplyLoadedTexture()
        {
            if (textureLoadHandle != null && textureLoadHandle.Complete)
            {
                texture = textureLoadHandle.Texture;
                width = textureLoadHandle.Width;
                height = textureLoadHandle.Height;
                textureLoadHandle = null;
            }
        }

        private Texture2D texture = null;
        private int width = 0;
        private int height = 0;
        private AsyncTextureLoader.ITextureLoadHandle textureLoadHandle = null;
    }
}
