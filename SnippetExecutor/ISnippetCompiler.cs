using System;
using System.Collections.Generic;
using System.Text;

namespace SnippetExecutor
{
    interface ISnippetCompiler
    {
        Writer writer
        {
            set;
        }

        bool Compile(String text, string options);

        bool execute(string args);
    }

    interface Writer
    {
        void write(String s);

        void writeLine();

        void writeLine(String s);
    }

}
