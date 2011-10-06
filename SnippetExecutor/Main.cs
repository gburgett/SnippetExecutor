using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using SnippetExecutor.Compilers;

namespace SnippetExecutor
{
    internal class Main
    {
        #region " Fields "

        internal const string PluginName = "SnippetExecutor";
        private static string iniFilePath = null;
        private static bool someSetting = false;

        private static frmMyDlg frmMyDlg = null;
        private static int idMyDlg = -1;
        private static Bitmap tbBmp = Properties.Resources.star;
        private static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        private static Icon tbIcon = null;

        #endregion

        #region " StartUp/CleanUp "

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH,
                              sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);

            PluginBase.SetCommand(1, "MyDockableDialog", myDockableDialog);
            idMyDlg = 1;
            PluginBase.SetCommand(2, "CompileSnippet", CompileSnippet, new ShortcutKey(false, true, false, Keys.F5));


        }

        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON,
                              PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        {
            Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);

        }

        #endregion

        #region " notifications "

        #endregion

        #region " Menu functions "

        internal static void myMenuFunction()
        {
            MessageBox.Show("Hello N++!");
        }

        internal static void myDockableDialog()
        {
            if (frmMyDlg == null)
            {
                frmMyDlg = new frmMyDlg();

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "My dockable dialog";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint) tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }
        }

        internal static void RunSnippet()
        {

        }


        internal static void CompileSnippet()
        {
            
            IntPtr currScint = PluginBase.GetCurrentScintilla();

            IO mIO = new IODoc();
            try
            {


                mIO.writeLine("\r\n\r\n--- SnippetExecutor " + DateTime.Now.ToShortTimeString() + " ---");

                int len = (int)Win32.SendMessage(currScint, SciMsg.SCI_GETSELTEXT, 0, 0);
                StringBuilder text;
                mIO.writeLine("length: " + len);

                if (len > 1)
                {
                    //a selection exists
                    text = new StringBuilder(len);
                    Win32.SendMessage(currScint, SciMsg.SCI_GETSELTEXT, 0, text);
                }
                else
                {
                    //no selection, parse whole file
                    len = (int)Win32.SendMessage(currScint, SciMsg.SCI_GETTEXT, 0, 0) + 1;
                    text = new StringBuilder(len);
                    mIO.writeLine("doclength: " + len);
                    Win32.SendMessage(currScint, SciMsg.SCI_GETTEXT, len, text);
                }

                if (text.Length == 0)
                {
                    mIO.writeLine("No Text");
                    return;
                }

                mIO.writeLine("PreProcessing");

                //create defaults
                SnippetInfo info = new SnippetInfo();
                info.language = LangType.L_TEXT;
                int langtype = (int)LangType.L_TEXT;
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTLANGTYPE, 0, out langtype);
                info.language = (LangType) langtype;
                info.currIO = mIO;
                info.preprocessed = text.ToString();
                info.runCmdLine = String.Empty;
                info.compilerCmdLine = String.Empty;
                
                //process overrides
                PreprocessSnippet(ref info, text.ToString());

                //get correct compiler for language
                info.compiler = getCompilerForLanguage(info.language);

                mIO.writeLine("Compiling");
                mIO.writeLine("lang: " + info.language);
                mIO.writeLine("postProcessed: " + info.postprocessed);
                
                info.compiler.io = info.currIO;

                Thread th = new Thread(
                    delegate()
                    {
                        try
                        {
                            info.postprepared = info.compiler.PrepareSnippet(info.postprocessed);

                            mIO.writeLine("postPrepared:");
                            mIO.write(info.postprepared);

                            if (String.IsNullOrEmpty(info.postprepared)) return;

                            info.compiled = info.compiler.Compile(info.postprepared, info.compilerCmdLine);

                            if (info.compiled == null) return;

                            info.compiler.execute(info.compiled, info.runCmdLine);

                        }
                        catch (Exception ex)
                        {
                            mIO.writeLine("Exception! " + ex.Message);
                            mIO.writeLine(ex.StackTrace);
                            if (ex.InnerException != null)
                            {
                                mIO.writeLine("inner exception: " + ex.InnerException.Message);
                                mIO.writeLine(ex.InnerException.StackTrace);
                            }

                        }
                        finally
                        {
                            if (info.compiled != null)
                            {
                                info.compiler.cleanup(info);
                            }
                        }
                    }
                );

                th.Start();
            }
            catch (Exception ex)
            {
                mIO.writeLine(ex.Message);
            }
        }

        private static ISnippetCompiler getCompilerForLanguage(LangType langType)
        {
            switch (langType)
            {
                case LangType.L_CS:
                    return new CSharpCompiler();

                case LangType.L_VB:
                    return new VBCompiler();

                default:
                    throw new Exception("No compiler for language " + langType.ToString());
            }
        }

        static void PreprocessSnippet(ref SnippetInfo info, String snippetText)
        {
            string[] lines = snippetText.Split(new String[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            int snippetStart = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i].Trim();
                if (!String.IsNullOrEmpty(l))
                {
                    if (l.StartsWith(">>"))
                    {
                        l = l.Substring(2);
                        parseOption(ref info, l);
                    }
                    else
                    {
                        snippetStart = i;
                        break;
                    }
                }
            }

            int snippetEnd = lines.Length;
            for (int i = snippetStart; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("--- SnippetExecutor"))
                {
                    snippetEnd = i;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = snippetStart; i < snippetEnd; i++)
            {
                sb.AppendLine(lines[i]);
            }

            info.postprocessed = sb.ToString();

            return;
        }

        static void parseOption(ref SnippetInfo info, String option)
        {
            option.Trim();
            string[] cmdOps = option.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            switch (cmdOps[0].ToLower())
            {
                case "lang":
                    string lang = "L_" + cmdOps[1].Trim().ToUpper();
                    bool success = Enum.TryParse<LangType>(lang, out info.language);
                    break;

                case "run":
                    info.runCmdLine = option.Substring(option.IndexOf(cmdOps[1]));
                    break;

                case "compile":
                    info.compilerCmdLine = option.Substring(option.IndexOf(cmdOps[1]));
                    break;

                case "out":
                    if ("console".Equals(cmdOps[1], StringComparison.OrdinalIgnoreCase))
                    {
                        //TODO: info.currIO = new IOConsole();
                    }
                    else if ("insert".Equals(cmdOps[1], StringComparison.OrdinalIgnoreCase))
                    {
                        //TODO: info.currIO = new IOInsert();
                    }
                    else if ("append".Equals(cmdOps[1], StringComparison.OrdinalIgnoreCase))
                    {
                        info.currIO = new IOAppendCurrentDoc();
                    }
                    else if ("new".Equals(cmdOps[1], StringComparison.OrdinalIgnoreCase))
                    {
                        // Open a new document
                        Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
                        //create an IO to that document
                        info.currIO = new IODoc();
                    }
                    else
                    {
                        //TODO: create new file, open it, info.currIO = new IOAppendDoc();
                        //and mark to save after done
                    }
                    break;



            }
        }

        static string PrepareSnippet(ISnippetCompiler compiler, String snippetText)
        {
            return compiler.PrepareSnippet(snippetText);

        }

        static Object Compile(ISnippetCompiler compiler, String PreparedText, String compilerOptions)
        {   
            return compiler.Compile(PreparedText, compilerOptions);
        }

        #endregion
    }

    public struct SnippetInfo
    {
        public ISnippetCompiler compiler;
        public IO currIO;

        public Hashtable options;

        public LangType language;

        /// <summary>
        /// The compiler command line options
        /// </summary>
        public String compilerCmdLine;
        /// <summary>
        /// The run-time command line options
        /// </summary>
        public String runCmdLine;

        /// <summary>
        /// The initial input text
        /// </summary>
        public String preprocessed;
        /// <summary>
        /// The snippet text after parsing the options
        /// </summary>
        public String postprocessed;
        /// <summary>
        /// The valid source code prepared for the snippet
        /// </summary>
        public String postprepared;
        /// <summary>
        /// A reference to the compiled, executable source code
        /// </summary>
        public Object compiled;
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
            if (this.bufferId == (int) Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0))
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
            if(isMyBuffer)
                Win32.SendMessage(currScint, SciMsg.SCI_APPENDTEXT, s.Length, s);
            else
            {
                lock(toAppend)
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

    public static class TemplateLoader
    {
        private static Hashtable templates = new Hashtable();

        public static string getTemplate(LangType language)
        {
            lock (templates.SyncRoot)
            {

                if (templates[language] == null)
                {
                    string ret = loadTemplate(@"plugins/SnippetExecutor/templates/" + language.ToString() + ".txt");
                    templates[language] = ret;
                    return ret;
                }
                else
                {
                    return (string) templates[language];
                }
            }
        }

        private static string loadTemplate(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(path)))
                {
                    return reader.ReadToEnd();
                }
            }catch(IOException ex)
            {
                return null;
            }
        }

        public static void clearTemplates()
        {
            lock(templates.SyncRoot)
            {
                templates.Clear();
            }
        }

        private const string tagStart = "${SnippetExecutor.";
        private static readonly Regex tagRegex = new Regex(@"\${SnippetExecutor.(?<tagName>[a-zA-Z0-9]+)}");

        public static string insertSnippet(string tagName, string snippet, string template)
        {
            string toReplace = String.Concat(tagStart, tagName, "}");

            template = template.Replace(toReplace, snippet);
            
            return template;
        }

        public static string removeOtherSnippets(string template)
        {
            template = tagRegex.Replace(template, new MatchEvaluator(m => String.Empty));

            return template;
        }

    }
}