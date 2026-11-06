using System;
using System.Collections.Generic;

namespace TuTienCore
{
    // Domain payload; v0.7 stores its JSON in the canonical Hero only for explicit isolated playtest.
    // configurationVersion=0 remains v0.6 domain-fixture-only, never accepted by the gameplay adapter.
    [Serializable]
    public sealed class CultivationComponentDto
    {
        public int version;public int configurationVersion;public string configurationId,ownerId,actualRealmId;
        public double foundation,unrefinedQi,spiritualEnergy,injury,bloodProgress;public int bloodCharges;
        public bool hasBurn;public string burnId,burnOwnerId,burnActualRealmId;
        public double burnReference,burnDebtRatio,burnRemaining;public int burnChargesUsed,burnStatus,burnEndReason=-1;
        public List<string> settledTransactions=new List<string>();
    }
    public static class CultivationComponentCodec
    {
        public const int ProposedVersion=1;
        public static CultivationComponentDto Capture(CultivationSession session,string configurationId)
        {
            if(session==null || string.IsNullOrWhiteSpace(configurationId))throw new ArgumentException("Owned session and configuration ID required.");
            var s=session.Current;var d=new CultivationComponentDto {version=ProposedVersion,configurationId=configurationId,ownerId=session.OwnerId.ToString("N"),
                actualRealmId=s.Foundation.ActualRealmId,foundation=s.Foundation.Amount,unrefinedQi=s.Reserve.Amount,spiritualEnergy=s.Energy.Current,injury=s.Injury.Severity,
                bloodCharges=s.Blood.Charges,bloodProgress=s.Blood.AccumulationProgress};
            foreach(Guid id in s.SettledTransactions)d.settledTransactions.Add(id.ToString("N"));
            var b=s.Burning;if(b!=null) {
                d.hasBurn=true;d.burnId=b.Id.ToString("N");d.burnOwnerId=b.OwnerId.ToString("N");d.burnActualRealmId=b.ActualRealmId;
                d.burnReference=b.ReferenceFoundation;d.burnDebtRatio=b.DebtRatio;d.burnChargesUsed=b.ChargesUsed;d.burnRemaining=b.RemainingSeconds;
                d.burnStatus=(int)b.Status;d.burnEndReason=b.EndReason.HasValue?(int)b.EndReason.Value:-1;
            }
            return d;
        }
    }
    public sealed class CultivationCandidateSlot
    {
        public CultivationSession Current {get;private set;}
        public CultivationCandidateSlot(CultivationSession current) {Current=current;}
        public CultivationReason TryLoad(CultivationComponentDto dto,CultivationRealmEvaluator catalog,CultivationRules rules,string expectedConfiguration,
            Guid expectedOwner,Guid freshEpoch,BurningEndReason recoveryReason=BurningEndReason.Load)
        {
            if(dto==null || dto.version==0)return CultivationReason.LegacyMigrationRequired;
            if(dto.version!=CultivationComponentCodec.ProposedVersion)return CultivationReason.UnsupportedVersion;
            if(string.IsNullOrWhiteSpace(expectedConfiguration) || dto.configurationId!=expectedConfiguration || catalog==null || rules==null)return CultivationReason.MissingPolicy;
            Guid owner;if(!Guid.TryParseExact(dto.ownerId,"N",out owner) || owner==Guid.Empty || owner!=expectedOwner || freshEpoch==Guid.Empty ||
                (Current!=null && freshEpoch==Current.Epoch))return CultivationReason.OwnerMismatch;
            if(recoveryReason!=BurningEndReason.Load && recoveryReason!=BurningEndReason.CrashRecovery)return CultivationReason.InvalidValue;
            try
            {
                var settled=new List<Guid>();if(dto.settledTransactions==null)return CultivationReason.InvalidSnapshot;
                foreach(string text in dto.settledTransactions) {Guid id;if(!Guid.TryParseExact(text,"N",out id) || id==Guid.Empty)return CultivationReason.InvalidSnapshot;settled.Add(id);}
                BurningTransaction burn=null;
                if(dto.hasBurn) {
                    Guid id,burnOwner;if(!Guid.TryParseExact(dto.burnId,"N",out id) || !Guid.TryParseExact(dto.burnOwnerId,"N",out burnOwner))return CultivationReason.InvalidSnapshot;
                    if(!Enum.IsDefined(typeof(BurningStatus),dto.burnStatus) || (dto.burnEndReason!=-1 && !Enum.IsDefined(typeof(BurningEndReason),dto.burnEndReason)))return CultivationReason.InvalidSnapshot;
                    burn=new BurningTransaction(id,burnOwner,dto.burnReference,dto.burnDebtRatio,dto.burnChargesUsed,dto.burnRemaining,
                        (BurningStatus)dto.burnStatus,dto.burnActualRealmId,dto.burnEndReason==-1?(BurningEndReason?)null:(BurningEndReason)dto.burnEndReason);
                } else if(!string.IsNullOrEmpty(dto.burnId) || !string.IsNullOrEmpty(dto.burnOwnerId) || !string.IsNullOrEmpty(dto.burnActualRealmId) ||
                    dto.burnReference!=0 || dto.burnDebtRatio!=0 || dto.burnChargesUsed!=0 || dto.burnRemaining!=0 || dto.burnStatus!=0 || dto.burnEndReason!=-1)
                    return CultivationReason.InvalidSnapshot; // cannot hide outstanding liability behind hasBurn=false
                var snapshot=new CultivationSnapshot(new CultivationFoundation(dto.actualRealmId,dto.foundation),new UnrefinedQiReserve(dto.unrefinedQi),new SpiritualEnergy(dto.spiritualEnergy),
                    new BloodEssenceState(dto.bloodCharges,dto.bloodProgress),new FoundationInjuryState(dto.injury),burn,settled);
                var candidate=new CultivationSession(owner,freshEpoch,catalog,rules,snapshot);
                if(burn!=null && burn.Status==BurningStatus.Active) {
                    var result=candidate.EndBlood(owner,freshEpoch,burn.Id,recoveryReason);if(result!=CultivationReason.None)return result;
                }
                // No offline tick, healing, resource grant or implicit settlement on Load.
                Current=candidate;return CultivationReason.None;
            }
            catch(ArgumentException) {return CultivationReason.InvalidSnapshot;}
        }
    }
}
