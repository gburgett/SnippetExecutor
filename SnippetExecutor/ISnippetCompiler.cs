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

        Options options { set; get; }

        bool Compile(String text, string options);

        bool execute(string args);
    }

    interface Writer
    {
        void write(String s);

        void writeLine();

        void writeLine(String s);
    }

    [Flags]
    enum Options
    {
        none = 1 << 0,
        VerboseCompile = 1 << 1,
        Timings = 1 << 2,

    }

}
