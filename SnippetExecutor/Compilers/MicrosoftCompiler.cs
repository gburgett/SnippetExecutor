using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using NppPluginNET;
using System.Xml;
using System.Collections;

namespace SnippetExecutor.Compilers
{
    public abstract class MicrosoftCompiler : AbstractSnippetCompiler
    {

        private static readonly string[] understands
            = new string[]
                  {
                      "verbosecompile"
                  };
        public override string[] UnderstoodArguments
        {
            get
            {
                return understands;
            }
        }
		
		protected abstract CodeDomProvider getCompiler();



        private static string DllBase = null;
        private static Hashtable KnownDlls = new Hashtable();

        private static void loadDllConfig()
        {
            if (DllBase == null || KnownDlls.Count == 0)
            {
                using (System.IO.FileStream fs = System.IO.File.Open("LibraryReferences.xml", System.IO.FileMode.Open))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(fs);

                    XmlNode dllBase = doc.SelectSingleNode("DllBase");
                    if (dllBase != null) MicrosoftCompiler.DllBase = dllBase.InnerText;

                    foreach(XmlNode node in doc.SelectNodes("DllRef"))
                    {
                        KnownDlls[node.Attributes["namespace"].Value] = node.InnerText;
                    }
                }
            }
        }


        private const string quote = "\"";
        private string[] getAssemblies()
        {
            string assemblies = this.options["include"] as string;
            List<string> ret = new List<string>();
            
            int startIndex = assemblies.IndexOf(quote);
            int endIndex;
            while (startIndex > -1)
            {
                endIndex = assemblies.IndexOf(quote, startIndex + 1);
                if (endIndex <= -1)
                {
                    throw new Exception("unmatched '" + quote + "' in includes");
                }

                ret.Add(assemblies.Substring(startIndex + 1, (endIndex - startIndex - 1)));

                //end of string
                if (endIndex >= assemblies.Length - 1) break;

                startIndex = assemblies.IndexOf(quote, endIndex + 1);

                //split the between "" by string
                ret.AddRange(
                    assemblies.Substring(endIndex+1, (startIndex > -1) ? (startIndex - endIndex - 1) : (assemblies.Length - endIndex - 1))
                    .Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
            }

            for (int i = 0; i < ret.Count; i++)
            {
                if(KnownDlls.ContainsKey(ret[i]))
                {
                    //replace with known path
                    ret[i] = (string)KnownDlls[ret[i]];
                }
                else if(!System.IO.File.Exists(ret[i]) 
                    && DllBase != null
                    && !System.Text.RegularExpressions.Regex.IsMatch(ret[i], @"[a-zA-Z]:[\\/]"))
                {
                    //look in the DllBase path
                    string path = DllBase + ret[i];
                    if (!ret[i].EndsWith(".dll"))
                        path += ".dll";

                    if (System.IO.File.Exists(path))
                    {
                        ret[i] = path;
                    }
                }
                //else just got to hope they provided the full path
            }

            return ret.ToArray();
        }

        public override Object Compile(string toCompile, string options)
        {
            
            CodeDomProvider codeProvider = getCompiler();
            CompilerParameters p;
            string[] assemblies = getAssemblies();
            if(assemblies.Length > 0)
                p = new CompilerParameters(assemblies);
            else
                p = new CompilerParameters();
            p.IncludeDebugInformation = true;
            p.GenerateExecutable = true;
            //p.GenerateInMemory = true;
            CompilerResults compiled = codeProvider.CompileAssemblyFromSource(p, toCompile);

            console.writeLine("Compiled! " + compiled.PathToAssembly);
            
            if (compiled.Errors.HasErrors || compiled.Errors.HasWarnings)
            {
                console.writeLine("Errors!");
                foreach(var error in compiled.Errors)
                {
                    console.writeLine(error.ToString());
                }
            }

            if (this.options.ContainsKey("verbosecompile") &&
                (this.options["verbosecompile"] as string).Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string s in compiled.Output)
                {
                    String s2 = s.Trim();
                    if (!String.IsNullOrEmpty(s2))
                    {
                        console.writeLine(s2);
                    }
                }
            }

            if (compiled.Errors.HasErrors) return null;

            return compiled;
        }

        protected override string getArgs(object executable, string args)
        {
            return args;
        }

        protected override string cmdToExecute(object executable, string args)
        {
            return (executable as CompilerResults).PathToAssembly;
        }

    }

}
