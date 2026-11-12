using System;

partial class MotionRegression
{
    static int passed,failed;
    static void Check(bool condition,string message) {if(!condition)throw new Exception(message);}
    static void Run(string name,Action test)
    {
        try {test();passed++;Console.WriteLine("PASS "+name);}
        catch(Exception e) {failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}
    }
    static void Main()
    {
        WorldMapRegressionTests();StoryOriginTests();N3DomainTests();N3InteractionTests();
        Console.WriteLine("RESULT "+passed+" PASS, "+failed+" FAIL (UnityStub/domain + real origin UI/host fragment; no Save I/O)");
        Console.WriteLine("NOT TESTED: native lifecycle/Retry/Travel/Load, SaveManager transactions, Unity renderer/input, Windows Player.");
        if(failed>0)Environment.Exit(1);
    }
}
