using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;

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
            if (Texture != null)
            {
                UnityEngine.Object.Destroy(Texture);
                Texture = null;
            }
        }

        public bool IsValid()
        {
            ApplyLoadedTexture();
            return textureLoadHandle != null || Texture != null;
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
                Texture = textureLoadHandle.Texture;
                width = textureLoadHandle.Width;
                height = textureLoadHandle.Height;
                textureLoadHandle = null;
            }
        }

        public Texture2D Texture { get; private set; } = null;

        private int width = 0;
        private int height = 0;
        private AsyncTextureLoader.ITextureLoadHandle textureLoadHandle = null;
    }
}
