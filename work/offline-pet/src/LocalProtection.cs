using System;using System.IO;using System.Runtime.InteropServices;using System.Security.Cryptography;using System.Security.Principal;using Microsoft.Win32;
internal static class LocalProtection {
 [StructLayout(LayoutKind.Sequential)]struct Blob{internal int Size;internal IntPtr Data;}
 [DllImport("crypt32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool CryptProtectData(ref Blob input,string description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
 [DllImport("crypt32.dll",SetLastError=true)]static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
 [DllImport("kernel32.dll")]static extern IntPtr LocalFree(IntPtr memory);
 internal static byte[] Protect(byte[] value){return Transform(value,true);}
 internal static byte[] Unprotect(byte[] value){return Transform(value,false);}
 static byte[] Transform(byte[] value,bool protect){var input=new Blob{Size=value.Length,Data=Marshal.AllocHGlobal(value.Length)};var output=new Blob();try{Marshal.Copy(value,0,input.Data,value.Length);bool ok=protect?CryptProtectData(ref input,"Conan local authorization",IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output):CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);if(!ok)throw new CryptographicException(Marshal.GetLastWin32Error());var result=new byte[output.Size];Marshal.Copy(output.Data,result,0,result.Length);return result;}finally{if(output.Data!=IntPtr.Zero)LocalFree(output.Data);Marshal.FreeHGlobal(input.Data);}}
 internal static string Machine{get{using(var root=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64))using(var key=root.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")){string id=key==null?null:key.GetValue("MachineGuid") as string;if(String.IsNullOrWhiteSpace(id))throw new IOException("无法识别本机，暂不保存程序授权。");return id;}}}
 internal static string User{get{using(var identity=WindowsIdentity.GetCurrent())return identity.User.Value;}}
}
