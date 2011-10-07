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
			return new VBCodeProvider();
		}
    }
}
