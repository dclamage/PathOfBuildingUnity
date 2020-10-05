using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using MoonSharp.Interpreter;
using PathOfBuilding;
using UnityEngine.UI;

public class LuaInit : MonoBehaviour
{
    [Header("Engine References")]
    [SerializeField]
    private GameObject asyncTextureLoader = null;

    // Start is called before the first frame update
    void Start()
    {
        PobScript.StaticInit(asyncTextureLoader.GetComponent<AsyncTextureLoader>());
        rendering = GetComponent<Rendering>();
        launchScript = new PobScript(rendering);
    }

    // Update is called once per frame
    async void Update()
    {
        if (scriptState == ScriptState.Initial)
        {
            scriptState = ScriptState.OnInitRunning;
            try
            {
                await Task.Run(InitScript);
                scriptState = ScriptState.OnInitSuccess;
            }
            catch (Exception e)
            {
                Debug.LogError($"OnInit failed: {e}");
                scriptState = ScriptState.OnInitFailed;
            }
        }

        if (scriptState == ScriptState.OnInitSuccess)
        {
            rendering.PreScriptUpdate();
            MainObjectCall("OnFrame");
            rendering.PostScriptUpdate();
        }
    }

    private DynValue MainObjectCall(string functionName, params DynValue[] paramValues)
    {
        DynValue mainObject = launchScript.MainObject;
        if (mainObject == null || mainObject.Type != DataType.Table)
        {
            return DynValue.Nil;
        }

        DynValue func = mainObject.Table.Get(functionName);
        if (func == null || func.Type != DataType.Function)
        {
            return DynValue.Nil;
        }

        return func.Function.Call(new DynValue[] { mainObject }.Concat(paramValues).ToArray());
    }

    private async Task InitScript()
    {
        string launchScriptCode;
        using (StreamReader launchScriptReader = File.OpenText(@"C:\ProgramData\Path of Building\Launch.lua"))
        {
            launchScriptCode = await launchScriptReader.ReadToEndAsync();
        }
        if (launchScriptCode.StartsWith("#@ SimpleGraphic"))
        {
            launchScriptCode = launchScriptCode.Substring("#@ SimpleGraphic".Length);
        }

        launchScript.DoString(launchScriptCode);
        MainObjectCall("OnInit");
    }

    private PobScript launchScript;
    private Rendering rendering;
    
    enum ScriptState
    {
        Initial,
        OnInitRunning,
        OnInitSuccess,
        OnInitFailed
    }
    private ScriptState scriptState;
}
