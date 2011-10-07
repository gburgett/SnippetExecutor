using System.Windows.Forms;
using System.Text;
using System.IO;
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(0, 0);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(284, 262);
            this.textBox1.TabIndex = 0;
            this.textBox1.AcceptsReturn = true;
            // 
            // frmMyDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.textBox1);
            this.Name = "frmMyDlg";
            this.Text = "frmMyDlg";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;

        public IO getIOToConsole()
        {
            return new IOConsole(textBox1);
        }

        /// <summary>
        /// IO to the text box which functions as the console.
        /// </summary>
        private class IOConsole : IO
        {
            private TextBox console;
            private StreamReader inputReader;
            private StreamWriter inputWriter;

            public IOConsole(TextBox console)
            {
                this.console = console;
                this.console.KeyPress += onKeyPress;
                MemoryStream ms = new MemoryStream(200);
                this.inputReader = new StreamReader(ms);
                this.inputWriter = new StreamWriter(ms);
            }

            private void onKeyPress(object sender, KeyPressEventArgs args)
            {
                inputWriter.Write(args.KeyChar);
            }


            public void write(string s)
            {
                console.Text = string.Concat(console.Text, s);
            }

            public void writeLine()
            {
                write("\r\n");
            }

            public void writeLine(string s)
            {
                console.Text = string.Concat(console.Text, s, "\r\n");
            }

            public int read()
            {
                return inputReader.Read();
            }

            public string readLine()
            {
                return inputReader.ReadLine();
            }

            public void Dispose()
            {
                inputReader.Close();
                inputWriter.Close();
            }
        }

    }
}