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
            new Regex(@"^ *(?<access>public|private|internal|protected|protected internal) +(?<static>static|const|readonly|static readonly)? *(?<type>[a-zA-Z_]\S*) +(?<name>\S+)(?: ?= ?(?<value>.+))?;");

        private static Regex methodsRegex =
            new Regex(@"^ *(?<access>public|private|internal|protected|protected internal)? *(?<override>static|virtual|abstract|override|abstract override|sealed override|new)? *(?<type>[a-zA-Z_]\S*) +(?<name>\S+)\((?<params>[^,]+(?:,[^,]+)*)?\)");

        private static Regex classesRegex =
            new Regex(@"");

        private void stripComments(string line, ref StringBuilder output)
        {
            
            output.Clear();
            int index = 0;
            while(index < line.Length)
            {
                int lineComment = line.IndexOf("//", index);
                int blockComment = line.IndexOf("/*", index);
                if(lineComment > -1)
                {
                    //is there a prior block comment?
                    if(blockComment <= -1 || blockComment > lineComment)
                    {
                        //no, escape.
                        output.Append(line.Substring(index, lineComment - index));
                        return;
                    }
                }
                if(blockComment > -1)
                {
                    //append from the index to the blockcomment
                    output.Append(line.Substring(index, blockComment - index));
                    //advance the index to the end of the block comment
                    int end = line.IndexOf("*/", blockComment);
                    if (end > -1 && end + 2 < line.Length)
                    {
                        index = end + 2;
                    }
                    else
                    {
                        //block goes to end of line
                        return;
                    }

                    continue;
                }

                //if no block or line comments,
                output.Append(line.Substring(index));
                return;

            }
            
        }



        public override string PrepareSnippet(string snippetText)
        {
            string toCompile = TemplateLoader.getTemplate(this.lang);

            StringBuilder sb = new StringBuilder();
            StringBuilder temp = new StringBuilder();
            string[] lines = snippetText.Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int snippetStart = 0;
            int snippetEnd = lines.Length;

            #region using statements
            string usingCmd = "using";
            int usingEnd = 0;
            for (int i = snippetStart; i < lines.Length; i++)
            {
                string l = lines[i].Trim();
                stripComments(l, ref temp);
                l = temp.ToString();

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
            snippetStart = usingEnd;
            #endregion

            #region fields
            int fieldsEnd = snippetStart;
            for (int i = snippetStart; i < lines.Length; i++ )
            {
                string l = lines[i].Trim();
                stripComments(l, ref temp);
                l = temp.ToString().ToLower();

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
            for(int i = snippetStart; i < fieldsEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }
            toCompile = TemplateLoader.insertSnippet("fields", sb.ToString(), toCompile);
            snippetStart = fieldsEnd;
            #endregion

            #region methods & classes
            //go find the first method, then read until end or found first class
            int methodsStart = snippetStart;
            int methodsEnd = snippetStart;
            bool foundFirstMethod = false;
            for (int i = snippetStart; i < lines.Length; i++)
            {
                string l = lines[i].Trim();
                stripComments(l, ref temp);
                l = temp.ToString().ToLower();

                if (!String.IsNullOrEmpty(l))   //ignore whitespace lines
                {
                    if (foundFirstMethod)
                    {
                        //TODO: find classes
                        //if(classesRegex.IsMatch(l))
                        //{
                        //    methodsEnd = i;
                        //    break;
                        //}
                    }
                    else
                    {
                        Match m = methodsRegex.Match(l);
                        if (m.Success)
                        {
                            Main.debug.writeLine("method: " + m.Value);
                            foundFirstMethod = true;
                            methodsStart = i;
                        }
                    }
                }
            }
            //if we found a method
            if (foundFirstMethod)
            {
                snippetEnd = methodsStart;
            
                //did we get to the end of the file without finding the method end
                if (methodsEnd == snippetStart)
                {
                    methodsEnd = lines.Length;

                }
            }

            Main.debug.writeLine(String.Format("start: {0}, methodsStart {1}, methodsEnd {2}, end {3}", snippetStart,
                                               methodsStart, methodsEnd, snippetEnd));

            sb.Clear();
            for (int i = methodsStart; i < methodsEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }
            toCompile = TemplateLoader.insertSnippet("methods", sb.ToString(), toCompile);
            
            

            #endregion

            

            sb.Clear();
            for(int i = snippetStart; i < snippetEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }

            toCompile = TemplateLoader.insertSnippet("snippet", sb.ToString(), toCompile);
            toCompile = TemplateLoader.removeOtherSnippets(toCompile);
            return toCompile;
        }
    }

}
