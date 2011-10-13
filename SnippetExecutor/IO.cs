using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NppPluginNET;
using System.IO;
using System.Threading;

namespace SnippetExecutor
{


    public interface IO : IDisposable
    {
        void write(char c);

        void write(String s);

        void writeLine();

        void writeLine(String s);

        void err(char c);

        void err(String s);

        void errLine();

        void errLine(String s);

        TextWriter stdIn { get; set; }
    }

    internal abstract class IOTimedBuffer : IO
    {

        private Timer tWrite;
        private Timer tErr;
        private StringBuilder writeBuffer = new StringBuilder(50);
        private StringBuilder errBuffer = new StringBuilder(50);

        private void timeoutWrite(object state)
        {
            string toWrite;
            lock (writeBuffer)
            {
                if (writeBuffer.Length > 0)
                {
                    toWrite = writeBuffer.ToString();
                    writeBuffer.Clear();
                }
                else
                {
                    tWrite.Dispose();
                    tWrite = null;
                    return;
                }
            }
            writeImpl(toWrite);
        }

        private void timeoutErr(object state)
        {
            string toWrite;
            lock (errBuffer)
            {
                if (errBuffer.Length > 0)
                {
                    toWrite = errBuffer.ToString();
                    errBuffer.Clear();
                }
                else
                {
                    tErr.Dispose();
                    tErr = null;
                    return;
                }
            }
            errImpl(toWrite);
        }

        protected abstract void writeImpl(string s);

        public void write(char c)
        {
            lock (writeBuffer)
            {
                writeBuffer.Append(c);
                if (tWrite == null)
                {
                    tWrite = new Timer(timeoutWrite, writeBuffer, 50, 50);
                }
            }
        }

        public void write(string s)
        {
            lock (writeBuffer)
            {
                writeBuffer.Append(s);
                if (tWrite == null)
                {
                    tWrite = new Timer(timeoutWrite, writeBuffer, 50, 50);
                }
            }
        }

        public void writeLine()
        {
            write(Environment.NewLine);
        }

        public void writeLine(string s)
        {
            write(String.Concat(s, Environment.NewLine));
        }

        public void err(char c)
        {
            lock (errBuffer)
            {
                errBuffer.Append(c);
                if (tErr == null)
                {
                    tErr = new Timer(timeoutErr, errBuffer, 50, 50);
                }
            }
        }

        protected abstract void errImpl(String s);

        public void err(string s)
        {
            lock (errBuffer)
            {
                errBuffer.Append(s);
                if (tErr == null)
                {
                    tErr = new Timer(timeoutErr, errBuffer, 50, 50);
                }
            }
        }

        public void errLine()
        {
            err(Environment.NewLine);
        }

        public void errLine(string s)
        {
            err(string.Concat(s, Environment.NewLine));
        }

        public virtual TextWriter stdIn
        {
            get; set;
        }

        public virtual void Dispose()
        {
        }
    }


    internal class IOAppendCurrentDoc : IOTimedBuffer
    {
        private IntPtr currScint;

        public IOAppendCurrentDoc()
        {
            currScint = PluginBase.GetCurrentScintilla();
            SciNotificationEvents.CharAdded += this.onCharAdded;
        }

        protected override void writeImpl(string s)
        {
            Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, s.Length, s);
        }

        protected override void errImpl(String s)
        {
            writeImpl(s);
        }

        private void onCharAdded(SCNotification sn)
        {
            if (stdIn != null)
                stdIn.Write(sn.ch);
        }

        public override void Dispose()
        {
            SciNotificationEvents.CharAdded -= this.onCharAdded;
        }

        ~IOAppendCurrentDoc()
        {
            Dispose();
        }
    }

    internal class IODoc : IOTimedBuffer
    {
        readonly IntPtr currScint;
        private int bufferId = 0;

        private StringBuilder toAppend = new StringBuilder();

        private Boolean isVisible;

        protected static event NotificationEvent BufferChanged;

        public readonly string path;

        static IODoc()
        {
            NppNotificationEvents.BufferActivated += onBufferActivated;
            NppNotificationEvents.FileBeforeClose += onBufferActivated;

        }

        public IODoc(string path)
        {
            this.currScint = PluginBase.GetCurrentScintilla();
            this.bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            if(bufferId0 == 0)
                bufferId0 = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETBUFFERIDFROMPOS, 0, 0);
            if (bufferId1 == 0)
                bufferId1 = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETBUFFERIDFROMPOS, 0, 1);

            isVisible = true;

            this.path = path;

            IODoc.BufferChanged += onBufferChanged;
            SciNotificationEvents.CharAdded += onCharAdded;
        }

        public IODoc() : this(string.Empty)
        {
            
        }



        protected IODoc(int bufferId) : this(bufferId, string.Empty)
        {
        }

        protected IODoc(int bufferId, string path)
        {
            this.currScint = PluginBase.GetCurrentScintilla();
            this.bufferId = bufferId;
            if (this.bufferId == (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0))
                isVisible = true;
            else
                isVisible = false;

            this.path = path;

            IODoc.BufferChanged += onBufferChanged;
            SciNotificationEvents.CharAdded += onCharAdded;
        }

        private static System.Collections.Hashtable docs = new System.Collections.Hashtable();

        public static IODoc ForBuffer(int bufferId)
        {
            return ForBuffer(bufferId, string.Empty);
        }

        public static IODoc ForBuffer(int bufferId, string path)
        {
            IODoc ret = null;
            if(docs.ContainsKey(bufferId))
            {
                ret = (IODoc)docs[bufferId];
            }

            if (ret == null)
            {
                ret = new IODoc(bufferId, path);
            }

            return ret;
        }

        private static int bufferId0 = 0;
        private static int bufferId1;


        private static void onBufferActivated(SCNotification scn)
        {
            bufferId0 = (int) Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETBUFFERIDFROMPOS, 0, 0);
            bufferId1 = (int) Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETBUFFERIDFROMPOS, 0, 1);


            if(BufferChanged != null)
                BufferChanged(scn);
        }

        private void onBufferChanged(SCNotification scn)
        {
            if (this.bufferId == bufferId0 || this.bufferId == bufferId1)
                isVisible = true;
            else
                isVisible = false;


            if (isVisible && toAppend.Length > 0)
            {
                lock (toAppend)
                {
                    Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, toAppend.Length, toAppend.ToString());
                    toAppend.Clear();
                }
            }
        }

        #region out
        protected override void writeImpl(string s)
        {
            if (isVisible)
                Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, s.Length, s);
            else
            {
                lock (toAppend)
                {
                    toAppend.Append(s);
                }
            }
        }


        #endregion

        #region error
        protected override void errImpl(String s)
        {
            writeImpl(s);
        }
        #endregion

        #region in
        private void onCharAdded(SCNotification sn)
        {
            if (stdIn != null)
                if (this.bufferId == (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0))
                {
                    stdIn.Write(sn.ch);
                }
        }
        #endregion

        public override void Dispose()
        {
            IODoc.BufferChanged -= onBufferChanged;
            SciNotificationEvents.CharAdded -= onCharAdded;

            if(!string.IsNullOrEmpty(path))
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, path);
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
            }
        }

        ~IODoc()
        {
            Dispose();
        }
    }

    internal class IONull : IO
    {

        public void write(char c)
        {
            
        }

        public void write(string s)
        {

        }

        public void writeLine()
        {

        }

        public void writeLine(string s)
        {

        }

        public void err(char c)
        {
            
        }

        public void err(String s)
        {
            
        }

        public void errLine()
        {
            
        }

        public void errLine(String s)
        {
            
        }

        public TextWriter stdIn { get; set; }

        public void Dispose()
        {

        }
    }
}
