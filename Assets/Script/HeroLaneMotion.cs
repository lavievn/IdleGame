using System;

// Per-Hero, unsaved Y-only motion. No Unity random state, X, camera, combat
// timers, target selection or exploration distance is accessed here.
public sealed class HeroLaneMotion
{
    public float TargetY { get; private set; }
    public float HoldRemaining { get; private set; }
    public bool HasTarget { get; private set; }
    private int direction=1;

    public void Reset(bool magic)
    {
        HasTarget=false;TargetY=0f;HoldRemaining=0f;direction=magic?-1:1;
    }
    private static float Positive(float value)
    {
        return float.IsNaN(value)||float.IsInfinity(value)?0f:Math.Max(0f,value);
    }
    private static float Clamp(float value,float low,float high)
    {
        return Math.Max(low,Math.Min(high,value));
    }
    public static float SpawnY(float requested,float low,float high)
    {
        // Placement occurs once, while the spawn visual is hidden. A requested
        // lane already inside the safe interval is preserved (e.g. a companion).
        return requested>=low && requested<=high?requested:(low+high)*.5f;
    }
    public float Step(float y,float low,float high,bool exploring,float moveDt,float holdDt,
        float amplitude,float speed,float holdSeconds)
    {
        if(high<low)high=low;
        moveDt=Positive(moveDt);holdDt=Positive(holdDt);
        holdSeconds=Positive(holdSeconds);
        amplitude=Math.Min(Positive(amplitude),(high-low)*.5f);
        if(!exploring)
        {
            // Stable firing position. Boundary recovery takes priority only
            // when the caller actually starts outside the permitted foot lanes.
            TargetY=Clamp(y,low,high);HasTarget=true;HoldRemaining=holdSeconds;
        }
        else if(!HasTarget)
        {
            Choose(low,high,amplitude,holdSeconds);
        }
        else
        {
            TargetY=Clamp(TargetY,low,high);
            // Hold begins at arrival, not while still walking to the lane.
            if(Math.Abs(y-TargetY)<.001f && moveDt>0f)
            {
                HoldRemaining=Math.Max(0f,HoldRemaining-holdDt);
                if(HoldRemaining<=0f)Choose(low,high,amplitude,holdSeconds);
            }
        }
        float step=Positive(speed)*moveDt;
        return y+Math.Sign(TargetY-y)*Math.Min(Math.Abs(TargetY-y),step);
    }
    private void Choose(float low,float high,float amplitude,float holdSeconds)
    {
        TargetY=Clamp((low+high)*.5f+direction*amplitude,low,high);
        direction=-direction;HasTarget=true;HoldRemaining=holdSeconds;
    }
}
