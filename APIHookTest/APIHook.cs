using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

/*
 * Reference / 参考来源: 
 * 1. 网络上关于 APIHook 的易语言源码
 * 2. http://www.ownedcore.com/forums/world-of-warcraft/world-of-warcraft-bots-programs/wow-memory-editing/424055-c-apihook-class.html
 * 3. http://www.mpgh.net/forum/250-c-programming/298510-c-writeprocessmemory-readprocessmemory.html
 * 4. http://stackoverflow.com/a/1318948
 * 5. http://stackoverflow.com/a/5056351
 * 6. http://stackoverflow.com/a/4015632
 * 7. http://stackoverflow.com/q/137544
 * 8. http://stackoverflow.com/a/2049606
 * 9. http://stackoverflow.com/a/311179
 */

namespace APIHook {
	/// <summary>
	/// Hook 当前进程的某个 API。
	/// </summary>
	public class APIHook : IDisposable {
		private memOp mem = new memOp();

		// 公开成员变量
		public bool unhookAtDispose = true;
		public bool isHooked { get; private set; }
		// 内部成员变量

		/// <summary>
		/// [函数地址] API 入口地址
		/// </summary>
		private int hFunc;
		/// <summary>
		/// 原保护值
		/// </summary>
		private uint oldProtection;

		/// <summary>
		/// [原字节] 保存API入口前5个字节
		/// </summary>
		private byte[] oldBytes;
		/// <summary>
		/// [原地址], AddressOf oldBytecode
		/// </summary>
		public int oldCodeEntryAddr{get; private set;}
		/// <summary>
		/// 用于写入的字节集
		/// </summary>
		private byte[] newBytes;

		/// <summary>
		/// _销毁
		/// </summary>
		public void Dispose() {
			if (unhookAtDispose)
				unhook();
		}
		public static string ByteArrayToString(byte[] ba) {
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("0x{0:x2}, ", b);
			return hex.ToString();
		}

		private byte[] myInt2Bytes(int input) {
			//Debug.WriteLine("Calc byte code for: " + input.ToString());
			byte[] intBytes = BitConverter.GetBytes(input);

			// Not needed?
			//if (BitConverter.IsLittleEndian)
			//	Array.Reverse(intBytes);


			//Debug.WriteLine(ByteArrayToString(intBytes));
			return intBytes;
		}

		/// <summary>
		/// 安装 Hook
		/// </summary>
		/// <param name="sLibPathOrName">DLL 文件名或完整路径</param>
		/// <param name="funcName">API 名称</param>
		/// <param name="callback">自己的函数</param>
		/// <returns></returns>
		public bool installHook(string sLibPathOrName, string funcName, Delegate callback) {
			// Print warnning. Will not compilied to Execuble if is release.
			Debug.WriteLine("Run as 'Release', or this will fail.");

			if (hFunc != 0)
				return false;

			int hModule = GetModuleHandle(sLibPathOrName);
			if (hModule == 0) {
				hModule = LoadLibrary(sLibPathOrName);

				// If still fail... Well, throw an exception.
				if (hModule == 0)
					throw new Exception("Unable to find or load target library.");
			}
			hFunc = GetProcAddress(hModule, funcName);
			if (hFunc == 0)
				throw new Exception("Function not found in the DLL or load failed.");

			if (!VirtualProtect(hFunc, (uint)0x05, (uint)Protection.PAGE_EXECUTE_READWRITE, out this.oldProtection))
				throw new Exception("Unable to overide protection settings.");

			/*
			 * oldBytes
			 *   - First five bytes will be same as the origional API.
			 *   - Next five bytes, jmp to origional API addr + 5.
			 */
			this.oldBytes = new byte[10];
			Array.Copy(
				mem.ReadMem(hFunc, 0x5),
				0, oldBytes, 0, 5
			);

			int iCallback = Marshal.GetFunctionPointerForDelegate(callback).ToInt32();
			// Debug.WriteLine("hFunc =>   " + hFunc.ToString("X"));
			// Debug.WriteLine("newFunc => " + iCallback.ToString("X"));

			// 原地址 ＝ lstrcpyn_字节集 (原字节, 原字节, 0)
			this.oldCodeEntryAddr = addr.get(oldBytes);
			// CURRENT_RVA: jmp (DESTINATION_RVA - CURRENT_RVA - 5 [sizeof(E9 xx xx xx xx)])
			// 新函数地址 － 函数地址 － 5
			oldBytes[5] = 0xE9; // jmp xxxxxx
			Array.Copy(
				myInt2Bytes(hFunc - oldCodeEntryAddr - 5),
				0, oldBytes, 6, 4
			);

			/*
			 * newBytes
			 *   - Jump to custom callback
			 */

			// 新字节 ＝ { 233 } ＋ 到字节集 (到整数 (新函数地址 － (函数地址 ＋ 5)))
			this.newBytes = new byte[] {
				0xE9, 0x00, 0x00, 0x00, 0x00
			};

			Array.Copy(
				// 新函数地址 － 函数地址
				myInt2Bytes(iCallback - hFunc - 5),
				0, newBytes, 1, 4
			);

			uint temp = 0;
			VirtualProtect(oldCodeEntryAddr, (uint)0x05, (uint)Protection.PAGE_EXECUTE_READWRITE, out temp);
			// Debug.WriteLine("oldCodeEntryAddr => " + oldCodeEntryAddr.ToString("X"));
			// Debug.WriteLine("oldBytes => " + ByteArrayToString(oldBytes));
			// Debug.WriteLine("newBytes => " + ByteArrayToString(newBytes));

			// 写到内存 (新字节, 函数地址, 5)  ' 修改API入口前5字节
			mem.WriteMem(hFunc, newBytes, 5);

			// 写到内存 (到整数 (函数地址 ＋ 5 － (原地址 ＋ 10)), 原地址 ＋ 6, 4)
			/*
			BitConverter.GetBytes(false);
			mem.WriteInt(
				this.oldCodeEntryAddr + 6,
				(int)Marshal.GetFunctionPointerForDelegate(callback) + 5 - this.oldCodeEntryAddr + 10
			);
			 */

			FreeLibrary(hModule);
			return true;
		}

		/// <summary>
		/// Pause the hook
		/// </summary>
		public void pause() {
			mem.WriteMem(hFunc, this.oldBytes, 5);
		}

		/// <summary>
		/// Resume the hook
		/// </summary>
		public void resume() {
			mem.WriteMem(hFunc, newBytes);
		}

		/// <summary>
		/// Unload the hook code.
		/// </summary>
		public void unhook() {
			if (hFunc == 0)
				return;
			mem.WriteMem(hFunc, oldBytes);
			uint temp = 0;
			VirtualProtect(hFunc, 5, oldProtection, out temp);
			hFunc = 0;
		}

		/// <summary>
		/// An alternative method.
		/// </summary>
		/// <param name="pOffset">Memory location</param>
		/// <returns>Unicode <b>String</b></returns>
		public string fetchString(int pOffset) {
			byte[] m;
			int myOffset = pOffset;
			var ret = new List<byte>();
			while (true) {
				m = mem.ReadMem(myOffset, 2);
				if (m[0] == 0x00 && m[1] == 0x00)
					break;
				ret.AddRange(m);
				myOffset += 2;
			}

			string mm = Encoding.Unicode.GetString(ret.ToArray());
			return mm;
		}

		#region API Calls
		[DllImport("kernel32.dll")]
		public static extern bool CloseHandle(int hObject);

		[DllImport("kernel32.dll")]
		public static extern int GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll")]
		public static extern int GetProcAddress(int hModule, string procName);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern int LoadLibrary(string lpFileName);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool VirtualProtect(int lpAddress, uint dwSize,
		   uint flNewProtect, out uint lpflOldProtect);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool FreeLibrary(int hModule);

		[Flags]
		public enum Protection : uint {
			PAGE_NOACCESS = 0x01,
			PAGE_READONLY = 0x02,
			PAGE_READWRITE = 0x04,
			PAGE_WRITECOPY = 0x08,
			PAGE_EXECUTE = 0x10,
			PAGE_EXECUTE_READ = 0x20,
			PAGE_EXECUTE_READWRITE = 0x40,
			PAGE_EXECUTE_WRITECOPY = 0x80,
			PAGE_GUARD = 0x100,
			PAGE_NOCACHE = 0x200,
			PAGE_WRITECOMBINE = 0x400
		}

		#endregion
	}

	class addr {
		[DllImport("kernel32.dll")]
		public static extern int lstrcpyn(byte[] var1, byte[] var2, int zero);
		public static int get(byte[] Var) {
			return lstrcpyn(Var, Var, 0);
		}
		public static int get(string Var) {
			byte[] bin = Encoding.Unicode.GetBytes(Var);
			return lstrcpyn(bin, bin, 0);
		}

		[DllImport("kernel32.dll")]
		public static extern int lstrcpyn(char[] var1, char[] var2, int zero);
		public static int get(char[] Var) {
			return lstrcpyn(Var, Var, 0);
		}

		[DllImport("kernel32.dll")]
		public static extern int lstrcpyn(int var1, int var2, int zero);
		public static int get(int Var) {
			return lstrcpyn(Var, Var, 0);
		}
	}

	class memOp {
		[DllImport("kernel32.dll")]
		public static extern bool VirtualProtectEx(int hProcess, int lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);
		[DllImport("kernel32.dll")]
		public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesRead);
		[DllImport("kernel32.dll")]
		public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesWritten);
		[DllImport("kernel32.dll")]
		public static extern int OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int GetCurrentProcessId();

		public int processHandle { get; private set; }

		public memOp() {
			processHandle = OpenProcess(0x001F0FFF, false, GetCurrentProcessId());
		}
		public byte[] ReadMem(int pOffset, int pSize) {
			byte[] buffer = new byte[pSize];
			ReadProcessMemory(this.processHandle, pOffset, buffer, pSize, 0);
			return buffer;
		}

		public string CutString(string mystring) {
			char[] chArray = mystring.ToCharArray();
			string str = "";
			for (int i = 0; i < mystring.Length; i++) {
				if ((chArray[i] == ' ') && (chArray[i + 1] == ' ')) {
					return str;
				}
				if (chArray[i] == '\0') {
					return str;
				}
				str = str + chArray[i].ToString();
			}
			return mystring.TrimEnd(new char[] { '0' });
		}

		public byte ReadByte(int pOffset) {
			byte[] buffer = new byte[1];
			ReadProcessMemory(this.processHandle, pOffset, buffer, 1, 0);
			return buffer[0];
		}

		public void WriteMem(int pOffset, byte[] pBytes) {
			WriteProcessMemory(this.processHandle, pOffset, pBytes, pBytes.Length, 0);
		}
		public void WriteMem(int pOffset, byte[] pBytes, int dLen) {
			WriteProcessMemory(this.processHandle, pOffset, pBytes, dLen, 0);
		}

		public float ReadFloat(int pOffset) {
			return BitConverter.ToSingle(this.ReadMem(pOffset, 4), 0);
		}

		public int ReadInt(int pOffset) {
			return BitConverter.ToInt32(this.ReadMem(pOffset, 4), 0);
		}

		public short ReadShort(int pOffset) {
			return BitConverter.ToInt16(this.ReadMem(pOffset, 2), 0);
		}

		public string ReadStringAscii(int pOffset, int pSize) {
			return this.CutString(Encoding.ASCII.GetString(this.ReadMem(pOffset, pSize)));
		}

		public string ReadStringUnicode(int pOffset, int pSize) {
			return this.CutString(Encoding.Unicode.GetString(this.ReadMem(pOffset, pSize)));
		}

		public uint ReadUInt(int pOffset) {
			return BitConverter.ToUInt32(this.ReadMem(pOffset, 4), 0);
		}

		public void WriteByte(int pOffset, byte pBytes) {
			this.WriteMem(pOffset, BitConverter.GetBytes((short)pBytes));
		}

		public void WriteDouble(int pOffset, double pBytes) {
			this.WriteMem(pOffset, BitConverter.GetBytes(pBytes));
		}

		public void WriteFloat(int pOffset, float pBytes) {
			this.WriteMem(pOffset, BitConverter.GetBytes(pBytes));
		}

		public void WriteInt(int pOffset, int pBytes) {
			this.WriteMem(pOffset, BitConverter.GetBytes(pBytes));
		}

		public void WriteShort(int pOffset, short pBytes) {
			this.WriteMem(pOffset, BitConverter.GetBytes(pBytes));
		}

		public void WriteStringAscii(int pOffset, string pBytes) {
			this.WriteMem(pOffset, Encoding.ASCII.GetBytes(pBytes + "\0"));
		}

		public void WriteStringUnicode(int pOffset, string pBytes) {
			this.WriteMem(pOffset, Encoding.Unicode.GetBytes(pBytes + "\0"));
		}

		public void WriteUInt(int pOffset, uint pBytes) {
			this.WriteMem(pOffset, BitConverter.GetBytes(pBytes));
		}

	}
}
