using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UI;
using PathOfBuilding;
using UnityEngine.UIElements.Experimental;

[RequireComponent(typeof(Graphic), typeof(RectTransform))]
public class Rendering : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject imagePrefab = null;
    [SerializeField]
    private GameObject textPrefab = null;

    private Graphic graphic = null;
    private RectTransform rectTransform = null;

    void Start()
    {
        graphic = GetComponent<Graphic>();
        rectTransform = GetComponent<RectTransform>();

        mainThread = Thread.CurrentThread;
        whiteTexture = Texture2D.whiteTexture;

        Rect pixelRect = graphic.GetPixelAdjustedRect();
        int width = (int)pixelRect.width;
        int height = (int)pixelRect.height;
        viewportW = width;
        viewportH = height;
    }

    public void PreScriptUpdate()
    {
        ResetState();
    }

    public void PostScriptUpdate()
    {
        Draw();
    }

    private void Draw()
    {
        foreach (var layer in layers)
        {
            foreach (var sublayer in layer.Value)
            {
                foreach (var action in sublayer.Value)
                {
                    action.Invoke();
                }
            }
        }
        layers.Clear();
    }

    public DynValue GetScreenSize()
    {
        ThrowIfNotMainThread();
        Rect pixelRect = graphic.GetPixelAdjustedRect();
        return DynValue.NewTuple(DynValue.NewNumber(pixelRect.width), DynValue.NewNumber(pixelRect.height));
    }

    public void SetClearColor(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        float r = values.Length >= 1 && values[0].Type == DataType.Number ? (float)values[0].Number : 0f;
        float g = values.Length >= 2 && values[1].Type == DataType.Number ? (float)values[1].Number : 0f;
        float b = values.Length >= 3 && values[2].Type == DataType.Number ? (float)values[2].Number : 0f;
        float a = values.Length >= 4 && values[3].Type == DataType.Number ? (float)values[3].Number : 1f;
        clearColor = new Color(r, g, b, a);
    }

    public void SetDrawLayer(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length == 0)
        {
            Debug.LogError("Usage: SetDrawLayer({layer|nil}[, subLayer])");
            return;
        }
        if (values[0].Type != DataType.Number)
        {
            Debug.LogError($"SetDrawLayer(): layer must be a number, got {values[0].Type}");
            return;
        }
        if (values.Length >= 2 && values[1].Type != DataType.Number)
        {
            Debug.LogError($"SetDrawLayer(): sublayer must be a number, got {values[1].Type}");
            return;
        }

        if (values[0].IsNil())
        {
            if (values.Length < 2)
            {
                Debug.LogError("SetDrawLayer(): must provide subLayer if layer is nil");
                return;
            }
            drawSubLayer = (int)values[1].Number;
        }
        else if (values[0].Type == DataType.Number)
        {
            drawLayer = (int)values[0].Number;
            if (values.Length >= 2)
            {
                drawSubLayer = (int)values[1].Number;
            }
        }
        else
        {
            Debug.LogError($"SetDrawLayer(): layer must be nil or a number, got {values[1].Type}");
        }
    }

    public void SetViewport(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length > 0 && values.Length < 4)
        {
            Debug.LogError("Usage: SetViewport([x, y, width, height])");
            return;
        }
        Rect pixelRect = graphic.GetPixelAdjustedRect();

        int newViewportX, newViewportY, newViewportW, newViewportH;
        if (values.Length >= 4 &&
            values[0].Type == DataType.Number &&
            values[1].Type == DataType.Number &&
            values[2].Type == DataType.Number &&
            values[3].Type == DataType.Number)
        {
            newViewportX = (int)values[0].Number;
            newViewportY = (int)values[1].Number;
            newViewportW = (int)values[2].Number;
            newViewportH = (int)values[3].Number;

            if (viewportW == 0 || viewportH == 0)
            {
                newViewportW = (int)pixelRect.width;
                newViewportH = (int)pixelRect.height;
            }
        }
        else
        {
            newViewportX = 0;
            newViewportY = 0;
            newViewportW = (int)pixelRect.width;
            newViewportH = (int)pixelRect.height;
        }
        AppendCommand(() =>
        {
            viewportX = newViewportX;
            viewportY = newViewportY;
            viewportW = newViewportW;
            viewportH = newViewportH;
        });
    }

    public void SetDrawColor(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length == 0)
        {
            Debug.LogError("Usage: SetDrawColor(red, green, blue[, alpha]) or SetDrawColor(escapeStr)");
            return;
        }

        Color newDrawColor = Color.white;
        if (values[0].Type == DataType.String)
        {
            string colorStr = values[0].String;
            if (ColorHelper.IsColorEscape(colorStr) == 0)
            {
                Debug.LogError("SetDrawColor() argument 1: invalid color escape sequence");
                return;
            }
            newDrawColor = ColorHelper.ReadColorEscape(colorStr);
        }
        else
        {
            if (values.Length < 3)
            {
                Debug.LogError("Usage: SetDrawColor(red, green, blue[, alpha]) or SetDrawColor(escapeStr)");
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                if (values[i].Type != DataType.Number)
                {
                    Debug.LogError($"SetDrawColor() argument {i + 1}: expected number, got {values[i].Type}");
                    return;
                }
                newDrawColor[i] = (float)values[i].Number;
            }
            if (values.Length >= 4)
            {
                if (values[3].Type != DataType.Number)
                {
                    Debug.LogError($"SetDrawColor() argument 4: expected number, got {values[3].Type}");
                    return;
                }
                newDrawColor.a = (float)values[3].Number;
            }
        }
        AppendCommand(() =>
        {
            drawColor = newDrawColor;
        });
    }

    public void DrawImage(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length < 5)
        {
            Debug.LogError("Usage: DrawImage({imgHandle|nil}, left, top, width, height[, tcLeft, tcTop, tcRight, tcBottom])");
            return;
        }
        if (!values[0].IsNil() && values[0].Type != DataType.UserData && values[0].UserData.Object is ImageHandle)
        {
            string type = values[0].Type.ToString();
            if (values[0].Type == DataType.UserData)
            {
                type = values[0].UserData.Object.GetType().Name;
            }
            Debug.LogError($"DrawImage() argument 1: expected image handle or nil, got {type}");
            return;
        }

        Texture texture = null;
        if (!values[0].IsNil())
        {
            if (values[0].UserData.Object is ImageHandle imageHandle)
            {
                if (!imageHandle.IsLoading() && imageHandle.IsValid())
                {
                    texture = imageHandle.Texture;
                }
            }
        }

        float[] arg = new float[8];
        if (values.Length > 5)
        {
            if (values.Length < 9)
            {
                Debug.LogError("DrawImage(): incomplete set of texture coordinates provided");
                return;
            }
            for (int i = 1; i < 9; i++)
            {
                if (values[i].Type != DataType.Number)
                {
                    Debug.LogError($"DrawImage() argument {i + 1}: expected number, got {values[i].Type}");
                    return;
                }
                arg[i - 1] = (float)values[i].Number;
            }
        }
        else
        {
            for (int i = 1; i < 5; i++)
            {
                if (values[i].Type != DataType.Number)
                {
                    Debug.LogError($"DrawImage() argument {i + 1}: expected number, got {values[i].Type}");
                    return;
                }
                arg[i - 1] = (float)values[i].Number;
            }
            arg[4] = 0f;
            arg[5] = 0f;
            arg[6] = 1f;
            arg[7] = 1f;
        }

        float[] quadArg = new float[16];
        quadArg[0] = arg[0];
        quadArg[1] = arg[1];
        quadArg[2] = arg[0] + arg[2];
        quadArg[3] = arg[1];
        quadArg[4] = arg[0] + arg[2];
        quadArg[5] = arg[1] + arg[3];
        quadArg[6] = arg[0];
        quadArg[7] = arg[1] + arg[3];
        quadArg[8] = arg[4];
        quadArg[9] = arg[5];
        quadArg[10] = arg[6];
        quadArg[11] = arg[5];
        quadArg[12] = arg[6];
        quadArg[13] = arg[7];
        quadArg[14] = arg[5];
        quadArg[15] = arg[7];
        AppendCommand(() => InternalDrawImageQuad(texture, quadArg));

    }

    public void DrawImageQuad(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length < 9)
        {
            Debug.LogError("Usage: DrawImageQuad({imgHandle|nil}, x1, y1, x2, y2, x3, y3, x4, y4[, s1, t1, s2, t2, s3, t3, s4, t4])");
            return;
        }
        if (!values[0].IsNil() && values[0].Type != DataType.UserData && values[0].UserData.Object is ImageHandle)
        {
            string type = values[0].Type.ToString();
            if (values[0].Type == DataType.UserData)
            {
                type = values[0].UserData.Object.GetType().Name;
            }
            Debug.LogError($"DrawImageQuad() argument 1: expected image handle or nil, got {type}");
            return;
        }

        Texture texture = null;
        if (!values[0].IsNil())
        {
            if (values[0].UserData.Object is ImageHandle imageHandle)
            {
                if (!imageHandle.IsLoading() && imageHandle.IsValid())
                {
                    texture = imageHandle.Texture;
                }
            }
        }

        float[] arg = new float[16];
        if (values.Length > 9)
        {
            if (values.Length < 17)
            {
                Debug.LogError("DrawImageQuad(): incomplete set of texture coordinates provided");
                return;
            }
            for (int i = 1; i < 17; i++)
            {
                if (values[i].Type != DataType.Number)
                {
                    Debug.LogError($"DrawImageQuad() argument {i + 1}: expected number, got {values[i].Type}");
                    return;
                }
                arg[i - 1] = (float)values[i].Number;
            }
        }
        else
        {
            for (int i = 1; i < 9; i++)
            {
                if (values[i].Type != DataType.Number)
                {
                    Debug.LogError($"DrawImageQuad() argument {i + 1}: expected number, got {values[i].Type}");
                    return;
                }
                arg[i - 1] = (float)values[i].Number;
            }
            arg[8] = 0f;
            arg[9] = 0f;
            arg[10] = 1f;
            arg[11] = 0f;
            arg[12] = 1f;
            arg[13] = 1f;
            arg[14] = 0f;
            arg[15] = 1f;
        }

        AppendCommand(() => InternalDrawImageQuad(texture, arg));
    }

    public void DrawString(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length < 6)
        {
            Debug.LogError("Usage: DrawString(left, top, align, height, font, text)");
            return;
        }
        if (values[0].Type != DataType.Number)
        {
            Debug.LogError($"DrawString() argument 1: expected number, got {values[0].Type}");
            return;
        }
        if (values[1].Type != DataType.Number)
        {
            Debug.LogError($"DrawString() argument 2: expected number, got {values[1].Type}");
            return;
        }
        if (values[2].Type != DataType.String && !values[2].IsNil())
        {
            Debug.LogError($"DrawString() argument 3: expected string or nil, got {values[2].Type}");
            return;
        }
        if (values[3].Type != DataType.Number)
        {
            Debug.LogError($"DrawString() argument 4: expected number, got {values[3].Type}");
            return;
        }
        if (values[4].Type != DataType.String)
        {
            Debug.LogError($"DrawString() argument 5: expected string, got {values[4].Type}");
            return;
        }
        if (values[5].Type != DataType.String)
        {
            Debug.LogError($"DrawString() argument 6: expected string, got {values[5].Type}");
            return;
        }

        float x = (float)values[0].Number;
        float y = (float)values[1].Number;
        string align = values[2].IsNil() ? "LEFT" : values[2].String;
        int height = (int)values[3].Number;
        Color color = drawColor;
        string font = values[4].String;
        string str = values[5].String;

        // TODO: Draw
    }

    public DynValue DrawStringWidth(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length < 3)
        {
            Debug.LogError("Usage: DrawStringWidth(height, font, text)");
            return DynValue.Nil;
        }
        if (values[0].Type != DataType.Number)
        {
            Debug.LogError($"DrawStringWidth() argument 1: expected number, got {values[0].Type}");
            return DynValue.Nil;
        }
        if (values[1].Type != DataType.String)
        {
            Debug.LogError($"DrawStringWidth() argument 2: expected string, got {values[1].Type}");
            return DynValue.Nil;
        }
        if (values[2].Type != DataType.String)
        {
            Debug.LogError($"DrawStringWidth() argument 3: expected string, got {values[2].Type}");
            return DynValue.Nil;
        }

        int height = (int)values[0].Number;
        string font = values[1].String;
        string str = values[2].String;
        // TODO: Text width
        // TEMP
        return DynValue.NewNumber(height * str.Length);
    }

    public DynValue DrawStringCursorIndex(params DynValue[] values)
    {
        ThrowIfNotMainThread();
        if (values.Length < 5)
        {
            Debug.LogError("Usage: DrawStringCursorIndex(height, font, text, cursorX, cursorY)");
            return DynValue.Nil;
        }
        if (values[0].Type != DataType.Number)
        {
            Debug.LogError($"DrawString() argument 1: expected number, got {values[0].Type}");
            return DynValue.Nil;
        }
        if (values[1].Type != DataType.String)
        {
            Debug.LogError($"DrawString() argument 2: expected string, got {values[1].Type}");
            return DynValue.Nil;
        }
        if (values[2].Type != DataType.String)
        {
            Debug.LogError($"DrawString() argument 3: expected string, got {values[2].Type}");
            return DynValue.Nil;
        }
        if (values[3].Type != DataType.Number)
        {
            Debug.LogError($"DrawString() argument 4: expected number, got {values[3].Type}");
            return DynValue.Nil;
        }
        if (values[4].Type != DataType.Number)
        {
            Debug.LogError($"DrawString() argument 5: expected number, got {values[4].Type}");
            return DynValue.Nil;
        }

        int height = (int)values[0].Number;
        string font = values[1].String;
        string text = values[2].String;
        int cursorX = (int)values[3].Number;
        int cursorY = (int)values[4].Number;

        // TODO
        return DynValue.NewNumber(0);
    }

    public DynValue StripEscapes(params DynValue[] values)
    {
        if (values.Length < 1)
        {
            Debug.LogError("Usage: StripEscapes(string)");
            return DynValue.Nil;
        }
        if (values[0].Type != DataType.String)
        {
            Debug.Log($"StripEscapes() argument 1: expected string, got {values[0].Type}");
            return DynValue.Nil;
        }
        string str = values[0].String;
        StringBuilder strip = new StringBuilder(str.Length);
        while (str.Length > 0)
        {
            int esclen = ColorHelper.IsColorEscape(str);
            if (esclen > 0)
            {
                str = str.Substring(esclen);
            }
            else
            {
                strip.Append(str[0]);
                str = str.Substring(1);
            }
        }
        return DynValue.NewString(strip);
    }

    public DynValue RenderInit()
    {
        return DynValue.NewNumber(1);
    }

    private void InternalDrawImageQuad(Texture texture, float[] args)
    {
        if (texture == null)
        {
            texture = whiteTexture;
        }
#if false
        imageMaterial.SetColor("_Color", drawColor);
        imageMaterial.SetTexture("_MainTex", texture);

        float renderWidth = renderTexture.width;
        float renderHeight = renderTexture.height;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            vertices[i] = new Vector3(
                (viewportX + args[i * 2 + 0]) / renderWidth * 2f - 1f,
                (viewportY + args[i * 2 + 1]) / renderHeight * -2f + 1f,
                0);
        }
        mesh.vertices = vertices;
        mesh.triangles = quadIndices;
        mesh.normals = null;

        Vector2[] uvs = new Vector2[4]
        {
            new Vector3(args[8], args[9], 0),
            new Vector3(args[10], args[11], 0),
            new Vector3(args[12], args[13], 0),
            new Vector3(args[14], args[15], 0),
        };
        mesh.uv = uvs;

        Graphics.SetRenderTarget(renderTexture);
        imageMaterial.SetPass(0);
        Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
#endif
    }

    private void ThrowIfNotMainThread()
    {
        if (Thread.CurrentThread != mainThread)
        {
            throw new InvalidOperationException("Must be called from the main Unity thread");
        }
    }

    private Thread mainThread = null;
    private Texture2D whiteTexture = null;

    // State
    void ResetState()
    {
        clearColor = Color.black;
        drawColor = Color.white;
        drawLayer = 0;
        drawSubLayer = 0;

        Rect pixelRect = graphic.GetPixelAdjustedRect();
        viewportX = 0;
        viewportY = 0;
        viewportW = (int)pixelRect.width;
        viewportH = (int)pixelRect.height;
    }
    private Color clearColor = Color.black;
    private Color drawColor = Color.white;
    private int drawLayer = 0;
    private int drawSubLayer = 0;
    private int viewportX = 0;
    private int viewportY = 0;
    private int viewportW = 0;
    private int viewportH = 0;

    private void AppendCommand(Action action)
    {
        SortedDictionary<int, List<Action>> subLayer;
        if (!layers.TryGetValue(drawLayer, out subLayer))
        {
            subLayer = new SortedDictionary<int, List<Action>>();
            layers[drawLayer] = subLayer;
        }

        List<Action> actions;
        if (!subLayer.TryGetValue(drawSubLayer, out actions))
        {
            actions = new List<Action>();
            subLayer[drawSubLayer] = actions;
        }
        actions.Add(action);
    }
    private SortedDictionary<int, SortedDictionary<int, List<Action>>> layers = new SortedDictionary<int, SortedDictionary<int, List<Action>>>();

    private static int[] quadIndices = new int[6]
    {
        0, 3, 1,
        3, 2, 1
    };
}
