using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

public partial class GameManager
{
    SaveManager saveManager;
    public EntityDataSO heroDataSO;
    public bool HasPendingConfirmation;
    public readonly List<string> Log=new List<string>();
    public int PublishedLoads;
    internal SaveManager SavesFixture=>saveManager;
    internal void BindPersistenceFixture(EntityDataSO hero,SaveManager saves){BindFixture(hero);saveManager=saves;}
    void UpdateEventLog(string message){Log.Add(message);}
    bool PrepareCultivation(EntityDataSO h,out CultivationGameplayBinding binding)
    {
        binding=null;
        if(!CultivationGameplayState.TryRestore(h.cultivationJson,saveManager.CultivationProfile,out var state,out _))return false;
        if(state!=null)binding=new CultivationGameplayBinding(h,saveManager.CultivationProfile,state);return true;
    }
    bool ApplyLoadedCandidate(EntityDataSO h,SaveSlot slot,bool deploy,CultivationGameplayBinding prepared)
    {BindFixture(h);PublishedLoads++;prepared?.Dispose();return true;}
    void RequestCultivationMigration(SaveSlot slot,bool deploy,bool fromTitle)
    {throw new InvalidOperationException("Migration I/O is outside this fixture");}
    internal bool LoadFixture(SaveSlot slot)=>TryLoadSlot(slot,true);
}
