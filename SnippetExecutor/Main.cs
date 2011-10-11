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

            PluginBase.SetCommand(1, "Show Console", myDockableDialog);
            idMyDlg = 1;
            PluginBase.SetCommand(2, "Run Snippet", CompileSnippet, new ShortcutKey(false, true, false, Keys.F5));


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

        internal static void myDockableDialog()
        {
            if (frmMyDlg == null || frmMyDlg.IsDisposed)
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
                _nppTbData.pszName = "SnippetExecutor Console";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
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

            myDockableDialog();
            IO console = frmMyDlg.getIOToConsole();
            try
            {

                int len = (int)Win32.SendMessage(currScint, SciMsg.SCI_GETSELTEXT, 0, 0);
                StringBuilder text;

                if (len > 1)
                {
                    //a selection exists
                    text = new StringBuilder(len);
                    Win32.SendMessage(currScint, SciMsg.SCI_GETSELTEXT, 0, text);
                }
                else
                {
                    //no selection, parse whole file
                    len = (int)Win32.SendMessage(currScint, SciMsg.SCI_GETTEXT, 0, 0);
                    text = new StringBuilder(len);
                    Win32.SendMessage(currScint, SciMsg.SCI_GETTEXT, len, text);
                }

                StringBuilder sb = new StringBuilder(Win32.MAX_PATH);
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTDIRECTORY, Win32.MAX_PATH, sb);
                
                if (text.Length == 0)
                {
                    console.writeLine("No Text");
                    return;
                }

                //create defaults
                SnippetInfo info = new SnippetInfo();
                info.language = LangType.L_TEXT;
                int langtype = (int)LangType.L_TEXT;
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTLANGTYPE, 0, out langtype);
                info.language = (LangType)langtype;
                info.stdIO = console;
                info.console = console;
                info.preprocessed = text.ToString();
                info.runCmdLine = String.Empty;
                info.compilerCmdLine = String.Empty;
                info.options = new Hashtable();

                //process overrides
                try
                {
                    PreprocessSnippet(ref info, text.ToString());
                }
                catch (Exception ex)
                {
                    console.writeLine("\r\n\r\n--- SnippetExecutor " + DateTime.Now.ToShortTimeString() + " ---");
                    console.writeLine(ex.Message);
                    console.writeLine(ex.StackTrace);
                    return;
                }

                console = info.console;

                console.writeLine("\r\n\r\n--- SnippetExecutor " + DateTime.Now.ToShortTimeString() + " ---");

                foreach (DictionaryEntry pair in info.options)
                {
                    console.writeLine(pair.Key.ToString() + ":" + pair.Value.ToString());
                }

                //get correct compiler for language
                info.compiler = getCompilerForLanguage(info.language);

                info.compiler.console = info.console;
                info.compiler.stdIO = info.stdIO;
                foreach(DictionaryEntry e in info.options)
                {
                    info.compiler.options.Add(e.Key, e.Value);
                }

                Thread th = new Thread(
                    delegate()
                    {
                        try
                        {
                            console.writeLine("-- Generating source for snippet...");
                            info.postprepared = info.compiler.PrepareSnippet(info.postprocessed);

                            if (info.options.ContainsKey("source"))
                            {
                                IO writer = console;
                                writer.writeLine();
                                if (!String.IsNullOrEmpty((string)info.options["source"]))
                                {
                                    string opt = (info.options["source"] as string);
                                    if (!String.IsNullOrEmpty(opt))
                                    {
                                        try
                                        {
                                            writer = ioForOption(opt);
                                        }
                                        catch (Exception ex)
                                        {
                                            console.writeLine("Cannot write to " + opt);
                                            console.writeLine(ex.Message);
                                            return;
                                        }
                                    }
                                }
                                writer.write(info.postprepared);
                            }

                            if (String.IsNullOrEmpty(info.postprepared)) return;

                            info.compilerCmdLine = (string)info.options["compile"];
                            console.writeLine("\r\n-- compiling source with options " + info.compilerCmdLine);
                            info.executable = info.compiler.Compile(info.postprepared, info.compilerCmdLine);

                            if (info.executable == null) return;

                            EventHandler cancelDelegate = delegate(object sender, EventArgs e)
                                {
                                    info.compiler.Cancel();
                                    console.write("-- Cancelling --");
                                };
                            frmMyDlg.CancelRunButtonClicked += cancelDelegate;

                            info.runCmdLine = (string)info.options["run"];
                            console.writeLine("-- running with options " + info.runCmdLine + " --");
                            info.compiler.execute(info.executable, info.runCmdLine);
                            console.writeLine("\r\n-- finished run --");

                            frmMyDlg.CancelRunButtonClicked -= cancelDelegate;


                        }
                        catch (Exception ex)
                        {
                            console.writeLine("Exception! " + ex.Message);
                            console.writeLine(ex.StackTrace);
                            if (ex.InnerException != null)
                            {
                                console.writeLine("inner exception: " + ex.InnerException.Message);
                                console.writeLine(ex.InnerException.StackTrace);
                            }

                        }
                        finally
                        {
                            if (info.executable != null)
                            {
                                info.compiler.cleanup(info);
                            }

                            console.Dispose();
                            info.stdIO.Dispose();
                        }
                    }
                );

                th.Start();
            }
            catch (Exception ex)
            {
                console.writeLine(ex.Message);
                console.writeLine(ex.StackTrace);
                console.write("Caused by: ");
                console.writeLine(ex.InnerException.Message);
                console.writeLine(ex.InnerException.StackTrace);
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
                        try
                        {
                            parseOption(ref info, l);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Exception parsing option >>" + l + " : " + ex.Message, ex);
                        }
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
            if (cmdOps.Length == 0) return;
			String cmd = cmdOps[0].ToLower();
            switch (cmd)
            {
                case "lang":
                    if (cmdOps.Length < 2) throw new Exception("no language specified");
                    string lang = "L_" + cmdOps[1].Trim().ToUpper();
                    bool success = Enum.TryParse<LangType>(lang, out info.language);
                    break;

                case "out":
                    if (cmdOps.Length < 2) throw new Exception("no output specified");
                    info.stdIO = ioForOption(cmdOps[1]);
                    break;
					
				case "console":
                    if (cmdOps.Length < 2) throw new Exception("no output specified");
                    info.console = ioForOption(cmdOps[1]);
                    break;
					
				default:
					//shove it in the hashtable
					string s = (string)info.options[cmd];
					if (! String.IsNullOrEmpty(s)){
                        if (option.Length > cmd.Length)
                        {
                            //multiple lines with the same option: append them to one line
                            info.options[cmd] = String.Concat(s, " ", option.Substring(cmd.Length).Trim());
                        }
					}
					else
					{
                        if (option.Length > cmd.Length)
                            info.options[cmd] = option.Substring(cmd.Length).Trim();
                        else
                            info.options[cmd] = String.Empty;
					}
					break;



            }
        }

        #endregion

        private static IO ioForOption(String option)
        {
            if (String.IsNullOrEmpty(option))
            {
                throw new Exception("No IO destination specified");
            }

            if ("console".Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                return frmMyDlg.getIOToConsole();
            }
            else if ("insert".Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                //TODO: info.console = new IOInsert();
                throw new NotImplementedException("insert");
            }
            else if ("append".Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                return new IOAppendCurrentDoc();
            }
            else if ("new".Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                // Open a new document
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
                //create an IO to that document
                return new IODoc();
            }
            else
            {
                //TODO: new IOWriteDoc();
                throw new NotImplementedException("silent file");
            }
        }
    }

    public struct SnippetInfo
    {
        public ISnippetCompiler compiler;
        public IO stdIO;
		public IO console;

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
        /// The object returned by the compiler's Compile method.  
        /// Generally a reference to the compiled, executable source code
        /// </summary>
        public Object executable;
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
                    string path = @"plugins/SnippetExecutor/templates/" + language.ToString() + ".txt";
                    string ret = loadTemplate(path);
                    if (ret == null)
                    {
                        throw new Exception("No template for " + System.IO.Path.GetFullPath(path));
                    }
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
            }catch(IOException)
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