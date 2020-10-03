using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using UnityEditor;

namespace PathOfBuilding
{
    class PobScriptLoader : UnityAssetsScriptLoader
    {
        public PobScriptLoader(string assetsPath = null) : base(assetsPath)
        {
            
        }

        protected override string ResolveModuleName(string modname, string[] paths)
        {
            return modname;
        }

        public override string ResolveModuleName(string modname, Table globalContext)
        {
            return modname;
        }
    }
}
