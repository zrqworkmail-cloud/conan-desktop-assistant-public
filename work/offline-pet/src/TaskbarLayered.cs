using System;using System.Drawing;using System.Runtime.InteropServices;
sealed partial class TaskbarMonitor {
 [StructLayout(LayoutKind.Sequential)]struct LayerPoint {public int X,Y;public LayerPoint(int x,int y){X=x;Y=y;}}
 [StructLayout(LayoutKind.Sequential)]struct LayerSize {public int Width,Height;public LayerSize(int w,int h){Width=w;Height=h;}}
 [StructLayout(LayoutKind.Sequential,Pack=1)]struct LayerBlend {public byte Operation,Flags,Alpha,Format;}
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr screen,ref LayerPoint destination,ref LayerSize size,IntPtr source,ref LayerPoint origin,uint key,ref LayerBlend blend,uint flags);
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr hwnd);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr value);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr value);
 internal int LastLayeredError;internal long LayeredFrames;
 internal bool PresentLayered(Bitmap image){IntPtr screen=IntPtr.Zero,memory=IntPtr.Zero,bitmap=IntPtr.Zero,old=IntPtr.Zero;try{
  screen=GetDC(IntPtr.Zero);memory=CreateCompatibleDC(screen);if(screen==IntPtr.Zero||memory==IntPtr.Zero){LastLayeredError=Marshal.GetLastWin32Error();return false;}
  bitmap=image.GetHbitmap(Color.FromArgb(0));old=SelectObject(memory,bitmap);var position=new LayerPoint(Left,Top);var origin=new LayerPoint(0,0);var size=new LayerSize(image.Width,image.Height);var blend=new LayerBlend{Alpha=255,Format=1};bool ok=UpdateLayeredWindow(Handle,screen,ref position,ref size,memory,ref origin,0,ref blend,2);LastLayeredError=ok?0:Marshal.GetLastWin32Error();if(ok)LayeredFrames++;return ok;
 }finally{if(old!=IntPtr.Zero&&old!=new IntPtr(-1))SelectObject(memory,old);if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(memory!=IntPtr.Zero)DeleteDC(memory);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}}
}
