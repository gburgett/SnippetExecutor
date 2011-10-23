using System.Windows.Forms;
using System.Text;
using System.IO;
using System;
using System.Text.RegularExpressions;
using NppPluginNET;
namespace SnippetExecutor
{
    partial class frmMyDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            //cancel an existing run first
            if (CancelRunButtonClicked != null)
            {
                CancelRunButtonClicked(this, null);
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMyDlg));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripFilenameLabel = new System.Windows.Forms.ToolStripLabel();
            this.toolStripCancelButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripClearButton = new System.Windows.Forms.ToolStripButton();
            this.textBox1 = new System.Windows.Forms.RichTextBox();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripCancelButton,
            this.toolStripClearButton,
            this.toolStripDropDownButton1,
            this.toolStripFilenameLabel});

            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(632, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripFilenameLabel
            // 
            this.toolStripFilenameLabel.Name = "toolStripFilenameLabel";
            this.toolStripFilenameLabel.Size = new System.Drawing.Size(0, 22);
            // 
            // toolStripCancelButton
            // 
            this.toolStripCancelButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripCancelButton.Image = global::SnippetExecutor.Properties.Resources.stop;
            this.toolStripCancelButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripCancelButton.Name = "toolStripCancelButton";
            this.toolStripCancelButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripCancelButton.Text = "Cancel";
            // 
            // toolStripClearButton
            // 
            this.toolStripClearButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripClearButton.Image = global::SnippetExecutor.Properties.Resources.clear;
            this.toolStripClearButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripClearButton.Name = "toolStripClearButton";
            this.toolStripClearButton.Size = new System.Drawing.Size(23, 22);
            this.toolStripClearButton.Text = "Clear";
            this.toolStripClearButton.Click += this.toolStripClearButton_Click;
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(0, 25);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(632, 243);
            this.textBox1.TabIndex = 1;
            this.textBox1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.textBox1_PreviewKeyDown);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.loadToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(29, 22);
            this.toolStripDropDownButton1.Text = "toolStripDropDownButton1";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveToolStripMenuItem.Text = "save";
            this.saveToolStripMenuItem.Click += this.doSave;
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.loadToolStripMenuItem.Text = "load";
            this.loadToolStripMenuItem.Click += this.doLoad;
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newToolStripMenuItem.Text = "new";
            this.newToolStripMenuItem.Click += this.doNew;
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.clearToolStripMenuItem.Text = "clear";
            this.clearToolStripMenuItem.Click += this.toolStripClearButton_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveAsToolStripMenuItem.Text = "save as";
            this.saveAsToolStripMenuItem.Click += this.launchFileSaveDialog;
            // 
            // frmMyDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 268);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "frmMyDlg";
            this.Text = "frmMyDlg";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.S:
                case Keys.A:
                    if(e.Control)
                    {
                        e.IsInputKey = true;
                    }
                    break;
            }
        }

        void textBox1_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    if (e.Control)
                    {
                        textBox1.SelectAll();
                        e.Handled = true;
                    }
                    break;

                case Keys.S:
                    if(e.Control)
                    {
                        doSave(sender, e);
                        e.Handled = true;
                    }
                    break;
            }
        }

        public string consoleFileName
        {
            get { return toolStripFilenameLabel.Text; }
            set { toolStripFilenameLabel.Text = value; }
        }

        private void doSave(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(consoleFileName) || !File.Exists(consoleFileName))
            {
                launchFileSaveDialog(sender, e);
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(File.Open(consoleFileName, FileMode.Create)))
                {
                    writer.Write(textBox1.Text);
                    writer.Flush();
                }
            }
        }

        private void launchFileSaveDialog(object sender, EventArgs e)
        {

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            StringBuilder sb = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTDIRECTORY, Win32.MAX_PATH, sb);

            if (sb.Length > 0)
            {
                saveFileDialog1.InitialDirectory = sb.ToString();
            }
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(File.Open(saveFileDialog1.FileName, FileMode.Create)))
                    {
                        writer.Write(textBox1.Text);
                        writer.Flush();
                    }
                    consoleFileName = saveFileDialog1.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not save file. " + ex.Message);
                }
            }


        }

        private void doNew(object sender, EventArgs e)
        {
            this.textBox1.Clear();
            this.consoleFileName = string.Empty;
        }

        private void doLoad(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            StringBuilder sb = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTDIRECTORY, Win32.MAX_PATH, sb);

            if (sb.Length > 0)
            {
                openFileDialog1.InitialDirectory = sb.ToString();
            }
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (StreamReader reader = new StreamReader(myStream))
                        {
                            this.textBox1.Text = reader.ReadToEnd();
                        }
                        this.consoleFileName = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. " + ex.Message);
                }
            }
        }

        #endregion


        public IO getIOToConsole()
        {
            return new IOConsole(textBox1);
        }

        public event EventHandler CancelRunButtonClicked;
            
        void  toolStripCancelButton_Click(object sender, EventArgs e)
        {
 	        if(CancelRunButtonClicked != null)
 	        {
 	            CancelRunButtonClicked(sender, e);
 	        }
        }

        void toolStripClearButton_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private ToolStrip toolStrip1;
        private ToolStripButton toolStripCancelButton;
        private RichTextBox textBox1;
        private ToolStripButton toolStripClearButton;
        private ToolStripLabel toolStripFilenameLabel;
        private ToolStripDropDownButton toolStripDropDownButton1;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem loadToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;

        /// <summary>
        /// IO to the text box which functions as the console.
        /// </summary>
        private class IOConsole : IOTimedBuffer
        {
            private RichTextBox console;
            
            public IOConsole(RichTextBox console)
            {
                this.console = console;
                this.console.KeyPress += onKeyPress;
            }

            public override void write(string s)
            {
                //System.Windows.Forms.MessageBox.Show("write: " + s);
                base.write(s);
            }
            public override void err(string s)
            {
                //System.Windows.Forms.MessageBox.Show("err: " + s);
                base.err(s);
            }

            protected override void writeImpl(string s)
            {
                console.Invoke(new Action(delegate
                {
                    console.AppendText(s);
                    //move to end
                    console.SelectionStart = console.Text.Length;
                    console.Refresh();
                }));
            }

            protected override void errImpl(string s)
            {
                //System.Windows.Forms.MessageBox.Show("ErrorImpl: " + s);
                console.Invoke(new Action(delegate
                {
                    var oldcolor = console.ForeColor;
                    int selectionStart = console.TextLength;
                    console.AppendText(s);
                    console.Select(selectionStart, s.Length);
                    console.SelectionColor = System.Drawing.Color.Red;

                    //move to end
                    console.DeselectAll();
                    console.SelectionStart = console.TextLength;
                    
                    console.Refresh();
                }));
            }
            
            public override void Dispose()
            {
                this.console.KeyPress -= this.onKeyPress;
            }

            private void onKeyPress(object sender, KeyPressEventArgs args)
            {
                if(stdIn != null)
                {
                    if (args.KeyChar == (char)Keys.Enter)
                    {
                        stdIn.Write(Environment.NewLine);
                    }
                    else
                    {
                        stdIn.Write(args.KeyChar);
                    }
                }
                    

            }

            ~IOConsole()
            {
                Dispose();
            }

        }

    }
}