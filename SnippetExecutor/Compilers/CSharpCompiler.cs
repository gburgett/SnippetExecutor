using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using NppPluginNET;

namespace SnippetExecutor.Compilers
{
    class CSharpCompiler : AbstractSnippetCompiler
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

        protected override LangType lang
        {
            get { return LangType.L_CS; }
        }

        public override Object Compile(string toCompile, string options)
        {
            
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters p = new CompilerParameters();
            p.IncludeDebugInformation = true;
            p.GenerateExecutable = true;
            //p.GenerateInMemory = false;
            CompilerResults compiled = codeProvider.CompileAssemblyFromSource(p, toCompile);

            io.writeLine("Compiled! " + compiled.PathToAssembly);
            
            if (compiled.Errors.HasErrors || compiled.Errors.HasWarnings)
            {
                io.writeLine("Errors!");
                foreach(var error in compiled.Errors)
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


        private const string RunFromFileInsert =
            @"
            try{
                using (System.IO.StreamReader reader = new System.IO.StreamReader(System.IO.File.OpenRead(args[0])))
                {
                    List<string> a = new List<string>();
		            StreamReader reader = new StreamReader(File.OpenRead(""));
		            while(!reader.EndOfStream){
			            string line = reader.ReadLine();
			            String[] moreargs = line.Split(new String[]{"" ""}, StringSplitOptions.RemoveEmptyEntries);
			            for(int j = 0; j < moreargs.Length; j++){
				            a.Add(moreargs[j]);
			            }
		            }
                    args = a.ToArray();
                }
            }
            catch(IOException ex)
            {
                WL(""Cannot open file "" + args[0]);
            }
            ";
    }

}
