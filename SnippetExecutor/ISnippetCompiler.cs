﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using NppPluginNET;
using System.Threading;
using System.IO;

namespace SnippetExecutor
{
    public interface ISnippetCompiler
    {
        IO stdIO
        {
            set;
        }
		
		IO console
		{
			set;
		}

        String[] UnderstoodArguments
        {
            get;
        }

        System.Collections.Hashtable options { get; }

        string workingDirectory
        {
            set;
        }

        /// <summary>
        /// Processes snippet text and returns a string which should
        /// be directly compileable, i.e. if written to a file then executing
        /// the compiler on that file should succeed.
        /// </summary>
        /// <param name="snippetText">the snippet to prepare</param>
        /// <returns>valid source code as a string</returns>
        string PrepareSnippet(String snippetText);

        /// <summary>
        /// Compiles valid source code into an executable.  A reference to the executable
        /// should be returned for later handing to execute.
        /// </summary>
        /// <param name="text">Valid source code, generated by ProcessSnippet</param>
        /// <param name="options">compiler command line options</param>
        /// <returns>an object handle referencing the compiled code</returns>
        Object Compile(String text, string options);

        /// <summary>
        /// Executes the executable created by Compile, hooking stdin and stdout to
        /// the IO object injected in the property.
        /// </summary>
        /// <param name="args">command line arguments for the execution</param>
        /// <returns>true if execution finished without error</returns>
        bool execute(Object executable, string args);

        bool cleanup(SnippetInfo info);

        void Cancel();
    }

    public abstract class AbstractSnippetCompiler : ISnippetCompiler
    {
        public IO stdIO { 
            set;
            protected get;
        }
		
		public IO console{
			set;
			protected get;
		}

        public abstract String[] UnderstoodArguments
        {
            get;
        }

        protected abstract LangType lang
        {
            get;
        }

        public virtual string workingDirectory
        {
            get;
            set;
        }

        public Hashtable options
        {
            get { return _options; }
        }
        private Hashtable _options = new Hashtable();

        public virtual string PrepareSnippet(string snippetText)
        {
            string toCompile = TemplateLoader.getTemplate(this.lang);
            

            toCompile = TemplateLoader.insertSnippet("snippet", snippetText, toCompile);
            toCompile = TemplateLoader.removeOtherSnippets(toCompile);
            return toCompile;
        }

        public abstract object Compile(string text, string options);

        protected abstract string cmdToExecute(object executable, string args);

        protected abstract string getArgs(object executable, string args);

        protected abstract void preStart(object executable, string args);

        private Process processObj;

        public virtual bool execute(object executable, string args)
        {
            if (executable == null) throw new Exception("compile first");

            processObj = new Process();
            
            processObj.StartInfo.FileName = this.cmdToExecute(executable, args);
            processObj.StartInfo.Arguments = this.getArgs(executable, args);
            processObj.StartInfo.UseShellExecute = false;
            processObj.StartInfo.CreateNoWindow = true;
            processObj.StartInfo.RedirectStandardOutput = true;
            processObj.StartInfo.RedirectStandardError = true;
            processObj.StartInfo.RedirectStandardInput = true;
            if (!string.IsNullOrEmpty(this.workingDirectory))
            {
                console.writeLine("working dir: " + this.workingDirectory);
                processObj.StartInfo.WorkingDirectory = this.workingDirectory;
            }

            this.preStart(executable, args);

            bool started = processObj.Start();
            if(!started) return false;

            stdIO.stdIn = processObj.StandardInput;
            Thread thOut = new Thread(a =>
                {
                    try
                    {
                        char[] buffer = new char[1];
                        while (processObj.StandardOutput.Read(buffer, 0, buffer.Length) > 0)
                        {

                            stdIO.write(buffer[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        console.writeLine("Error! " + ex.Message);
                        console.writeLine(ex.StackTrace);
                    }
                });
            thOut.Start();
            Thread thIn = new Thread(a =>
                {
                    try
                    {
                        char[] buffer = new char[1];
                        while (processObj.StandardError.Read(buffer, 0, buffer.Length) > 0)
                        {

                            stdIO.err(buffer[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        console.writeLine("Error! " + ex.Message);
                        console.writeLine(ex.StackTrace);
                    }
                });
            thIn.Start();

            //wait for the process to finish
            processObj.WaitForExit();
            //wait for the IO redirect threads to finish
            thOut.Join();
            thIn.Join();

            processObj.Dispose();

            return true;
        }

        public virtual void Cancel()
        {
            if(processObj != null)
            {
                processObj.Kill();
            }
        }

        public abstract bool cleanup(SnippetInfo info);

    }

}
