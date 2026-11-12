using System;
partial class MotionRegression
{
    static int passed,failed;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Run(string name,Action test)
    {
        try{MemoryFiles.Reset();test();passed++;Console.WriteLine("PASS "+name);}
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}
    }
    static void Main()
    {
        WorldMapRegressionTests();StoryOriginTests();N4Tests();
        Console.WriteLine("RESULT "+passed+" PASS, "+failed+" FAIL (production host/SaveManager + in-memory OS/JSON boundary)");
        Console.WriteLine("NOT TESTED: native Unity JSON/clone/lifecycle, real Save I/O/fsync/crash, Windows Player.");
        if(failed>0)Environment.Exit(1);
    }
}
