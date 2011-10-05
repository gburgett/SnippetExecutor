using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace SnippetExecutor
{
    class CSharpCompiler : ISnippetCompiler
    {
        public Writer writer { set; private get; }

        private CompilerResults compiled;

        public bool Compile(string text, string options)
        {
            
            string toCompile = string.Concat(preSnippet, text.ToString(), postSnippet);
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters p = new CompilerParameters();
            p.IncludeDebugInformation = true;
            p.GenerateExecutable = true;
            //p.GenerateInMemory = false;
            compiled = codeProvider.CompileAssemblyFromSource(p, toCompile);

            writer.writeLine("Compiled! " + compiled.PathToAssembly);
            
            if (compiled.Errors.HasErrors || compiled.Errors.HasWarnings)
            {
                writer.writeLine("Errors!");
            }

            StringBuilder output = new StringBuilder();
            output.AppendLine();
            foreach (string s in compiled.Output)
            {
                output.AppendLine(s);
            }

            writer.writeLine(output.ToString());

            return true;
        }

        public bool execute(string args)
        {
            Process processObj = new Process();

            if(compiled == null) throw new Exception("compile first");

            processObj.StartInfo.FileName = compiled.PathToAssembly;
            processObj.StartInfo.Arguments = args;
            processObj.StartInfo.UseShellExecute = false;
            processObj.StartInfo.CreateNoWindow = true;

            writer.writeLine();
            processObj.Start();

            while(!processObj.HasExited)
            {
                System.Threading.Thread.Sleep(50);
                writer.write(processObj.StandardOutput.ReadToEnd());
            }

            processObj.WaitForExit();
            writer.write(processObj.StandardOutput.ReadToEnd());

            writer.writeLine("\r\n\r\nFinished, ");

            return true;
        }

        private const string preSnippet =
            "using System;"                                                         + "\r\n" +
            "using System.Collections.Generic;"                                     + "\r\n" +
            "using System.Text;"                                                    + "\r\n" +
                                                                                      "\r\n" +
            "namespace ConsoleApplication1"                                         + "\r\n" +
            "{"                                                                     + "\r\n" +
            "   class Program"                                                      + "\r\n" +
            "   {"                                                                  + "\r\n" +
            "       static void Main(string[] args)"                                + "\r\n" +
            "       {"                                                              + "\r\n";

        private const string postSnippet =
                                                                                      "\r\n" +
            "           System.Console.WriteLine(\"Finished, hit a key to end\");"  + "\r\n" +
            "           System.Console.ReadKey(true);"                              + "\r\n" +
            "       }"                                                              + "\r\n" +
            "       private static void WL(object text, params object[] args)"      + "\r\n" +
            "       {"                                                              + "\r\n" +
            "           Console.WriteLine(text.ToString(), args);"                  + "\r\n" +
            "       }"                                                              + "\r\n" +
            "       private static void RL()"                                       + "\r\n" +
            "       {"                                                              + "\r\n" +
            "           Console.ReadLine();"                                        + "\r\n" +
            "       }"                                                              + "\r\n" +
            "   }"                                                                  + "\r\n" +
            "}";

    }

}
