using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// In-memory OS boundary only. Production SaveGame/WriteToFile/Load validators
// and ReplaceExpected execute unchanged; no file in a player's directory is
// opened. This does not model fsync, Windows sharing, power loss or native I/O.
internal static class MemoryFiles
{
    internal static readonly Dictionary<string,byte[]> Bytes=new Dictionary<string,byte[]>();
    internal static bool FailDuringWrite;
    internal static Action AfterTempFlush;
    internal static Action CloneHook;
    internal static int Replaces;
    internal static void Reset(){Bytes.Clear();FailDuringWrite=false;AfterTempFlush=null;CloneHook=null;Replaces=0;}
    public static bool Exists(string path)=>Bytes.ContainsKey(path);
    public static byte[] ReadAllBytes(string path)
    {if(!Exists(path))throw new FileNotFoundException(path);return (byte[])Bytes[path].Clone();}
    public static string ReadAllText(string path)=>Encoding.UTF8.GetString(ReadAllBytes(path)).TrimStart('\uFEFF');
    public static void WriteAllBytes(string path,byte[] bytes){Bytes[path]=(byte[])bytes.Clone();}
    public static void Delete(string path){Bytes.Remove(path);}
    public static DateTime GetLastWriteTimeUtc(string path)=>new DateTime(2026,11,12,0,0,0,DateTimeKind.Utc);
    public static FileAttributes GetAttributes(string path)=>FileAttributes.Normal;
    public static void Move(string from,string to)
    {if(Exists(to))throw new IOException("Destination exists");WriteAllBytes(to,ReadAllBytes(from));Delete(from);Replaces++;}
    public static void Replace(string from,string to,string backup)
    {if(!Exists(to))throw new FileNotFoundException(to);if(backup!=null)WriteAllBytes(backup,ReadAllBytes(to));WriteAllBytes(to,ReadAllBytes(from));Delete(from);Replaces++;}
}

internal sealed class MemoryFileStream : MemoryStream
{
    readonly string path;
    public MemoryFileStream(string path,FileMode mode,FileAccess access,FileShare share)
    {
        this.path=path;
        if(mode==FileMode.CreateNew && MemoryFiles.Exists(path))throw new IOException("Already exists");
        if(mode==FileMode.Create || mode==FileMode.CreateNew)MemoryFiles.WriteAllBytes(path,new byte[0]);
        else if(MemoryFiles.Exists(path)){var bytes=MemoryFiles.ReadAllBytes(path);base.Write(bytes,0,bytes.Length);Position=0;}
    }
    public override void Write(byte[] buffer,int offset,int count)
    {
        if(MemoryFiles.FailDuringWrite)
        {
            MemoryFiles.FailDuringWrite=false;
            if(count>0)base.Write(buffer,offset,Math.Max(1,count/2));
            MemoryFiles.WriteAllBytes(path,ToArray());throw new IOException("Owned in-memory interrupted write");
        }
        base.Write(buffer,offset,count);
    }
    public override void Flush(){MemoryFiles.WriteAllBytes(path,ToArray());}
    public void Flush(bool flushToDisk)
    {
        Flush();var callback=MemoryFiles.AfterTempFlush;MemoryFiles.AfterTempFlush=null;callback?.Invoke();
    }
    protected override void Dispose(bool disposing)
    {if(disposing)MemoryFiles.WriteAllBytes(path,ToArray());base.Dispose(disposing);}
}

internal static class MemoryDirectories
{
    public static bool Exists(string path)=>true;
    public static DirectoryInfo CreateDirectory(string path)=>null;
}
