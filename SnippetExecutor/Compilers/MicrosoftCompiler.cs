using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using NppPluginNET;

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

        public override Object Compile(string toCompile, string options)
        {
            
            CodeDomProvider codeProvider = getCompiler();
            CompilerParameters p = new CompilerParameters();
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
