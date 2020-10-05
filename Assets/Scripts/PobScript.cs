using System;
using System.Linq;
using System.Text;
using UnityEngine;
using LuaExtensions;
using MoonSharp.Interpreter;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Ionic.Zlib;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

namespace PathOfBuilding
{
    public class PobScript : Script
    {
        public static void StaticInit(AsyncTextureLoader asyncTextureLoader)
        {
            if (!initCalled)
            {
                dataPath = Application.dataPath + "\\";
                PobScript.asyncTextureLoader = asyncTextureLoader;
                GlobalOptions.Platform = new PobPlatformAccessor(basePath);
                UserData.RegisterAssembly();
                initCalled = true;
            }
        }
        private static bool initCalled = false;

        public PobScript(Rendering rendering) : base()
        {
            this.rendering = rendering;
            Options.ScriptLoader = new PobScriptLoader();

            // Callbacks
            Globals["SetCallback"] = (Action<string, DynValue>)SetCallback;
            Globals["GetCallback"] = (Func<string, DynValue>)GetCallback;
            Globals["SetMainObject"] = (Action<DynValue>)SetMainObject;

            // Image handles
            Globals["NewImageHandle"] = (Func<DynValue>)(() => UserData.Create(new ImageHandle()));

            // Rendering
            if (rendering != null)
            {
                Globals["GetScreenSize"] = (Func<DynValue>)rendering.GetScreenSize;
                Globals["SetClearColor"] = (Action<DynValue[]>)rendering.SetClearColor;
                Globals["SetDrawLayer"] = (Action<DynValue[]>)rendering.SetDrawLayer;
                Globals["SetViewport"] = (Action<DynValue[]>)rendering.SetViewport;
                Globals["SetDrawColor"] = (Action<DynValue[]>)rendering.SetDrawColor;
                Globals["DrawImage"] = (Action<DynValue[]>)rendering.DrawImage;
                Globals["DrawImageQuad"] = (Action<DynValue[]>)rendering.DrawImageQuad;
                Globals["DrawString"] = (Action<DynValue[]>)rendering.DrawString;
                Globals["DrawStringWidth"] = (Func<DynValue[], DynValue>)rendering.DrawStringWidth;
                Globals["DrawStringCursorIndex"] = (Func<DynValue[], DynValue>)rendering.DrawStringCursorIndex;
                Globals["StripEscapes"] = (Func<DynValue[], DynValue>)rendering.StripEscapes;
                Globals["GetAsyncCount"] = (Func<DynValue>)GetAsyncCount;
                Globals["RenderInit"] = (Func<DynValue>)rendering.RenderInit;
            }

            // File Search
            Globals["NewFileSearch"] = (Func<DynValue[], DynValue>)FileSearch.NewFileSearch;

            // General function
            Globals["SetWindowTitle"] = (Action<string>)SetWindowTitle;
            Globals["GetCursorPos"] = (Func<DynValue>)GetCursorPos;
            Globals["SetCursorPos"] = (Action<DynValue[]>)SetCursorPos;
            Globals["ShowCursor"] = (Action<DynValue[]>)ShowCursor;
            Globals["IsKeyDown"] = (Func<DynValue[], DynValue>)IsKeyDown;
            Globals["Copy"] = (Action<DynValue[]>)Copy;
            Globals["Paste"] = (Func<DynValue>)Paste;
            Globals["Deflate"] = (Func<DynValue[], DynValue>)Deflate;
            Globals["Inflate"] = (Func<DynValue[], DynValue>)Inflate;
            Globals["GetTime"] = (Func<int>)GetTime;
            Globals["GetScriptPath"] = (Func<DynValue>)GetScriptPath;
            Globals["GetRuntimePath"] = (Func<DynValue>)GetRuntimePath;
            Globals["GetUserPath"] = (Func<DynValue>)GetUserPath;
            Globals["MakeDir"] = (Func<DynValue[], DynValue>)MakeDir;
            Globals["RemoveDir"] = (Func<DynValue[], DynValue>)RemoveDir;
            Globals["SetWorkDir"] = (Func<DynValue[], DynValue>)SetWorkDir;
            Globals["GetWorkDir"] = (Func<DynValue>)GetWorkDir;

            // TODO: SubScript
            Globals["LaunchSubScript"] = (Func<DynValue[], DynValue>)Unimplemented;
            Globals["AbortSubScript"] = (Func<DynValue[], DynValue>)Unimplemented;
            Globals["IsSubScriptRunning"] = (Func<DynValue[], DynValue>)Unimplemented;

            Globals["loadstring"] = (Func<DynValue[], DynValue>)LoadFromString;
            Globals["LoadModule"] = (Func<DynValue[], DynValue>)LoadModule;
            Globals["PLoadModule"] = (Func<DynValue[], DynValue>)PLoadModule;
            Globals["PCall"] = (Func<DynValue[], DynValue>)PCall;
            Globals["ConPrintf"] = (Action<DynValue[]>)ConPrintf;
            Globals["ConPrintTable"] = (Action<DynValue[]>)ConPrintTable;
            Globals["ConExecute"] = (Action<string>)ConExecute;
            Globals["ConClear"] = (Action)ConClear;
            Globals["print"] = (Action<DynValue[]>)Print;
            Globals["SpawnProcess"] = (Action<DynValue[]>)SpawnProcess;
            Globals["OpenURL"] = (Action<DynValue[]>)OpenURL;
            Globals["SetProfiling"] = (Action<DynValue[]>)SetProfiling;
            Globals["Restart"] = (Action)Restart;
            Globals["Exit"] = (Action)Exit;

            // Extension Libraries
            Globals["bit"] = typeof(BitOps);

            // Extras
            Globals["GetCurl"] = (Func<DynValue>)GetCurl;
            Globals["Sleep"] = (Action<DynValue>)Sleep;
        }

        // TEMP
        protected DynValue Unimplemented(params DynValue[] values)
        {
            return DynValue.Nil;
        }

        #region Callbacks
        protected void SetCallback(string name, DynValue func)
        {
            callbacks[name] = func;
        }

        protected DynValue GetCallback(string name)
        {
            DynValue func;
            if (callbacks.TryGetValue(name, out func))
            {
                return func;
            }
            return DynValue.Nil;
        }

        protected void SetMainObject(DynValue mainObject)
        {
            Debug.Log($"SetMainObject: {mainObject.ToDebugPrintString()}");
            MainObject = mainObject;
        }
        #endregion

        #region Rendering
        // Most rendering callbacks are in Rendering.cs

        public DynValue GetAsyncCount()
        {
            return DynValue.NewNumber(asyncTextureLoader.QueueCount);
        }
        #endregion

        #region General Functions
        protected void SetWindowTitle(string windowTitle)
        {
            Debug.Log($"SetWindowTitle: {windowTitle}");
        }

        protected DynValue GetCursorPos()
        {
            Vector3 mousePos = Input.mousePosition;
            Rect pixelRect = rendering.GetComponent<Graphic>().GetPixelAdjustedRect();
            int mouseX = (int)((mousePos.x / Screen.width) * pixelRect.width);
            int mouseY = (int)(((Screen.height - mousePos.y) / Screen.height) * pixelRect.height);
            return DynValue.NewTuple(DynValue.NewNumber(mouseX), DynValue.NewNumber(mouseY));
        }

        protected void SetCursorPos(params DynValue[] values)
        {
            // No thanks
            return;
        }

        protected void ShowCursor(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: ShowCursor(doShow)");
                return;
            }
            bool showCursor = (values[0].Type == DataType.Number || values[0].Type == DataType.Boolean) ? values[0].Boolean : true;
            Cursor.visible = showCursor;
        }

        protected DynValue IsKeyDown(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: IsKeyDown(keyName)");
                return DynValue.False;
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"IsKeyDown() argument 1: expected string, got {values[0].Type}");
                return DynValue.False;
            }
            string keyName = values[0].String;
            if (keyName.Length < 1)
            {
                Debug.LogError($"IsKeyDown() argument 1: string is empty");
                return DynValue.False;
            }
            bool isKeyDown = false;
            if (keyName.Length == 1)
            {
                keyName = keyName.ToLowerInvariant();
                isKeyDown = Input.GetKeyDown(keyName);
            }
            else
            {
                KeyCode keyCode = KeyCode.None;
                switch (keyName.ToUpperInvariant())
                {
                    case "LEFTBUTTON":
                        isKeyDown = Input.GetMouseButtonDown(0);
                        break;
                    case "MIDDLEBUTTON":
                        isKeyDown = Input.GetMouseButtonDown(2);
                        break;
                    case "RIGHTBUTTON":
                        isKeyDown = Input.GetMouseButtonDown(1);
                        break;
                    case "MOUSE4":
                        isKeyDown = Input.GetMouseButtonDown(4);
                        break;
                    case "MOUSE5":
                        isKeyDown = Input.GetMouseButtonDown(5);
                        break;
                    case "WHEELUP":
                        isKeyDown = Input.mouseScrollDelta.y > 0;
                        break;
                    case "WHEELDOWN":
                        isKeyDown = Input.mouseScrollDelta.y < 0;
                        break;
                    case "BACK":
                        keyCode = KeyCode.Backspace;
                        break;
                    case "TAB":
                        keyCode = KeyCode.Tab;
                        break;
                    case "RETURN":
                        keyCode = KeyCode.Return;
                        break;
                    case "ESCAPE":
                        keyCode = KeyCode.Escape;
                        break;
                    case "SHIFT":
                        isKeyDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
                        break;
                    case "CTRL":
                        isKeyDown = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
                        break;
                    case "ALT":
                        isKeyDown = Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
                        break;
                    case "PAUSE":
                        keyCode = KeyCode.Pause;
                        break;
                    case "PAGEUP":
                        keyCode = KeyCode.PageUp;
                        break;
                    case "PAGEDOWN":
                        keyCode = KeyCode.PageDown;
                        break;
                    case "END":
                        keyCode = KeyCode.End;
                        break;
                    case "HOME":
                        keyCode = KeyCode.Home;
                        break;
                    case "PRINTSCREEN":
                        keyCode = KeyCode.Print;
                        break;
                    case "INSERT":
                        keyCode = KeyCode.Insert;
                        break;
                    case "DELETE":
                        keyCode = KeyCode.Delete;
                        break;
                    case "UP":
                        keyCode = KeyCode.UpArrow;
                        break;
                    case "DOWN":
                        keyCode = KeyCode.DownArrow;
                        break;
                    case "LEFT":
                        keyCode = KeyCode.LeftArrow;
                        break;
                    case "RIGHT":
                        keyCode = KeyCode.RightArrow;
                        break;
                    case "F1":
                        keyCode = KeyCode.F1;
                        break;
                    case "F2":
                        keyCode = KeyCode.F2;
                        break;
                    case "F3":
                        keyCode = KeyCode.F3;
                        break;
                    case "F4":
                        keyCode = KeyCode.F4;
                        break;
                    case "F5":
                        keyCode = KeyCode.F5;
                        break;
                    case "F6":
                        keyCode = KeyCode.F6;
                        break;
                    case "F7":
                        keyCode = KeyCode.F7;
                        break;
                    case "F8":
                        keyCode = KeyCode.F8;
                        break;
                    case "F9":
                        keyCode = KeyCode.F9;
                        break;
                    case "F10":
                        keyCode = KeyCode.F10;
                        break;
                    case "F11":
                        keyCode = KeyCode.F11;
                        break;
                    case "F12":
                        keyCode = KeyCode.F12;
                        break;
                    case "F13":
                        keyCode = KeyCode.F13;
                        break;
                    case "F14":
                        keyCode = KeyCode.F14;
                        break;
                    case "F15":
                        keyCode = KeyCode.F15;
                        break;
                    case "NUMLOCK":
                        keyCode = KeyCode.Numlock;
                        break;
                    case "SCROLLLOCK":
                        keyCode = KeyCode.ScrollLock;
                        break;
                }
                if (keyCode != KeyCode.None)
                {
                    isKeyDown = Input.GetKeyDown(keyCode);
                }
            }
            return DynValue.NewBoolean(isKeyDown);
        }

        protected void Copy(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: Copy(string)");
                return;
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"Copy() argument 1: expected string, got {values[0].Type}");
                return;
            }
            GUIUtility.systemCopyBuffer = values[0].String;
        }

        protected DynValue Paste()
        {
            return DynValue.NewString(GUIUtility.systemCopyBuffer);
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string GetString(byte[] bytes)
        {
            char[] chars = new char[(bytes.Length + sizeof(char) - 1) / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        protected DynValue Deflate(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: Deflate(string)");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"Deflate() argument 1: expected string, got {values[0].Type}");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }

            byte[] decompressed = GetBytes(values[0].String);
            try
            {
                byte[] compressed = DeflateStream.CompressBuffer(decompressed);
                return DynValue.NewString(GetString(compressed));
            }
            catch (Exception e)
            {
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString($"{e}"));
            }
        }

        protected DynValue Inflate(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: Inflate(string)");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"Inflate() argument 1: expected string, got {values[0].Type}");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }

            byte[] compressed = GetBytes(values[0].String);
            try
            {
                byte[] decompressed = DeflateStream.UncompressBuffer(compressed);
                return DynValue.NewString(GetString(decompressed));
            }
            catch (Exception e)
            {
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(e.Message));
            }
        }

        protected int GetTime()
        {
            return (int)Math.Round((DateTime.UtcNow - epochTime).TotalMilliseconds);
        }

        protected DynValue GetScriptPath()
        {
            return DynValue.NewString(basePath);
        }

        protected DynValue GetRuntimePath()
        {
            return DynValue.NewString(dataPath);
        }

        protected DynValue GetUserPath()
        {
            // TODO: Cross Platform
            return DynValue.NewString(@"C:\ProgramData\");
        }

        protected DynValue MakeDir(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: MakeDir(path)");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"MakeDir() argument 1: expected string, got {values[0].Type}");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }

            string path = values[0].String;
            try
            {
                DirectoryInfo dir = Directory.CreateDirectory(path);
                if (dir == null || !dir.Exists)
                {
                    return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Failed to create directory."));
                }
                return DynValue.True;
            }
            catch (Exception e)
            {
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(e.Message));
            }
        }

        protected DynValue RemoveDir(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: RemoveDir(path)");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"RemoveDir() argument 1: expected string, got {values[0].Type}");
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString("Invalid arguments"));
            }

            string path = values[0].String;
            try
            {
                Directory.Delete(path, false);
                return DynValue.True;
            }
            catch (Exception e)
            {
                return DynValue.NewTuple(DynValue.Nil, DynValue.NewString(e.Message));
            }
        }

        protected DynValue SetWorkDir(params DynValue[] values)
        {
            throw new NotImplementedException("SetWorkDir not implemented!");
        }

        protected DynValue GetWorkDir()
        {
            return DynValue.NewString(Directory.GetCurrentDirectory());
        }

        protected DynValue LaunchSubScript(params DynValue[] values)
        {
            if (values.Length < 3)
            {
                Debug.LogError("Usage: LaunchSubScript(scriptText, funcList, subList[, ...])");
                return DynValue.Nil;
            }

            for (int i = 0; i < 3; i++)
            {
                if (values[i].Type != DataType.String)
                {
                    Debug.LogError($"LaunchSubScript() argument {i + 1}: expected string, got {values[i].Type}");
                    return DynValue.Nil;
                }
            }

            for (int i = 3; i < values.Length; i++)
            {
                var type = values[i].Type;
                if (type != DataType.Nil && type != DataType.Void && type != DataType.Boolean && type != DataType.Number && type != DataType.String)
                {
                    Debug.LogError($"LaunchSubScript() argument {i + 1}: only nil, boolean, number and string types can be passed to sub script");
                    return DynValue.Nil;
                }
            }

            PobScript newScript = new PobScript(null);

            return DynValue.Nil;
        }

        protected DynValue LoadFromString(params DynValue[] values)
        {
            if (values.Length == 0 || values[0].Type != DataType.String)
            {
                return ErrorTuple("LoadFromString called with invalid params");
            }
            return LoadString(values[0].String);
        }

        protected DynValue LoadModule(params DynValue[] values)
        {
            if (values.Length == 0 || values[0].Type != DataType.String)
            {
                return ErrorTuple("LoadModule called with invalid params");
            }
            string name = values[0].String;
            Debug.Log($"LoadModule {name} with {values.Length - 1} params");
            name = basePath + name;
            name = Path.ChangeExtension(name, "lua");
            string scriptCode = File.ReadAllText(name);
            if (scriptCode == null || scriptCode.Length == 0)
            {
                return ErrorTuple($"LoadModule: Could not load script from file {name}");
            }

            DynValue func = LoadString(scriptCode);
            if (func == null || func.Type != DataType.Function)
            {
                return ErrorTuple($"LoadModule: Script {name} failed to compile.");
            }
            return Call(func, values.Skip(1).ToArray());
        }

        protected DynValue PLoadModule(params DynValue[] values)
        {
            try
            {
                if (values.Length == 0 || values[0].Type != DataType.String)
                {
                    return ErrorTuple("PLoadModule called with invalid params");
                }
                string name = values[0].String;
                Debug.Log($"PLoadModule {name} with {values.Length - 1} params");
                name = basePath + name;
                name = Path.ChangeExtension(name, "lua");
                string scriptCode = File.ReadAllText(name);
                if (scriptCode == null || scriptCode.Length == 0)
                {
                    return ErrorTuple($"PLoadModule: Could not load script from file {name}");
                }

                DynValue func = LoadString(scriptCode);
                if (func == null || func.Type != DataType.Function)
                {
                    return ErrorTuple($"PLoadModule: Script {name} failed to compile.");
                }
                DynValue ret = Call(func, values.Skip(1).ToArray());
                return SuccessTuple(ret);
            }
            catch (Exception e)
            {
                return ErrorTuple($"PLoadModule exception: {e}");
            }
        }

        protected DynValue PCall(params DynValue[] values)
        {
            Debug.Log($"PCall with {values.Length} params");
            try
            {
                if (values.Length == 0)
                {
                    return ErrorTuple("PCall requires a parameter");
                }
                Debug.Log($"PCall type: {values[0].Type}");

                DynValue func = null;
                switch (values[0].Type)
                {
                    case DataType.String:
                        func = LoadString(values[0].String);
                        if (func == null || func.Type != DataType.Function)
                        {
                            return ErrorTuple("PCall script string failed to compile");
                        }
                        break;
                    case DataType.Function:
                        func = values[0];
                        break;
                }
                if (func != null && func.Type == DataType.Function)
                {
                    DynValue ret = func.Function.Call(values.Skip(1).ToArray());
                    return SuccessTuple(ret);
                }
                return ErrorTuple("PCall first param is bad type");
            }
            catch (Exception e)
            {
                return ErrorTuple($"PCall exception: {e}");
            }
        }

        private static DynValue ErrorTuple(string err)
        {
            Debug.LogError(err);
            return DynValue.NewTuple(DynValue.NewString(err), DynValue.Nil);
        }

        private static DynValue SuccessTuple(DynValue value)
        {
            return DynValue.NewTupleNested(DynValue.Nil, value);
        }

        protected void ConPrintf(params DynValue[] vals)
        {
            if (vals.Length == 0)
            {
                return;
            }
            if (vals.Length == 1)
            {
                Debug.Log($"ConPrintf: {vals[0].ToPrintString()}");
            }
            else
            {
                string formatStr = vals[0].ToPrintString();

                int valIndex = 1;
                StringBuilder finalStr = new StringBuilder(formatStr.Length);
                int nextPercent = formatStr.IndexOf('%');
                while (nextPercent >= 0)
                {
                    if (nextPercent > 0)
                    {
                        finalStr.Append(formatStr.Substring(0, nextPercent));
                        formatStr = formatStr.Substring(nextPercent);
                    }
                    if (formatStr.Length == 1)
                    {
                        finalStr.Append('%');
                        formatStr = "";
                    }
                    else
                    {
                        switch (formatStr[1])
                        {
                            case 's':
                                if (valIndex < vals.Length)
                                {
                                    finalStr.Append(vals[valIndex].ToPrintString());
                                }
                                else
                                {
                                    finalStr.Append("(null)");
                                }
                                break;
                            case 'd':
                                {
                                    double number = 0.0;
                                    if (valIndex < vals.Length && vals[valIndex].Type == DataType.Number)
                                    {
                                        number = vals[valIndex].Number;
                                    }
                                    finalStr.Append((int)number);
                                }
                                break;
                            case 'f':
                                {
                                    double number = 0.0;
                                    if (valIndex < vals.Length && vals[valIndex].Type == DataType.Number)
                                    {
                                        number = vals[valIndex].Number;
                                    }
                                    finalStr.Append(number);
                                }
                                break;
                            case '%':
                                finalStr.Append('%');
                                break;
                        }
                        valIndex++;
                        formatStr = formatStr.Substring(2);
                    }
                    nextPercent = formatStr.IndexOf('%');
                }
                finalStr.Append(formatStr);
                ConsolePrint(finalStr.ToString());
            }
        }

        protected void ConPrintTable(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: ConPrintTable(tbl[, noRecurse])");
                return;
            }
            if (values[0].Type != DataType.Table)
            {
                Debug.LogError($"ConPrintTable() argument 1: expected table, got {values[0].Type}");
                return;
            }
            bool recurse = values.Length == 1 || values[1].Type != DataType.Boolean || !values[1].Boolean;
            StringBuilder sb = new StringBuilder();
            TableToString(values[0].Table, recurse, sb);
            ConsolePrint(sb.ToString());
        }

        private void TableToString(Table table, bool recurse, StringBuilder sb)
        {
            foreach (DynValue key in table.Keys)
            {
                // Print key
                if (key.Type == DataType.String)
                {
                    sb.Append($"[\"{key.String}^7\"] = ");
                }
                else
                {
                    sb.Append($"{key.ToPrintString()} = ");
                }

                // Print value
                DynValue value = table.Get(key);
                if (value.Type == DataType.Table)
                {
                    if (recurse)
                    {
                        sb.Append($"{value.ToPrintString()} {{\n");
                        TableToString(value.Table, recurse, sb);
                        sb.Append("}\n");
                    }
                    else
                    {
                        sb.Append($"{value.ToPrintString()} {{ ... }}\n");
                    }
                }
                else if (value.Type == DataType.String)
                {
                    sb.Append($"\"{value.String}\"\n");
                }
                else
                {
                    sb.Append($"\"{value.ToPrintString()}\"\n");
                }
            }
        }

        protected void Print(params DynValue[] values)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(values[i].ToPrintString());
            }
            sb.Append("\n");
            ConsolePrint(sb.ToString());
        }

        protected void SpawnProcess(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: SpawnProcess(cmdName[, args])");
                return;
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"SpawnProcess() argument 1: expected string, got {values[0].Type}");
                return;
            }

            string process = values[0].String;
            string args = values.Length > 1 && values[1].Type == DataType.String ? values[1].String : "";

            // Don't want to
            Debug.LogError($"Not going to spawn process {values[0].String} {args}");
            return;
        }

        protected void OpenURL(params DynValue [] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: OpenURL(url)");
                return;
            }
            if (values[0].Type != DataType.String)
            {
                Debug.LogError($"OpenURL() argument 1: expected string, got {values[0].Type}");
                return;
            }

            Application.OpenURL(values[0].String);
        }

        protected void SetProfiling(params DynValue[] values)
        {
            if (values.Length < 1)
            {
                Debug.LogError("Usage: SetProfiling(isEnabled)");
                return;
            }

            // Unsupported
            Debug.Log($"SetProfiling: {values[0].ToPrintString()}");
        }

        protected void Restart()
        {
            Debug.Log("Restart called (unsupported)");
        }

        protected void Exit()
        {
            Application.Quit();
        }

        private void ConsolePrint(string s)
        {
            // TODO
            Debug.Log(s.TrimEnd('\n'));
        }

        protected void ConExecute(string executeString)
        {
            Debug.Log($"ConExecute: {executeString}");
        }

        protected void ConClear()
        {
            Debug.Log($"ConClear");
        }

        #endregion

        #region Extras
        protected DynValue GetCurl()
        {
            return new Curl(this).Table();
        }

        protected void Sleep(DynValue durationVal)
        {
            double ms = durationVal != null && durationVal.Type == DataType.Number ? durationVal.Number : 0;
            Thread.Sleep((int)Math.Round(ms));
        }
        #endregion

        public DynValue MainObject { get; protected set; }
        private Dictionary<string, DynValue> callbacks = new Dictionary<string, DynValue>();
        private Rendering rendering = null;
        private int nextSubScriptTaskId = 1;
        private Dictionary<int, Task<DynValue>> subScriptTasks = new Dictionary<int, Task<DynValue>>();
        private static string dataPath = null;
        public static readonly string basePath = @"C:\ProgramData\Path of Building\";
        public static AsyncTextureLoader asyncTextureLoader = null;
        private static readonly DateTime epochTime = DateTime.UtcNow;
    }
}
