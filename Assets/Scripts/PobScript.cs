using System;
using System.Linq;
using System.Text;
using UnityEngine;
using LuaExtensions;
using MoonSharp.Interpreter;
using System.IO;

namespace PathOfBuilding
{
    public class PobScript : Script
    {
        public static void StaticInit()
        {
            if (!initCalled)
            {
                GlobalOptions.Platform = new PobPlatformAccessor(basePath);
                UserData.RegisterAssembly();
                initCalled = true;
            }
        }
        private static bool initCalled = false;

        public PobScript() : base()
        {
            Options.ScriptLoader = new PobScriptLoader();
            Globals["SetMainObject"] = (Action<Table>)SetMainObject;
            Globals["SetWindowTitle"] = (Action<string>)SetWindowTitle;
            Globals["ConExecute"] = (Action<string>)ConExecute;
            Globals["ConClear"] = (Action)ConClear;
            Globals["ConPrintf"] = (Action<DynValue[]>)ConPrintf;
            Globals["GetTime"] = (Func<int>)GetTime;
            Globals["RenderInit"] = (Action)RenderInit;
            Globals["PLoadModule"] = (Func<DynValue[], DynValue>)PLoadModule;
            Globals["LoadModule"] = (Func<DynValue[], DynValue>)LoadModule;
            Globals["PCall"] = (Func<DynValue[], DynValue>)PCall;
            Globals["bit"] = typeof(BitOps);
            Globals["GetCurl"] = (Func<DynValue>)GetCurl;
        }

        protected void SetMainObject(Table mainObject)
        {
            Debug.Log($"SetMainObject: {mainObject}");
            MainObject = mainObject;
        }

        protected void SetWindowTitle(string windowTitle)
        {
            Debug.Log($"SetWindowTitle: {windowTitle}");
        }

        protected void ConExecute(string executeString)
        {
            Debug.Log($"ConExecute: {executeString}");
        }

        protected void ConClear()
        {
            Debug.Log($"ConClear");
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
                        switch (formatStr[nextPercent + 1])
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
                Debug.Log($"ConPrintf: {finalStr}");
            }
        }

        protected int GetTime()
        {
            return (int)Math.Round((DateTime.UtcNow - epochTime).TotalMilliseconds);
        }

        protected void RenderInit()
        {
            Debug.Log($"RenderInit");
        }

        protected static DynValue ErrorTuple(string err)
        {
            Debug.LogError(err);
            return DynValue.NewTuple(DynValue.NewString(err), DynValue.NewNil());
        }

        protected static DynValue SuccessTuple(DynValue value)
        {
            return DynValue.NewTupleNested(DynValue.NewNil(), value);
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

        protected DynValue GetCurl()
        {
            return new Curl(this).Table();
        }

        public Table MainObject { get; protected set; }
        private static readonly string basePath = @"C:\ProgramData\Path of Building\";
        private static readonly DateTime epochTime = DateTime.UtcNow;
    }
}
