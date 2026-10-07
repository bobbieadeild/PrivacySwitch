using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public class CaptureState { public string Id; public uint State; }
public static class CoreAudio {
 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class EnumeratorObject {}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Enumerator {
  void EnumAudioEndpoints(int flow,uint mask,out Collection collection);
  void GetDefaultAudioEndpoint(int flow,int role,out Endpoint endpoint);
  void GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out Endpoint endpoint);
  void RegisterEndpointNotificationCallback(IntPtr callback);
  void UnregisterEndpointNotificationCallback(IntPtr callback);
 }
 [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Collection {
  void GetCount(out uint count); void Item(uint index,out Endpoint endpoint);
 }
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Endpoint {
  void Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object result);
  void OpenPropertyStore(uint access,out IntPtr store);
  void GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
  void GetState(out uint state);
 }
 [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface Volume {
  void RegisterControlChangeNotify(IntPtr p);void UnregisterControlChangeNotify(IntPtr p);void GetChannelCount(out uint c);
  void SetMasterVolumeLevel(float v,ref Guid g);void SetMasterVolumeLevelScalar(float v,ref Guid g);void GetMasterVolumeLevel(out float v);void GetMasterVolumeLevelScalar(out float v);
  void SetChannelVolumeLevel(uint c,float v,ref Guid g);void SetChannelVolumeLevelScalar(uint c,float v,ref Guid g);void GetChannelVolumeLevel(uint c,out float v);void GetChannelVolumeLevelScalar(uint c,out float v);
  void SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid g);void GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
  void GetVolumeStepInfo(out uint a,out uint b);void VolumeStepUp(ref Guid g);void VolumeStepDown(ref Guid g);void QueryHardwareSupport(out uint mask);void GetVolumeRange(out float min,out float max,out float step);
 }
 public static uint Hardware(string id){return Access(id,(v)=>{uint mask;v.QueryHardwareSupport(out mask);return mask;});}
 public static bool Muted(string id){return Access(id,(v)=>{bool mute;v.GetMute(out mute);return mute;});}
 public static void Mute(string id,bool muted){Access(id,(v)=>{uint mask;v.QueryHardwareSupport(out mask);if((mask&2)==0)throw new Exception("Hardware microphone mute is unsupported; software-only mute cannot guarantee exclusive-mode capture is cut");Guid g=Guid.Empty;v.SetMute(muted,ref g);bool result;v.GetMute(out result);if(result!=muted)throw new Exception("Hardware mute readback failed");return true;});}
 static T Access<T>(string id,Func<Volume,T> action){if(!Capture().Exists(capture=>capture.Id.Equals(id,StringComparison.OrdinalIgnoreCase)))throw new Exception("Not a capture endpoint");Enumerator e=null;Endpoint d=null;object v=null;try{e=(Enumerator)new EnumeratorObject();e.GetDevice(id,out d);Guid iid=new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");d.Activate(ref iid,23,IntPtr.Zero,out v);return action((Volume)v);}finally{if(v!=null)Marshal.ReleaseComObject(v);if(d!=null)Marshal.ReleaseComObject(d);if(e!=null)Marshal.ReleaseComObject(e);}} public static List<CaptureState> Capture(){var results=new List<CaptureState>();Enumerator e=null;Collection c=null;try{e=(Enumerator)new EnumeratorObject();e.EnumAudioEndpoints(1,15,out c);uint count;c.GetCount(out count);for(uint i=0;i<count;i++){Endpoint d=null;try{c.Item(i,out d);string id;uint state;d.GetId(out id);d.GetState(out state);results.Add(new CaptureState{Id=id,State=state});}finally{if(d!=null)Marshal.ReleaseComObject(d);}}}finally{if(c!=null)Marshal.ReleaseComObject(c);if(e!=null)Marshal.ReleaseComObject(e);}return results;}
 public static string Describe(uint state){return state==1?"ACTIVE (capture remains available)":state==2?"DISABLED in Core Audio":state==4?"NOT PRESENT":state==8?"UNPLUGGED":"state "+state;}
}



