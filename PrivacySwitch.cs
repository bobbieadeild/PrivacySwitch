using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using System.Xml.Serialization;


public class Device { public string Id, Name, Class, Manufacturer, Driver; public uint Status, Problem; public bool Selected; }
public class Entry { public string Id, Name, Type, Class; public bool OriginalMute, Pending, Done; public string Result; }
public class Journal { public List<Entry> Entries = new List<Entry>(); public string Created; }
public static class Native {
 [StructLayout(LayoutKind.Sequential)] public struct Info { public uint Size; public Guid Class; public uint Dev; public IntPtr Reserved; }
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode)] static extern IntPtr SetupDiGetClassDevs(IntPtr g,string e,IntPtr w,uint f);
 [DllImport("setupapi.dll",SetLastError=true)] static extern bool SetupDiEnumDeviceInfo(IntPtr h,uint n,ref Info i);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode)] static extern bool SetupDiGetDeviceInstanceId(IntPtr h,ref Info i,System.Text.StringBuilder b,uint s,out uint r);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode)] static extern bool SetupDiGetDeviceRegistryProperty(IntPtr h,ref Info i,uint p,out uint t,byte[] b,uint s,out uint r);
 [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr h);
 [DllImport("cfgmgr32.dll")] public static extern uint CM_Get_DevNode_Status(out uint s,out uint p,uint d,uint f);
 [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)] static extern uint CM_Locate_DevNode(out uint d,string id,uint f);
 [DllImport("cfgmgr32.dll")] static extern uint CM_Disable_DevNode(uint d,uint f);
 [DllImport("cfgmgr32.dll")] static extern uint CM_Enable_DevNode(uint d,uint f);
 static string DriverInfo(IntPtr h,ref Info i){string key=Prop(h,ref i,9);if(key=="")return "driver key unavailable";try{using(var k=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\"+key)){return k==null?"driver metadata unavailable":Convert.ToString(k.GetValue("ProviderName"))+" | "+Convert.ToString(k.GetValue("DriverVersion"))+" | "+Convert.ToString(k.GetValue("DriverDate"));}}catch{return "driver metadata permission unavailable";}}
 static string Prop(IntPtr h,ref Info i,uint p) { byte[] b=new byte[8192]; uint t,r; return SetupDiGetDeviceRegistryProperty(h,ref i,p,out t,b,(uint)b.Length,out r)?System.Text.Encoding.Unicode.GetString(b,0,(int)r).TrimEnd('\0'):""; }
 public static bool Choose(string c,string id,string name) { return c.Equals("Camera",StringComparison.OrdinalIgnoreCase) || (c.Equals("AudioEndpoint",StringComparison.OrdinalIgnoreCase)&&id.IndexOf("{0.0.1.",StringComparison.OrdinalIgnoreCase)>=0); }
 public static List<Device> Inventory() { var list=new List<Device>(); IntPtr h=SetupDiGetClassDevs(IntPtr.Zero,null,IntPtr.Zero,6); if(h==new IntPtr(-1)) throw new Exception("Windows device inventory unavailable."); try { for(uint n=0;;n++){ Info i=new Info(); i.Size=(uint)Marshal.SizeOf(i); if(!SetupDiEnumDeviceInfo(h,n,ref i)) { int error=Marshal.GetLastWin32Error(); if(error!=259 && error!=0) throw new Exception("Device enumeration failed: "+error); break; } var b=new System.Text.StringBuilder(2048); uint r,s,p; if(!SetupDiGetDeviceInstanceId(h,ref i,b,2048,out r)) throw new Exception("Cannot read device identity"); if(CM_Get_DevNode_Status(out s,out p,i.Dev,0)!=0) throw new Exception("Cannot read device status"); string c=Prop(h,ref i,7),name=Prop(h,ref i,12); if(name=="")name=Prop(h,ref i,0); list.Add(new Device{Id=b.ToString(),Name=name,Class=c,Status=s,Problem=p,Manufacturer=Prop(h,ref i,11),Driver=DriverInfo(h,ref i),Selected=Choose(c,b.ToString(),name)}); } } finally{SetupDiDestroyDeviceInfoList(h);} return list; }
 public static uint Problem(string id) {uint d,s,p; uint r=CM_Locate_DevNode(out d,id,0); if(r!=0)throw new Exception("Device missing/unavailable ("+r+")"); r=CM_Get_DevNode_Status(out s,out p,d,0); if(r!=0)throw new Exception("Status unavailable ("+r+")"); return p; }
 public static void Change(string id,bool disable) {uint d; uint r=CM_Locate_DevNode(out d,id,0); if(r!=0)throw new Exception("Locate failed: "+r); r=disable?CM_Disable_DevNode(d,8):CM_Enable_DevNode(d,0); if(r!=0)throw new Exception("Windows refused device change: "+r); if(Problem(id)!=(disable?22u:0u))throw new Exception("Change not verified; restart or driver intervention may be required"); }
}
public static class Engine {
 public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"PrivacySwitch");
 public static string FilePath {get{return Path.Combine(Root,"recovery.xml");}}
 public static Func<string,uint> DeviceStatus=Native.Problem;
 public static Action<string,bool> DeviceChange=Native.Change;
 public static Func<List<Device>> Inventory=Native.Inventory;
 public static Func<List<CaptureState>> Capture=CoreAudio.Capture;
 public static Func<string,uint> Hardware=CoreAudio.Hardware;
 public static Func<string,bool> Muted=CoreAudio.Muted;
 public static Action<string,bool> Mute=CoreAudio.Mute;
 public static Journal Load(){using(var f=File.OpenRead(FilePath)){var j=(Journal)new XmlSerializer(typeof(Journal)).Deserialize(f);foreach(var e in j.Entries)if(!((e.Type=="device"&&Native.Choose(e.Class??"",e.Id??"",e.Name??""))||(e.Type=="hardware-mute"&&e.Class=="AudioEndpoint"&&(e.Id??"").StartsWith("{0.0.1.",StringComparison.OrdinalIgnoreCase))))throw new Exception("Recovery contains unsupported or legacy targets. It is preserved; no changes made.");return j;}}
 public static void Save(Journal j){Directory.CreateDirectory(Root);string tmp=FilePath+".tmp";using(var f=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(Journal)).Serialize(f,j);f.Flush(true);}if(File.Exists(FilePath))File.Replace(tmp,FilePath,FilePath+".bak");else File.Move(tmp,FilePath);}
 public static void Apply(){if(File.Exists(FilePath))throw new Exception("Recovery exists. Restore first.");var j=new Journal{Created=DateTime.UtcNow.ToString("o")};var inventory=Inventory();foreach(var d in inventory)if(d.Class.Equals("Camera",StringComparison.OrdinalIgnoreCase)&&d.Problem==0)j.Entries.Add(new Entry{Id=d.Id,Name=d.Name,Class=d.Class,Type="device"});foreach(var a in Capture())if(a.State==1){string name=a.Id;var pnp=inventory.Find(d=>d.Id.EndsWith(a.Id,StringComparison.OrdinalIgnoreCase));if(pnp!=null)name=pnp.Name;if((Hardware(a.Id)&2)==0){j.Entries.Add(new Entry{Id=a.Id,Name=name,Class="AudioEndpoint",Type="hardware-mute",Result="UNSUPPORTED: no hardware mute; microphone remains available"});continue;}bool original=Muted(a.Id);j.Entries.Add(new Entry{Id=a.Id,Name=name,Class="AudioEndpoint",Type="hardware-mute",OriginalMute=original,Done=original,Result=original?"Already muted; original mute will be preserved":null});}if(j.Entries.Count==0)throw new Exception("No supported active targets found. No changes made.");Save(j);foreach(var e in j.Entries){if(e.Done||e.Result!=null)continue;try{if(e.Type=="hardware-mute"){if(Muted(e.Id)!=e.OriginalMute)throw new Exception("Mute changed since snapshot; skipped");e.Pending=true;Save(j);Mute(e.Id,true);if(!Muted(e.Id))throw new Exception("Hardware mute not verified");e.Result="Driver reports hardware-muted; live recording cutoff UNTESTED";}else{if(DeviceStatus(e.Id)!=0)throw new Exception("Device changed since snapshot; skipped");e.Pending=true;Save(j);DeviceChange(e.Id,true);if(DeviceStatus(e.Id)!=22)throw new Exception("Disabled status not verified");e.Result="Camera devnode disabled; live camera cutoff UNTESTED";}e.Done=true;}catch(Exception x){e.Result="Failed: "+x.Message;}Save(j);}}
 public static void Restore(){var j=Load();for(int i=j.Entries.Count-1;i>=0;i--){var e=j.Entries[i];if(!e.Pending)continue;try{if(e.Type=="hardware-mute"){if(Muted(e.Id)!=e.OriginalMute)Mute(e.Id,e.OriginalMute);if(Muted(e.Id)!=e.OriginalMute)throw new Exception("Original mute readback failed");}else{uint p=DeviceStatus(e.Id);if(p==22)DeviceChange(e.Id,false);else if(p!=0)throw new Exception("Unexpected device state; manual review needed");if(DeviceStatus(e.Id)!=0)throw new Exception("Restored status not verified");}e.Pending=false;e.Done=false;e.Result="Original state readback restored";}catch(Exception x){e.Result="Restore failed: "+x.Message;}Save(j);}if(j.Entries.TrueForAll(e=>!e.Pending))File.Move(FilePath,Path.Combine(Root,"audit-"+Guid.NewGuid()+".xml"));}
}
public static class Tests {
 public static int Run(){try{
  if(!Native.Choose("AudioEndpoint",@"SWD\MMDEVAPI\{0.0.1.00000000}.abc","Mic")||Native.Choose("AudioEndpoint",@"SWD\MMDEVAPI\{0.0.0.00000000}.abc","Speaker")||Native.Choose("Image","x","Scanner")||Native.Choose("Sensor","x","GPS")||Native.Choose("Net","x","WiFi"))throw new Exception("selection");
  Engine.Root=Path.Combine(Environment.CurrentDirectory,"work","PrivacySwitch-tests-"+Guid.NewGuid());
  string mic="{0.0.1.00000000}.mic";bool muted=false;uint camera=0;int muteCalls=0,deviceCalls=0;
  Engine.Inventory=()=>new List<Device>{new Device{Id="camera",Name="Camera",Class="Camera",Problem=camera},new Device{Id="already-disabled",Name="Camera",Class="Camera",Problem=22},new Device{Id="speaker",Class="AudioEndpoint",Problem=0},new Device{Id="network",Class="Net",Problem=0}};
  Engine.Capture=()=>new List<CaptureState>{new CaptureState{Id=mic,State=1}};
  Engine.Hardware=id=>3;Engine.Muted=id=>muted;Engine.Mute=(id,value)=>{if(id!=mic)throw new Exception("wrong mic");muteCalls++;muted=value;};Engine.DeviceStatus=id=>camera;Engine.DeviceChange=(id,disable)=>{if(id!="camera")throw new Exception("wrong camera");deviceCalls++;camera=disable?22u:0u;};
  Engine.Apply();if(!muted||camera!=22||muteCalls!=1||deviceCalls!=1||Engine.Load().Entries.Count!=2)throw new Exception("apply");if(!File.Exists(Engine.FilePath+".bak"))throw new Exception("atomic backup");Engine.Restore();if(muted||camera!=0||File.Exists(Engine.FilePath))throw new Exception("original restore");
  muted=true;Engine.Apply();Engine.Restore();if(!muted||muteCalls!=2)throw new Exception("original muted changed");
  muted=false;Engine.Hardware=id=>0;Engine.Apply();if(muted||Engine.Load().Entries.Find(e=>e.Type=="hardware-mute").Done)throw new Exception("software mute accepted");Engine.Restore();
  Engine.Hardware=id=>3;Engine.Apply();Engine.Mute=(id,value)=>{throw new Exception("denied");};Engine.Restore();if(!File.Exists(Engine.FilePath)||!Engine.Load().Entries.Exists(e=>e.Pending))throw new Exception("failed restore lost");Engine.Mute=(id,value)=>muted=value;Engine.Restore();
  Engine.Mute=(id,value)=>{};muted=false;Engine.Apply();if(Engine.Load().Entries.Find(e=>e.Type=="hardware-mute").Done)throw new Exception("false mute readback accepted");Engine.Restore();Engine.Mute=(id,value)=>muted=value;var legacy=new Journal();legacy.Entries.Add(new Entry{Id="sensor",Class="Sensor",Type="device",Pending=true});Engine.Save(legacy);bool rejected=false;try{Engine.Restore();}catch{rejected=true;}if(!rejected)throw new Exception("scope guard");
  Engine.Root=Path.Combine(Engine.Root,"crash");var crash=new Journal();crash.Entries.Add(new Entry{Id=mic,Class="AudioEndpoint",Type="hardware-mute",Pending=true,OriginalMute=false});muted=true;Engine.Save(crash);Engine.Restore();if(muted)throw new Exception("crash recovery");return 0;
 }catch(Exception x){File.WriteAllText(Path.Combine(Path.GetTempPath(),"PrivacySwitch-test-error.txt"),x.ToString());return 1;}}
}
public static class Compatibility {
 public static string OS(){try{using(var k=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")){string build=Convert.ToString(k.GetValue("CurrentBuildNumber"));int number;string family=Int32.TryParse(build,out number)&&number>=22000?"Windows 11":"Windows";return family+" "+Convert.ToString(k.GetValue("DisplayVersion"))+" | build "+build+"."+Convert.ToString(k.GetValue("UBR"))+" | edition "+Convert.ToString(k.GetValue("EditionID"))+" | process "+(Environment.Is64BitProcess?"64-bit":"32-bit");}}catch{return Environment.OSVersion+" (detailed detection unavailable)";}}
}


