using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;

namespace SnippetExecutor
{


    public interface IO : IDisposable
    {
        void write(String s);

        void writeLine();

        void writeLine(String s);

        int read();

        string readLine();
    }
    

    internal class IOAppendCurrentDoc : IO
    {
        IntPtr currScint
        {
            get { return PluginBase.GetCurrentScintilla(); }
        }

        public void write(string s)
        {
            Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, s.Length, s);
        }

        public void writeLine()
        {
            Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, 2, "\r\n");
        }

        public void writeLine(string s)
        {
            String o = String.Concat(s, "\r\n");
            Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, o.Length, o);
        }

        public int read()
        {
            throw new NotImplementedException();
        }

        public string readLine()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }
    }

    internal class IODoc : IO
    {
        readonly IntPtr currScint;
        private int bufferId = 0;

        private StringBuilder toAppend = new StringBuilder();

        private Boolean isMyBuffer;

        public IODoc()
        {
            this.currScint = PluginBase.GetCurrentScintilla();
            this.bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            isMyBuffer = true;

            NppNotification.BufferActivated += onBufferActivated;
            NppNotification.FileBeforeClose -= onBufferActivated;
        }

        public IODoc(int bufferId)
        {
            this.currScint = PluginBase.GetCurrentScintilla();
            this.bufferId = bufferId;
            if (this.bufferId == (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0))
                isMyBuffer = true;
            else
                isMyBuffer = false;

            NppNotification.BufferActivated += onBufferActivated;
            NppNotification.FileBeforeClose -= onBufferActivated;
        }


        private void onBufferActivated(SCNotification scn)
        {
            if (this.bufferId == (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0))
                isMyBuffer = true;
            else
                isMyBuffer = false;


            if (isMyBuffer && toAppend.Length > 0)
            {
                lock (toAppend)
                {
                    Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, toAppend.Length, toAppend.ToString());
                    toAppend.Clear();
                }
            }
        }


        public void write(string s)
        {
            if (isMyBuffer)
                Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, s.Length, s);
            else
            {
                lock (toAppend)
                {
                    toAppend.Append(s);
                }
            }
        }

        public void writeLine()
        {
            write("\r\n");
        }

        public void writeLine(string s)
        {
            String o = String.Concat(s, "\r\n");
            write(o);
        }

        public int read()
        {
            throw new NotImplementedException();
        }

        public string readLine()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            NppNotification.BufferActivated -= onBufferActivated;
        }
    }

    internal class IONull : IO
    {

        public void write(string s)
        {

        }

        public void writeLine()
        {

        }

        public void writeLine(string s)
        {

        }

        public int read()
        {
            return -1;
        }

        public string readLine()
        {
            return null;
        }

        public void Dispose()
        {

        }
    }
}
