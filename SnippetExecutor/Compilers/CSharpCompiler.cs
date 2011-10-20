using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using NppPluginNET;
using System.Text.RegularExpressions;

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
			return new CSharpCodeProvider(new Dictionary<string, string>()
			                            {
			                                {"CompilerVersion","v4.0"}
			                            });
		}

        private static Regex fieldsRegex =
            new Regex(@"^ *(?<access>public|private|internal|protected|protected internal) +(?<static>static|const|readonly|static readonly)? *(?<type>\S+) +(?<name>\S+)(?: ?= ?(?<value>.+))?;");

        private static Regex methodsRegex =
            new Regex(@"^ *(?<access>public|private|internal|protected|protected internal)? *(?<override>static|virtual|abstract|override|abstract override|sealed override|new)? *(?<type>\S+) +(?<name>\S+)\((?<params>[^,]+(?:,[^,]+)*)?\)");

        private static Regex classesRegex =
            new Regex(@"");

        public override string PrepareSnippet(string snippetText)
        {
            string toCompile = TemplateLoader.getTemplate(this.lang);

            StringBuilder sb = new StringBuilder();
            string[] lines = snippetText.Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;
            int end = lines.Length;

            #region using statements
            string usingCmd = "using";
            int usingEnd = 0;
            for (int i = start; i < lines.Length; i++)
            {
                string l = lines[i].Trim();
                if (!String.IsNullOrEmpty(l))   //ignore whitespace lines
                {
                    if (l.ToLower().StartsWith(usingCmd))
                    {
                        if (l.EndsWith(";"))
                        {
                            string ns = l.Substring(usingCmd.Length, l.Length - usingCmd.Length - 1).Trim();
                            //append only if we can find it
                            if(!string.IsNullOrEmpty(this.getDllRef(ns)))
                                //need to surround with "", in case the ref contains spaces.
                                sb.Append('"').Append(ns).Append('"').Append(" ");
                        }
                    }
                    else
                    {
                        usingEnd = i;
                        break;
                    }
                }
            }
            if(sb.Length > 0)
            {
                string existingIncludes = (string)this.options["include"];
                if (string.IsNullOrWhiteSpace(existingIncludes))
                {
                    this.options["include"] = sb.ToString();
                }
                else
                {
                    this.options["include"] = existingIncludes + " " + sb.ToString();
                }
            }

            sb.Clear();
            for(int i = 0; i < usingEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }
            toCompile = TemplateLoader.insertSnippet("imports", sb.ToString(), toCompile);
            start = usingEnd;
            #endregion

            #region fields
            int fieldsEnd = start;
            for (int i = start; i < lines.Length; i++ )
            {
                string l = lines[i].Trim().ToLower();
                if (!String.IsNullOrEmpty(l))   //ignore whitespace lines
                {
                    Match m = fieldsRegex.Match(l);
                    if (!m.Success) //this line is the start of non-field declarations
                    {
                        fieldsEnd = i;
                        break;
                    }
                }
            }

            sb.Clear();
            for(int i = start; i < fieldsEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }
            toCompile = TemplateLoader.insertSnippet("fields", sb.ToString(), toCompile);
            start = fieldsEnd;
            #endregion

            #region methods & classes
            //go find the first method, then read until end or found first class
            int methodsStart = start;
            int methodsEnd = start;
            bool foundFirstMethod = false;
            for (int i = start; i < lines.Length; i++)
            {
                string l = lines[i].Trim().ToLower();
                if (!String.IsNullOrEmpty(l))   //ignore whitespace lines
                {
                    Match m = methodsRegex.Match(l);
                    if (m.Success) 
                    {
                        foundFirstMethod = true;
                        methodsStart = i;
                    }
                    else if(foundFirstMethod)
                    {
                        //TODO: find classes
                        //if(classesRegex.IsMatch(l))
                        //{
                        //    methodsEnd = i;
                        //    break;
                        //}
                    }
                }
            }
            //did we get to the end of the file without finding the method end
            if(methodsEnd == start)
            {
                methodsEnd = lines.Length;
            }

            sb.Clear();
            for (int i = methodsStart; i < methodsEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }
            toCompile = TemplateLoader.insertSnippet("methods", sb.ToString(), toCompile);
            end = methodsStart;
            #endregion

            sb.Clear();
            for(int i = start; i < end; i++)
            {
                sb.AppendLine(lines[i]);
            }

            toCompile = TemplateLoader.insertSnippet("snippet", sb.ToString(), toCompile);
            toCompile = TemplateLoader.removeOtherSnippets(toCompile);
            return toCompile;
        }
    }

}
