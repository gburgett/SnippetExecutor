using System.Windows.Forms;
using System.Text;
using System.IO;
using System;
using System.Text.RegularExpressions;
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
            if (CancelRunButtonClicked != null)
            {
                CancelRunButtonClicked(toolStripButton1, null);
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
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.textBox1 = new System.Windows.Forms.RichTextBox();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(284, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(0, 29);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(284, 235);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "";
            this.textBox1.Multiline = true;
            // 
            // frmMyDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "frmMyDlg";
            this.Text = "frmMyDlg";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion


        public IO getIOToConsole()
        {
            return new IOConsole(textBox1);
        }

        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;

        public event EventHandler CancelRunButtonClicked;
            
        void  toolStripButton1_Click(object sender, EventArgs e)
        {
 	        if(CancelRunButtonClicked != null)
 	        {
 	            CancelRunButtonClicked(toolStripButton1, e);
 	        }
        }

        private RichTextBox textBox1;

        /// <summary>
        /// IO to the text box which functions as the console.
        /// </summary>
        private class IOConsole : IOTimedCharBuffer
        {
            private RichTextBox console;
            
            public IOConsole(RichTextBox console)
            {
                this.console = console;
                this.console.KeyPress += onKeyPress;
            }

            public override void write(string s)
            {
                console.Invoke(new Action(delegate
                {
                    console.AppendText(s);
                    //move to end
                    console.SelectionStart = console.Text.Length;
                    console.Refresh();
                }));
            }

            public override void err(string s)
            {
                console.Invoke(new Action(delegate
                {
                    var oldcolor = console.ForeColor;
                    console.ForeColor = System.Drawing.Color.Red;
                    console.AppendText(s);
                    //move to end
                    console.SelectionStart = console.Text.Length;
                    console.ForeColor = oldcolor;

                    console.Refresh();
                }));
                
                write(s);
            }
            
            public override void Dispose()
            {
                this.console.KeyPress -= this.onKeyPress;
            }

            private void onKeyPress(object sender, KeyPressEventArgs args)
            {
                if(stdIn != null)
                {
                    stdIn.Write(args.KeyChar);
                }
                    

            }

            ~IOConsole()
            {
                Dispose();
            }

        }

    }
}