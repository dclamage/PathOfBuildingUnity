using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class AsyncTextureLoader : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        TextureLoadHandle textureLoadHandle;
        while (createTextureQueue.TryDequeue(out textureLoadHandle))
        {
            textureLoadHandle.Execute();
        }
    }

    public ITextureLoadHandle Load(byte[] data)
    {
        TextureLoadHandle textureLoadHandle = new TextureLoadHandle(data);
        createTextureQueue.Enqueue(textureLoadHandle);
        return textureLoadHandle;
    }

    public int QueueCount => createTextureQueue.Count;

    public interface ITextureLoadHandle
    {
        Texture2D Texture { get; }
        int Width { get; }
        int Height { get; }
        bool Complete { get; }
        void Cancel();
    }

    public class TextureLoadHandle : ITextureLoadHandle
    {
        public TextureLoadHandle(byte[] data)
        {
            this.data = data;
        }

        public bool Complete
        {
            get
            {
                lock (locker)
                {
                    return error || canceled || _texture != null;
                }
            }
        }
        public Texture2D Texture
        {
            get
            {
                lock (locker)
                {
                    return error || canceled ? null : _texture;
                }
            }
        }

        public int Width { get; private set; } = 0;
        public int Height { get; private set; } = 0;

        public void Execute()
        {
            lock (locker)
            {
                if (!canceled)
                {
                    _texture = new Texture2D(2, 2);
                    error = !_texture.LoadImage(data);
                    if (!error)
                    {
                        Width = _texture.width;
                        Height = _texture.height;
                    }
                    else
                    {
                        Destroy(_texture);
                        _texture = null;
                    }
                }
                data = null;
            }
        }

        public void Cancel()
        {
            lock (locker)
            {
                if (_texture != null)
                {
                    Destroy(_texture);
                    _texture = null;
                }
                canceled = true;
                data = null;
            }
        }

        private Object locker = new Object();
        private byte[] data;
        private Texture2D _texture = null;
        private bool error = false;
        private bool canceled = false;
    }

    private ConcurrentQueue<TextureLoadHandle> createTextureQueue = new ConcurrentQueue<TextureLoadHandle>();
}
