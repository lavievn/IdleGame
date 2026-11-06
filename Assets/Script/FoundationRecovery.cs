using System;
using System.Collections.Generic;

namespace TuTienCore
{
    public sealed partial class CultivationSession
    {
        readonly HashSet<string> treatmentSteps=new HashSet<string>(StringComparer.Ordinal);
        internal CultivationReason HealFromTreatment(Guid owner,Guid epoch,Guid treatment,long step,double amount,string source)
        {
            var reason=Check(owner,epoch);if(reason!=CultivationReason.None)return reason;
            string key=treatment.ToString("N")+":"+step;
            if(treatmentSteps.Contains(key))return CultivationReason.DuplicateCommand;
            reason=HealInjury(owner,epoch,Guid.NewGuid(),amount,source);
            if(reason==CultivationReason.None)treatmentSteps.Add(key);return reason;
        }
    }
    public enum InjuryTreatmentState { Active, Completed, Cancelled }
    public enum InjuryTreatmentSource { PassiveNurturing, ExplicitExternalTreatment }
    public sealed class InjuryTreatmentSession
    {
        public readonly Guid Id,OwnerId,Epoch;public readonly InjuryTreatmentSource Source;
        public InjuryTreatmentState State {get;private set;}public double RemainingBudget {get;private set;}
        readonly double rate,maximumStep;long lastStep=-1;
        public InjuryTreatmentSession(Guid id,Guid owner,Guid epoch,InjuryTreatmentSource source,double healingRate,double budget,double maximumStep)
        {
            if(id==Guid.Empty || owner==Guid.Empty || epoch==Guid.Empty || !Enum.IsDefined(typeof(InjuryTreatmentSource),source) || !CultivationNumber.NonNegative(healingRate) || !CultivationNumber.NonNegative(budget) || !CultivationNumber.NonNegative(maximumStep) || maximumStep<=0)throw new ArgumentException("Explicit valid treatment required.");
            Id=id;OwnerId=owner;Epoch=epoch;Source=source;rate=healingRate;RemainingBudget=budget;this.maximumStep=maximumStep;
        }
        public CultivationReason Tick(CultivationSession session,long step,double dt,bool paused)
        {
            if(session==null || session.OwnerId!=OwnerId || session.Epoch!=Epoch)return CultivationReason.OwnerMismatch;
            if(State!=InjuryTreatmentState.Active)return CultivationReason.InvalidSession;if(paused)return CultivationReason.Paused;
            if(step<=lastStep || step<0)return CultivationReason.DuplicateCommand;
            if(!CultivationNumber.NonNegative(dt) || dt>maximumStep)return CultivationReason.InvalidDelta;
            double desired=rate*dt;if(!CultivationNumber.NonNegative(desired))return CultivationReason.Overflow;
            double heal=Math.Min(session.Current.Injury.Severity,Math.Min(RemainingBudget,desired));
            // Session owner/epoch prevents a stale treatment from reaching a loaded Hero.
            var result=session.HealFromTreatment(OwnerId,Epoch,Id,step,heal,Source+":"+Id);
            if(result!=CultivationReason.None)return result;
            RemainingBudget-=heal;lastStep=step;if(RemainingBudget==0 || session.Current.Injury.Severity==0)State=InjuryTreatmentState.Completed;
            return CultivationReason.None;
        }
        public void Cancel() {if(State==InjuryTreatmentState.Active)State=InjuryTreatmentState.Cancelled;}
    }
}
