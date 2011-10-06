using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;
using System.CodeDom.Compiler;
using Microsoft.VisualBasic;

namespace SnippetExecutor.Compilers
{
    class VBCompiler : AbstractSnippetCompiler
    {

        protected override LangType lang
        {
            get { return LangType.L_VB; }
        }

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


        public override Object Compile(string toCompile, string options)
        {
            
            VBCodeProvider codeProvider = new VBCodeProvider();
            CompilerParameters p = new CompilerParameters();
            p.IncludeDebugInformation = true;
            p.GenerateExecutable = true;
            //p.GenerateInMemory = false;
            CompilerResults compiled = codeProvider.CompileAssemblyFromSource(p, toCompile);

            io.writeLine("Compiled! " + compiled.PathToAssembly);

            if (compiled.Errors.HasErrors || compiled.Errors.HasWarnings)
            {
                io.writeLine("Errors!");
                foreach (var error in compiled.Errors)
                {
                    io.writeLine(error.ToString());
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
                        io.writeLine(s2);
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
