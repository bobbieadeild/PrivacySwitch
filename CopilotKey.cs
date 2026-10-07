using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

// Stores modifier state and one F23 press decision only; no typed content/history.
public sealed class CopilotChord {
 public const int LS=0xA0,RS=0xA1,LW=0x5B,RW=0x5C,LC=0xA2,RC=0xA3,LA=0xA4,RA=0xA5,F23=0x86;
 bool ls,rs,lw,rw,lc,rc,la,ra,f23Held,f23Blocked;
 public void Sync(Func<int,bool> held){ls=held(LS);rs=held(RS);lw=held(LW);rw=held(RW);lc=held(LC);rc=held(RC);la=held(LA);ra=held(RA);}
 public void Initial(Func<int,bool> held){Sync(held);f23Held=held(F23);f23Blocked=false;}
 public bool Process(int key,bool down){switch(key){case LS:ls=down;break;case RS:rs=down;break;case LW:lw=down;break;case RW:rw=down;break;case LC:lc=down;break;case RC:rc=down;break;case LA:la=down;break;case RA:ra=down;break;}

  if(key!=F23)return false;
  if(down){if(!f23Held){f23Held=true;f23Blocked=ls&&lw&&!rs&&!rw&&!lc&&!rc&&!la&&!ra;}return f23Blocked;}
  bool block=f23Held&&f23Blocked;f23Held=false;f23Blocked=false;return block;
 }
 public void Clear(){ls=rs=lw=rw=lc=rc=la=ra=f23Held=f23Blocked=false;}
}

public sealed class HookLease:IDisposable {
 IntPtr handle;readonly Func<IntPtr,bool> release;
 public HookLease(IntPtr value,Func<IntPtr,bool> unhook){if(value==IntPtr.Zero)throw new InvalidOperationException("Hook installation failed");handle=value;release=unhook;}
 public void Dispose(){if(handle==IntPtr.Zero)return;if(!release(handle))throw new InvalidOperationException("Windows did not confirm hook removal");handle=IntPtr.Zero;}
}

public sealed class CopilotKeySession:IDisposable {
 delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
 [StructLayout(LayoutKind.Sequential)]struct Message{public IntPtr hwnd;public uint message;public UIntPtr wParam;public IntPtr lParam;public uint time;public int x,y;public uint extra;}
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SetWindowsHookEx(int type,HookProc callback,IntPtr module,uint thread);
 [DllImport("user32.dll",SetLastError=true)]static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Auto)]static extern IntPtr GetModuleHandle(string name);
 [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")]static extern bool PeekMessage(out Message m,IntPtr hwnd,uint min,uint max,uint flags);
 [DllImport("user32.dll",SetLastError=true)]static extern int GetMessage(out Message m,IntPtr hwnd,uint min,uint max);
 [DllImport("user32.dll",SetLastError=true)]static extern bool PostThreadMessage(uint thread,uint message,UIntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern UIntPtr SetTimer(IntPtr window,UIntPtr id,uint milliseconds,IntPtr callback);
 [DllImport("user32.dll")]static extern bool KillTimer(IntPtr window,UIntPtr id);
 readonly CopilotChord chord=new CopilotChord();readonly ManualResetEvent ready=new ManualResetEvent(false);
 Thread thread;uint threadId;IntPtr hook;HookProc callback;volatile bool installed,stopping;string error;
 public event Action Changed;
 public bool Installed{get{return installed;}} public string Error{get{return error;}}
 static bool Held(int key){return (GetAsyncKeyState(key)&0x8000)!=0;}
 public void Start(){if(thread!=null)throw new InvalidOperationException("Session already started");thread=new Thread(Run){IsBackground=true,Name="Copilot chord filter"};thread.SetApartmentState(ApartmentState.STA);thread.Start();if(!ready.WaitOne(3000)){Stop();throw new Exception("Hook startup timed out");}if(!installed){Stop();throw new Exception(error??"Hook installation failed");}}
 void Run(){HookLease lease=null;UIntPtr timer=UIntPtr.Zero;try{threadId=GetCurrentThreadId();Message message;PeekMessage(out message,IntPtr.Zero,0,0,0);if(stopping)throw new OperationCanceledException("Startup cancelled");chord.Initial(Held);callback=Handle;hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);if(hook==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());lease=new HookLease(hook,UnhookWindowsHookEx);timer=SetTimer(IntPtr.Zero,UIntPtr.Zero,150,IntPtr.Zero);if(timer==UIntPtr.Zero)throw new Exception("Modifier recovery timer failed");if(stopping)PostThreadMessage(threadId,0x12,UIntPtr.Zero,IntPtr.Zero);installed=true;ready.Set();int result;while((result=GetMessage(out message,IntPtr.Zero,0,0))>0){if(message.message==0x113)chord.Sync(Held);}if(result<0)throw new Win32Exception(Marshal.GetLastWin32Error());}catch(Exception e){error=e.Message;}finally{if(timer!=UIntPtr.Zero)KillTimer(IntPtr.Zero,timer);if(lease!=null)try{lease.Dispose();}catch(Exception e){error=e.Message;}hook=IntPtr.Zero;installed=false;chord.Clear();ready.Set();var changed=Changed;if(changed!=null)try{changed();}catch{}}}
 IntPtr Handle(int code,IntPtr message,IntPtr data){if(code!=0)return CallNextHookEx(hook,code,message,data);try{int m=message.ToInt32();bool down=m==0x100||m==0x104;bool up=m==0x101||m==0x105;if(!down&&!up)return CallNextHookEx(hook,code,message,data);bool block=chord.Process(Marshal.ReadInt32(data),down);if(block)return new IntPtr(1);}catch(Exception e){error=e.Message;PostThreadMessage(threadId,0x12,UIntPtr.Zero,IntPtr.Zero);}return CallNextHookEx(hook,code,message,data);}
 public void Stop(){stopping=true;if(thread==null)return;if(thread.IsAlive){if(threadId!=0&&!PostThreadMessage(threadId,0x12,UIntPtr.Zero,IntPtr.Zero))error="Could not request hook shutdown";if(Thread.CurrentThread!=thread&&!thread.Join(3000))throw new Exception("Hook shutdown not confirmed; close the app to stop this session");}chord.Clear();}
 public void Dispose(){Stop();if(thread==null||!thread.IsAlive)ready.Dispose();}
}

public static class CopilotKeyTests {
 public static int Run(){try{var c=new CopilotChord();Func<int,bool> none=k=>false;c.Initial(none);c.Process(CopilotChord.LS,true);c.Process(CopilotChord.LW,true);if(!c.Process(CopilotChord.F23,true))throw new Exception("chord down");c.Process(CopilotChord.LS,false);c.Process(CopilotChord.LW,false);if(!c.Process(CopilotChord.F23,true)||!c.Process(CopilotChord.F23,false))throw new Exception("repeat/release order");if(c.Process(CopilotChord.F23,true))throw new Exception("bare F23");c.Process(CopilotChord.LS,true);c.Process(CopilotChord.LW,true);if(c.Process(CopilotChord.F23,true)||c.Process(CopilotChord.F23,false))throw new Exception("unrelated held repeat");c.Process(CopilotChord.LC,true);c.Process(CopilotChord.RC,true);c.Process(CopilotChord.LC,false);c.Process(CopilotChord.LA,true);c.Process(CopilotChord.RA,true);c.Process(CopilotChord.LA,false);if(c.Process(0x51,true)||c.Process(0x51,false))throw new Exception("Ctrl Alt Q must pass through");if(c.Process(CopilotChord.F23,true))throw new Exception("extra-modifier shortcut");c.Clear();c.Sync(k=>k==CopilotChord.LS||k==CopilotChord.LW);if(!c.Process(CopilotChord.F23,true))throw new Exception("missed modifier recovery");c.Clear();if(c.Process(CopilotChord.F23,false))throw new Exception("clear state");c.Initial(k=>k==CopilotChord.F23||k==CopilotChord.LS||k==CopilotChord.LW);if(c.Process(CopilotChord.F23,true)||c.Process(CopilotChord.F23,false))throw new Exception("initial held F23 swallowed");c.Clear();c.Process(CopilotChord.LW,true);c.Process(CopilotChord.LS,true);if(!c.Process(CopilotChord.F23,true))throw new Exception("reverse modifier down order");c.Process(CopilotChord.LW,false);if(!c.Process(CopilotChord.F23,false))throw new Exception("reverse release order");if(c.Process(0x41,true)||c.Process(0x41,false))throw new Exception("ordinary key swallowed");int releases=0;using(var lease=new HookLease(new IntPtr(1),h=>{releases++;return true;})){lease.Dispose();}if(releases!=1)throw new Exception("unhook exactly once");bool failed=false;try{new HookLease(IntPtr.Zero,h=>true);}catch{failed=true;}if(!failed)throw new Exception("install failure");var retry=new HookLease(new IntPtr(2),h=>{releases++;return releases>2;});try{retry.Dispose();}catch{}retry.Dispose();return 0;}catch{return 1;}}
}



