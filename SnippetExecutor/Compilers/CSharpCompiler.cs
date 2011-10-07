using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using NppPluginNET;

namespace SnippetExecutor.Compilers
{
    class CSharpCompiler : MicrosoftCompiler
    {

        protected override LangType lang
        {
            get { return LangType.L_CS; }
        }

		protected override CodeDomProvider getCompiler()
		{
			return new CSharpCodeProvider();
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
