using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using NppPluginNET;
using System.Xml;
using System.Collections;
using System.IO;

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


        static MicrosoftCompiler()
        {
            MicrosoftCompiler.loadDllConfig();
        }

		
		protected abstract CodeDomProvider getCompiler();



        private static string DllBase = null;
        private static Hashtable KnownDlls = new Hashtable();
        private static List<String> DefaultDlls = new List<String>();

        private static void loadDllConfig()
        {
            if (DllBase == null || KnownDlls.Count == 0)
            {
                using (System.IO.FileStream fs = System.IO.File.OpenRead("plugins/SnippetExecutor/LibraryReferences.xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(fs);

                    XmlNode dllBase = doc.SelectSingleNode("/References/DotNet/DllBase");
                    if (dllBase != null) MicrosoftCompiler.DllBase = dllBase.InnerText;

                    foreach(XmlNode node in doc.SelectNodes("DllRef"))
                    {
                        string value = node.Attributes["namespace"].Value;
                        if(!string.IsNullOrEmpty(value))
                            KnownDlls[value] = node.InnerText;
                    }

                    foreach (XmlNode node in doc.SelectNodes("/References/DotNet/DefaultDll"))
                    {
                        string s;
                        if (!string.IsNullOrEmpty(s = node.InnerText))
                        {
                            DefaultDlls.Add(s);
                        }
                    }
                }
            }
        }


        private const string quote = "\"";
        protected string[] getAssemblies()
        {
            List<string> ret = new List<string>();

            //add assemblies which are included by default
            ret.AddRange(DefaultDlls);
            

            string assemblies = this.options["include"] as string;
            if (String.IsNullOrEmpty(assemblies)) return ret.ToArray();

            
            int startIndex = assemblies.IndexOf(quote);
            int endIndex;
            if (startIndex != 0)
            {
                //need to split the first bit on whitespace
                ret.AddRange(
                    assemblies.Substring(0, (startIndex > -1) ? (startIndex) : assemblies.Length)
                    .Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
            }

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
                string s = getDllRef(ret[i]);
                if (string.IsNullOrEmpty(s))
                {
                    console.errLine("cannot find DLL for " + ret[i]);
                    ret[i] = string.Empty;
                }
                else
                {
                    ret[i] = s;
                }
            }

            ret.RemoveAll(x => string.IsNullOrEmpty(x));

            //get distinct values
            Dictionary<string, bool> Distinct = new Dictionary<string, bool>();
            foreach (string value in ret)
            {
                Distinct[value] = true;
            }

            ret.Clear();
            ret.AddRange(Distinct.Keys);

            
            return ret.ToArray();
        }

        protected string getDllRef(string reference)
        {
            if(isDllPath(reference))
            {
                return reference;
            }
            else if(KnownDlls.ContainsKey(reference))
            {
                //replace with known reference
                return (string)KnownDlls[reference];
            }
            else if (DllBase != null)
            {
                //look in the DllBase path
                if (!reference.EndsWith(".dll"))
                    reference += ".dll";

                string path = DllBase + reference;

                if (System.IO.File.Exists(path))
                {
                    //return the dll name
                    return reference;
                }
            }

            //else can't find it, return nothing.
            return String.Empty;
        }

        protected bool isDllPath(string reference)
        {
            return reference.EndsWith(".dll") && System.IO.File.Exists(reference);
        }

        public override Object Compile(string toCompile, string options)
        {
            
            CodeDomProvider codeProvider = getCompiler();
            CompilerParameters p = new CompilerParameters();
            string[] assemblies = getAssemblies();
            if (assemblies.Length > 0)
            {
                console.write("Assemblies: ");
                for (int i = 0; i < assemblies.Length; i++)
                {
                    console.write(assemblies[i] + " ");
                    
                }
                console.writeLine();

                p.ReferencedAssemblies.AddRange(assemblies);
            }

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

            

            return new Dictionary<string, object>()
                       {
                           {"parameters", p},
                           {"results",compiled}
                       };
        }

        protected override string getArgs(object executable, string args)
        {
            return args;
        }

        protected override string cmdToExecute(object executable, string args)
        {
            return ((executable as Dictionary<string, object>)["results"] as CompilerResults).PathToAssembly;
        }

        protected override void preStart(object executable, string args)
        {
            //copy dlls to run path
            Dictionary<string, object> ex = (executable as Dictionary<string, object>);
            CompilerResults compiled = (ex["results"] as CompilerResults);
            CompilerParameters parameters =
                (ex["parameters"] as CompilerParameters);

            DirectoryInfo copyTo = new System.IO.FileInfo(compiled.PathToAssembly).Directory;

            List<string> copiedPaths = new List<String>();
            foreach(string s in parameters.ReferencedAssemblies)
            {   
                if(this.isDllPath(s))
                {
                    //copy to compiled dll path
                    FileInfo fi = new System.IO.FileInfo(s);
                    File.Copy(s, Path.Combine(copyTo.FullName, fi.Name));
                    copiedPaths.Add(s);
                }
            }
            ex["copiedPaths"] = copiedPaths;

        }

        public override bool cleanup(SnippetInfo info)
        {
            Dictionary<string, object> ex = (info.executable as Dictionary<string, object>);
            List<string> toDelete = (ex["copiedPaths"] as List<string>);
            for(int i = 0; i < toDelete.Count; i++)
            {
                File.Delete(toDelete[i]);
            }

            CompilerResults cr = (ex["results"] as CompilerResults);
            cr.TempFiles.Delete();
            File.Delete(cr.PathToAssembly);

            return true;
        }
    }

}
