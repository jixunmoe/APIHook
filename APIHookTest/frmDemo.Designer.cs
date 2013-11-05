namespace APIHookTest {
	partial class frmDemo {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.btnTest = new System.Windows.Forms.Button();
			this.textMyString = new System.Windows.Forms.TextBox();
			this.btnHook = new System.Windows.Forms.Button();
			this.btnUnhook = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnTest
			// 
			this.btnTest.Location = new System.Drawing.Point(12, 38);
			this.btnTest.Name = "btnTest";
			this.btnTest.Size = new System.Drawing.Size(75, 23);
			this.btnTest.TabIndex = 0;
			this.btnTest.Text = "Call";
			this.btnTest.UseVisualStyleBackColor = true;
			this.btnTest.Click += new System.EventHandler(this.btnCall_Click);
			// 
			// textMyString
			// 
			this.textMyString.Location = new System.Drawing.Point(12, 12);
			this.textMyString.Name = "textMyString";
			this.textMyString.Size = new System.Drawing.Size(237, 20);
			this.textMyString.TabIndex = 1;
			this.textMyString.Text = "New String";
			// 
			// btnHook
			// 
			this.btnHook.Location = new System.Drawing.Point(93, 38);
			this.btnHook.Name = "btnHook";
			this.btnHook.Size = new System.Drawing.Size(75, 23);
			this.btnHook.TabIndex = 2;
			this.btnHook.Text = "Hook";
			this.btnHook.UseVisualStyleBackColor = true;
			this.btnHook.Click += new System.EventHandler(this.btnHook_Click);
			// 
			// btnUnhook
			// 
			this.btnUnhook.Location = new System.Drawing.Point(174, 38);
			this.btnUnhook.Name = "btnUnhook";
			this.btnUnhook.Size = new System.Drawing.Size(75, 23);
			this.btnUnhook.TabIndex = 3;
			this.btnUnhook.Text = "Unhook";
			this.btnUnhook.UseVisualStyleBackColor = true;
			this.btnUnhook.Click += new System.EventHandler(this.btnUnhook_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(263, 72);
			this.Controls.Add(this.btnUnhook);
			this.Controls.Add(this.btnHook);
			this.Controls.Add(this.textMyString);
			this.Controls.Add(this.btnTest);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.Text = "Example: MessageBoxW";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnTest;
		private System.Windows.Forms.TextBox textMyString;
		private System.Windows.Forms.Button btnHook;
		private System.Windows.Forms.Button btnUnhook;
	}
}

