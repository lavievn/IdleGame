using System;
namespace TuTienCore
{
    // Pure validated conversion; filesystem commit stays in the existing migration transaction.
    public static class CultivationConfigurationUpgrade
    {
        public static bool TryConvert(string component,CultivationPlaytestProfile source,CultivationPlaytestProfile target,out string converted,out string reason)
        {
            converted=null;reason="Supported forward production upgrade required.";
            if(source==null || target==null || !source.IsProduction || !target.IsProduction || target.SaveVersion!=3 || target.Version<=source.Version || CultivationProductionConfiguration.SupportedVersion(source.Id,source.Version)==0 || CultivationProductionConfiguration.SupportedVersion(target.Id,target.Version)==0)return false;
            CultivationSession current;if(!CultivationGameplayState.TryRestore(component,source,out current,out reason))return false;
            if(current==null || current.Current.Burning!=null || current.Current.Blood.Charges!=0 || current.Current.Blood.AccumulationProgress!=0 || current.Current.SettledTransactions.Count!=0){reason="Unsupported Blood/Burning liability; never discard it during upgrade.";return false;}
            try{
                var candidate=new CultivationSession(current.OwnerId,Guid.NewGuid(),target.Catalog,target.Rules,current.Current);
                var clock=current.GameplayClock??new CultivationClockState();
                candidate.GameplayClock=new CultivationClockState(clock.Remainder,clock.Elapsed,clock.NodeId,clock.Remainder>0?(clock.ConfigurationId??source.Id):target.Id,clock.Remainder>0?(clock.ConfigurationId==null?source.Version:clock.ConfigurationVersion):target.Version);
                converted=CultivationGameplayState.Capture(candidate,target);CultivationSession verify;if(!CultivationGameplayState.TryRestore(converted,target,out verify,out reason))return false;
                var a=current.Current;var b=verify.Current;
                if(current.OwnerId!=verify.OwnerId || a.Foundation.ActualRealmId!=b.Foundation.ActualRealmId || a.Foundation.Amount!=b.Foundation.Amount || a.Reserve.Amount!=b.Reserve.Amount || a.Energy.Current!=b.Energy.Current || a.Injury.Severity!=b.Injury.Severity || clock.Remainder!=verify.GameplayClock.Remainder || clock.Elapsed!=verify.GameplayClock.Elapsed || clock.NodeId!=verify.GameplayClock.NodeId){reason="Upgrade conservation mismatch.";converted=null;return false;}
                return true;
            }catch(ArgumentException e){reason=e.Message;return false;}
        }
    }
}
