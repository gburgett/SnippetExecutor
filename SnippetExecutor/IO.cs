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

    /// <summary>
    /// This abstract class provides a string buffer to which writes are initially written.  After a short timeout
    /// the buffer is flushed to writeImpl and errImpl.
    /// </summary>
    internal abstract class IOTimedBuffer : IO
    {
        const int flushIntervalMS = 50;

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
                    tWrite = new Timer(timeoutWrite, writeBuffer, flushIntervalMS, flushIntervalMS);
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
                    tWrite = new Timer(timeoutWrite, writeBuffer, flushIntervalMS, flushIntervalMS);
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
                    tErr = new Timer(timeoutErr, errBuffer, flushIntervalMS, flushIntervalMS);
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
                    tErr = new Timer(timeoutErr, errBuffer, flushIntervalMS, flushIntervalMS);
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

    /// <summary>
    /// This IO is very simple, it always appends the current doc.  If you switch docs this IO will continue
    /// writing to the new doc.
    /// </summary>
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

    /// <summary>
    /// This IO writes to a currently open np++ buffer.  It has factory methods for choosing which buffer to write.
    /// </summary>
    internal class IODoc : IOTimedBuffer
    {
        protected IntPtr currScint = PluginBase.GetCurrentScintilla();
        protected int bufferId;

        protected StringBuilder toAppend = new StringBuilder();

        protected bool isVisible { 
            get 
            {
                return this.bufferId == IODoc.currentBuffer;
            }
        }

        public readonly string path;

        static IODoc()
        {
            currentBuffer = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
        }

        protected IODoc() : this(0, string.Empty)
        {
            
        }

        protected IODoc(int bufferId) : this(bufferId, string.Empty)
        {
        }

        protected IODoc(int bufferId, string path)
        {
            this.bufferId = bufferId;

            this.path = path;

            NppNotificationEvents.BufferActivated += onBufferChanged;
            SciNotificationEvents.CharAdded += onCharAdded;

        }

        protected static System.Collections.Hashtable docs = new System.Collections.Hashtable();
        protected int refs = 0;
        
        #region factories
        public static IODoc ForCurrentBuffer()
        {
            return ForCurrentBuffer(string.Empty);   
        }

        public static IODoc ForCurrentBuffer(string path)
        {
            
            int bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            return ForBuffer(bufferId, path);
        }

        public static IODoc ForBuffer(int bufferId)
        {
            return ForBuffer(bufferId, string.Empty);
        }

        public static IODoc ForBuffer(int bufferId, string path)
        {
            IODoc ret = null;
            lock (IODoc.docs)
            {
                if (docs.ContainsKey(bufferId))
                {
                    ret = (IODoc)docs[bufferId];
                }

                if (ret == null)
                {
                    ret = new IODoc(bufferId, path);
                }

                ret.refs++;
                return ret;
            }

            
        }
        #endregion

        private static int currentBuffer = 0;

        private void onBufferChanged(SCNotification scn)
        {
            currentBuffer = (int)scn.nmhdr.idFrom;

            Main.debug.write("currB: " + currentBuffer.ToString());
            Main.debug.writeLine(" buffer: " + bufferId.ToString());

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
            lock (IODoc.docs)
            {
                refs--;
                if (refs == 0)
                {
                    IODoc.docs.Remove(bufferId);
                }
            }

            //out of lock, if we've detached all references continue with dispose
            NppNotificationEvents.BufferActivated -= onBufferChanged;
            SciNotificationEvents.CharAdded -= onCharAdded;

            if (!string.IsNullOrEmpty(path))
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

    /// <summary>
    /// This IO writes to a new file, lazily opening it in a np++ window as though the user selected
    /// file -> new.
    /// </summary>
    internal class IONewDoc : IODoc
    {
        private bool isOpen = false;
        protected IONewDoc(int bufferId, string path) : base(bufferId, path) { }


        public static IONewDoc NewDocFactory()
        {
            return IONewDoc.NewDocFactory(string.Empty);
        }

        public static IONewDoc NewDocFactory(string path)
        {
            IONewDoc ret = null;
            lock (IODoc.docs)
            {
                if (docs.ContainsKey("new"))
                {
                    ret = (IONewDoc)docs["new"];
                }

                if (ret == null || !ret.isOpen)
                {
                    ret = new IONewDoc(-1, path);
                    ret.isOpen = false;
                    docs["new"] = ret;
                }

                ret.refs++;
                return ret;
            }
        }

        private void lazyOpen()
        {
            if (!isOpen)
            {
                lock (IODoc.docs)
                {
                    if (!isOpen)
                    {   
                        // Open a new document
                        Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
                        //get the buffer ID
                        int bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
                        
                        //set this object's bufferId
                        this.bufferId = bufferId;
                        //put this object in the new spot, so it's both here and in "new".  That way IODoc factories
                        //will also get this.
                        IODoc.docs[bufferId] = this;

                        isOpen = true;
                    }
                }
            }
        }

        protected override void writeImpl(string s)
        {
            lazyOpen();  
            
            base.writeImpl(s);
        }

        protected override void errImpl(string s)
        {
            lazyOpen();

            base.errImpl(s);
        }


        public override void Dispose()
        {
            base.Dispose();
            isOpen = false;
        }
    }

    /// <summary>
    /// This IO writes to a named file, lazily creating it and opening it in a np++ window.
    /// </summary>
    internal class IOFileDoc : IODoc
    {
        private bool isOpen = false;
        protected IOFileDoc(int bufferId, string path) : base(bufferId, path) { }

        /// <summary>
        /// Returns an IO to the file specified in path.
        /// The file will be opened in the view when it is first written to.  If the file doesnt exist it will be created.
        /// The file will be saved when all references to this IO have been disposed.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IOFileDoc FileDocFactory(string path)
        {
            IOFileDoc ret = null;
            string key = string.Concat("File:", path);
            lock (IODoc.docs)
            {
                if (docs.ContainsKey(key))
                {
                    ret = (IOFileDoc)docs[key];
                }

                if (ret == null || !ret.isOpen)
                {
                    ret = new IOFileDoc(-1, path);
                    ret.isOpen = false;
                    docs[key] = ret;
                }

                ret.refs++;
                return ret;
            }
        }

        private void lazyOpen()
        {
            if (!isOpen)
            {
                lock (IODoc.docs)
                {
                    if (!isOpen)
                    {
                        string filename = this.path;
                        bool created = false;
                        if (!File.Exists(filename))
                        {
                            using (File.Create(filename))
                            {
                                created = true;
                            }
                        }
                        //make a new doc and open it
                        bool success = Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, (int)0, filename).ToInt32() == 1;
                        if (!success)
                        {
                            if (created) File.Delete(filename);
                            throw new Exception("could not open file: " + Path.GetFullPath(filename));
                        }
                        //get the buffer ID
                        int bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
                        Main.debug.writeLine("new file buffer: " + bufferId);
                        //set this object's bufferId
                        this.bufferId = bufferId;
                        //put this object in the new spot, so it's both here and in "new".  That way IODoc factories
                        //will also get this.
                        IODoc.docs[bufferId] = this;

                        isOpen = true;
                        
                    }
                }
            }
        }

        protected override void writeImpl(string s)
        {
            lazyOpen();

            base.writeImpl(s);
        }

        protected override void errImpl(string s)
        {
            lazyOpen();

            base.errImpl(s);
        }


        public override void Dispose()
        {
            base.Dispose();
            isOpen = false;
        }

    }

    /// <summary>
    /// This IO doesn't print anything anywhere. It ignores all writes and has no stdIn.
    /// </summary>
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

        public TextWriter stdIn 
        { 
            get { return TextWriter.Null; }  
            set {} 
        }

        public void Dispose()
        {

        }
    }
}
