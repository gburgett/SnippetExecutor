using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;
using System.CodeDom.Compiler;
using Microsoft.VisualBasic;

namespace SnippetExecutor.Compilers
{
    class VBCompiler : MicrosoftCompiler
    {

        protected override LangType lang
        {
            get { return LangType.L_VB; }
        }

        protected override CodeDomProvider getCompiler()
		{
			return new VBCodeProvider(new Dictionary<string, string>()
			                            {
			                                {"CompilerVersion","v4.0"}
			                            });
		}

        public override string PrepareSnippet(string snippetText)
        {
            string toCompile = TemplateLoader.getTemplate(this.lang);


            //parse usings
            string usingCmd = "imports";
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
                        if (!l.EndsWith(";"))
                        {
                            string ns = l.Substring(usingCmd.Length, l.Length - usingCmd.Length).Trim();
                            //append only if we can find it
                            if (!string.IsNullOrEmpty(this.getDllRef(ns)))
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
            if (sb.Length > 0)
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
            for (int i = 0; i < usingEnd; i++)
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
