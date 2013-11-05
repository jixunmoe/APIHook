using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APIHookTest {
	public partial class frmDemo : Form {
		public frmDemo() {
			InitializeComponent();
		}

		// APIHook Class init.
		// 初始化
		public static APIHook.APIHook MyMsgBox = new APIHook.APIHook();

		// Define Callback
		// 定义回调
		public delegate int MyMsgBoxCallbackPtr(int hWnd, int lpText, int lpCaption, int uType);

		// 回调, 全部都是指针…
		public static int MyMsgBoxCallback(int hWnd, int lpText, int lpCaption, int uType) {
			Debug.WriteLine("MyMsgBoxCallback");

			MyMsgBoxCallbackPtr MyMsg =
				(MyMsgBoxCallbackPtr)Marshal.GetDelegateForFunctionPointer
				/* 
				 *   MyMsgBox.oldCodeEntryAddr -> 备份的 API 入口字节地址
				 */
					(new IntPtr(MyMsgBox.oldCodeEntryAddr), typeof(MyMsgBoxCallbackPtr));

			return MyMsg(
				hWnd, 
				// 文本需要取地址… 上面就是这么定义的 233
				APIHook.addr.get(MyMsgBox.fetchString(lpText) + "\n\n" + Program.Form.textMyString.Text),
				lpCaption,
				uType
			);
		}

		// !! 初始化 APIHook 
		private void Form1_Load(object sender, EventArgs e) {
			MyMsgBox.installHook("user32.dll", "MessageBoxW", new MyMsgBoxCallbackPtr(MyMsgBoxCallback));
			textMyString.Text = "Origional entry point: 0x" + MyMsgBox.oldCodeEntryAddr.ToString("X");

			btnTest.PerformClick();
		}

		private void btnCall_Click(object sender, EventArgs e) {
			MessageBox.Show("MyText", "MyTitle", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);
			// MessageBoxW(0, "String1", "String2", 0);
		}

		private void btnHook_Click(object sender, EventArgs e) {
			MyMsgBox.resume();
		}

		private void btnUnhook_Click(object sender, EventArgs e) {
			MyMsgBox.pause();
		}

	}
}
