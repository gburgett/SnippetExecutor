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
			return new CSharpCodeProvider(new Dictionary<string, string>()
			                            {
			                                {"CompilerVersion","v4.0"}
			                            });
		}


        public override string PrepareSnippet(string snippetText)
        {
            string toCompile = TemplateLoader.getTemplate(this.lang);


            //parse usings
            string usingCmd = "using";
            StringBuilder sb = new StringBuilder();
            string[] lines = snippetText.Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int usingEnd = 0;
            for (int i = 0; i < lines.Length; i++)
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
                                sb.Append(ns).Append(" ");
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

            toCompile = TemplateLoader.insertSnippet("snippet", snippetText.Substring(sb.Length), toCompile);
            toCompile = TemplateLoader.removeOtherSnippets(toCompile);
            return toCompile;
        }
    }

}
