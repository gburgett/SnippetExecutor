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

    internal abstract class IOTimedCharBuffer : IO
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
            write(toWrite);
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
            err(toWrite);
        }


        public virtual void write(char c)
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

        public abstract void write(string s);

        public virtual void writeLine()
        {
            write(Environment.NewLine);
        }

        public virtual void writeLine(string s)
        {
            write(String.Concat(s, Environment.NewLine));
        }

        public virtual void err(char c)
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

        public abstract void err(string s);

        public virtual void errLine()
        {
            err(Environment.NewLine);
        }

        public virtual void errLine(string s)
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


    internal class IOAppendCurrentDoc : IOTimedCharBuffer
    {
        private IntPtr currScint;

        public IOAppendCurrentDoc()
        {
            currScint = PluginBase.GetCurrentScintilla();
            SciNotificationEvents.CharAdded += this.onCharAdded;
        }

        public override void write(string s)
        {
            Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, s.Length, s);
        }

        public override void err(String s)
        {
            write(s);
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

    internal class IODoc : IOTimedCharBuffer
    {
        readonly IntPtr currScint;
        private int bufferId = 0;

        private StringBuilder toAppend = new StringBuilder();

        private Boolean isVisible;

        protected static event NotificationEvent BufferChanged;

        public IODoc()
        {
            this.currScint = PluginBase.GetCurrentScintilla();
            this.bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            if(bufferId0 == 0)
                bufferId0 = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETBUFFERIDFROMPOS, 0, 0);
            if (bufferId1 == 0)
                bufferId1 = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETBUFFERIDFROMPOS, 0, 1);

            isVisible = true;

            IODoc.BufferChanged += onBufferChanged;

            SciNotificationEvents.CharAdded += onCharAdded;
        }

        static IODoc()
        {
            NppNotificationEvents.BufferActivated += onBufferActivated;
            NppNotificationEvents.FileBeforeClose += onBufferActivated;

        }

        public IODoc(int bufferId)
        {
            this.currScint = PluginBase.GetCurrentScintilla();
            this.bufferId = bufferId;
            if (this.bufferId == (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0))
                isVisible = true;
            else
                isVisible = false;

            NppNotificationEvents.BufferActivated += onBufferActivated;
            NppNotificationEvents.FileBeforeClose -= onBufferActivated;
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
        public override void write(string s)
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
        public override void err(String s)
        {
            write(s);
        }
        #endregion

        #region in
        private void onCharAdded(SCNotification sn)
        {
            if(stdIn != null)
                stdIn.Write(sn.ch);
        }
        #endregion

        public override void Dispose()
        {
            IODoc.BufferChanged -= onBufferChanged;
            SciNotificationEvents.CharAdded -= onCharAdded;

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
