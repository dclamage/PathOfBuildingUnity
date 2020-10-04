using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using MoonSharp.Interpreter;
using PathOfBuilding;

public class LuaInit : MonoBehaviour
{
    [Header("Engine References")]
    [SerializeField]
    private GameObject asyncTextureLoader = null;

    // Start is called before the first frame update
    async void Start()
    {
        PobScript.StaticInit(asyncTextureLoader.GetComponent<AsyncTextureLoader>());
        launchScript = new PobScript();
        await Task.Run(async () => await InitScript());
    }

    // Update is called once per frame
    void Update()
    {

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

        Table mainObject = launchScript.MainObject;
        if (mainObject == null)
        {
            Debug.LogError("No main object set!");
            return;
        }

        object onInitObj = mainObject["OnInit"];
        if (onInitObj == null)
        {
            Debug.LogError("No OnInit found!");
            return;
        }

        Closure onInit = onInitObj as Closure;
        if (onInit == null)
        {
            Debug.LogError("OnInit not a Closure!");
            return;
        }

        onInit.Call(mainObject);
    }

    private PobScript launchScript;
}
