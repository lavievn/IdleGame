using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace TuTienCore
{
    public enum MartialDefinitionKind { CultivationMethod, MartialArt }
    public enum MartialEffectMode { Passive, Active }
    public enum MartialRootAffinityPolicy { None, EligibilityOnly, WeightedCounterOnce }
    public enum MartialRegistryReason
    { None, MissingDefinitions, InvalidDefinition, InvalidId, InvalidRevision, DuplicateId, InvalidWeaponPolicy, InvalidRootPolicy, InvalidUnlockCondition }
    public enum MartialEligibilityReason { None, UnknownDefinition, MissingContext, InvalidContext, MissingWeapon, WeaponMismatch, RootRequirementNotMet, UnlockRequired }

    public sealed class MartialRootRequirement
    {
        public ElementType Root {get;private set;}
        public int MinimumTier {get;private set;}
        public MartialRootRequirement(ElementType root,int minimumTier) {Root=root;MinimumTier=minimumTier;}
    }
    public sealed class MartialDefinition
    {
        public string Id {get;private set;}
        public int Revision {get;private set;}
        public MartialDefinitionKind Kind {get;private set;}
        public MartialEffectMode EffectMode {get;private set;}
        public MartialRootAffinityPolicy RootPolicy {get;private set;}
        public ReadOnlyCollection<WeaponFamily> AllowedWeapons {get;private set;}
        public ReadOnlyCollection<MartialRootRequirement> RequiredRoots {get;private set;}
        public ReadOnlyCollection<string> RequiredUnlocks {get;private set;}
        internal bool HasWeaponPolicy {get;private set;}
        // An explicit empty weapon list means unrestricted. Null is a missing policy.
        // Required roots and unlock facts are conjunctions, not implicit OR conditions.
        public MartialDefinition(string id,int revision,MartialDefinitionKind kind,MartialEffectMode mode,
            IEnumerable<WeaponFamily> allowedWeapons,MartialRootAffinityPolicy rootPolicy=MartialRootAffinityPolicy.None,
            IEnumerable<MartialRootRequirement> requiredRoots=null,IEnumerable<string> requiredUnlocks=null)
        {
            Id=id;Revision=revision;Kind=kind;EffectMode=mode;RootPolicy=rootPolicy;HasWeaponPolicy=allowedWeapons!=null;
            AllowedWeapons=new List<WeaponFamily>(allowedWeapons??new WeaponFamily[0]).AsReadOnly();
            RequiredRoots=new List<MartialRootRequirement>(requiredRoots??new MartialRootRequirement[0]).AsReadOnly();
            RequiredUnlocks=new List<string>(requiredUnlocks??new string[0]).AsReadOnly();
        }
    }
    public sealed class MartialEligibilityContext
    {
        public WeaponFamily? Weapon {get;private set;}
        internal bool Valid {get;private set;}
        readonly Dictionary<ElementType,int> roots=new Dictionary<ElementType,int>();
        readonly HashSet<string> unlocks=new HashSet<string>(StringComparer.Ordinal);
        // Null weapon means missing information; Unarmed is an explicit family.
        // No EntityDataSO, class, profession, learned state or mutable live root list.
        public MartialEligibilityContext(WeaponFamily? weapon,IEnumerable<MartialRootRequirement> rootTiers=null,IEnumerable<string> unlockedFacts=null)
        {
            Weapon=weapon;Valid=!weapon.HasValue || Enum.IsDefined(typeof(WeaponFamily),weapon.Value);
            foreach(var root in rootTiers??new MartialRootRequirement[0]) {
                if(root==null || !Enum.IsDefined(typeof(ElementType),root.Root) || root.MinimumTier<1 || root.MinimumTier>5 || roots.ContainsKey(root.Root)) {Valid=false;continue;}
                roots.Add(root.Root,root.MinimumTier);
            }
            foreach(string fact in unlockedFacts??new string[0]) {
                if(!MartialArtsRegistry.ValidId(fact))Valid=false;
                else unlocks.Add(fact);
            }
        }
        internal bool HasRoot(MartialRootRequirement root) {int tier;return roots.TryGetValue(root.Root,out tier) && tier>=root.MinimumTier;}
        internal bool HasUnlock(string id) {return unlocks.Contains(id);}
    }
    public sealed class MartialRegistryValidation
    {
        public MartialRegistryReason Reason {get;private set;}
        public string DefinitionId {get;private set;}
        internal MartialRegistryValidation(MartialRegistryReason reason,string id) {Reason=reason;DefinitionId=id;}
    }
    public sealed class MartialEligibilityResult
    {
        public MartialDefinition Definition {get;private set;}
        public MartialEligibilityReason Reason {get;private set;}
        public bool IsEligible {get{return Reason==MartialEligibilityReason.None;}}
        public bool IsPassive {get{return Definition!=null && Definition.EffectMode==MartialEffectMode.Passive;}}
        internal MartialEligibilityResult(MartialDefinition definition,MartialEligibilityReason reason) {Definition=definition;Reason=reason;}
    }
    // Registry of supplied definitions only: no static catalog, effects or startup hooks.
    public sealed class MartialArtsRegistry
    {
        readonly Dictionary<string,MartialDefinition> definitions=new Dictionary<string,MartialDefinition>(StringComparer.Ordinal);
        private MartialArtsRegistry() {}
        internal static bool ValidId(string id) {return id!=null && Regex.IsMatch(id,@"\A[a-z][a-z0-9_.-]*:[a-z0-9_.:-]+\z");}
        static MartialRegistryReason Validate(MartialDefinition d)
        {
            if(d==null || !Enum.IsDefined(typeof(MartialDefinitionKind),d.Kind) || !Enum.IsDefined(typeof(MartialEffectMode),d.EffectMode))return MartialRegistryReason.InvalidDefinition;
            if(!ValidId(d.Id))return MartialRegistryReason.InvalidId;
            if(d.Revision<1)return MartialRegistryReason.InvalidRevision;
            var weapons=new HashSet<WeaponFamily>();
            if(!d.HasWeaponPolicy)return MartialRegistryReason.InvalidWeaponPolicy;
            foreach(var family in d.AllowedWeapons)
                if(!Enum.IsDefined(typeof(WeaponFamily),family) || !weapons.Add(family))return MartialRegistryReason.InvalidWeaponPolicy;
            if(!Enum.IsDefined(typeof(MartialRootAffinityPolicy),d.RootPolicy) || (d.RootPolicy==MartialRootAffinityPolicy.None && d.RequiredRoots.Count>0))return MartialRegistryReason.InvalidRootPolicy;
            var roots=new HashSet<ElementType>();
            foreach(var root in d.RequiredRoots)
                if(root==null || !Enum.IsDefined(typeof(ElementType),root.Root) || root.MinimumTier<1 || root.MinimumTier>5 || !roots.Add(root.Root))return MartialRegistryReason.InvalidRootPolicy;
            var unlocks=new HashSet<string>(StringComparer.Ordinal);
            foreach(string id in d.RequiredUnlocks)if(!ValidId(id) || !unlocks.Add(id))return MartialRegistryReason.InvalidUnlockCondition;
            return MartialRegistryReason.None;
        }
        public static bool TryCreate(IEnumerable<MartialDefinition> source,out MartialArtsRegistry registry,out MartialRegistryValidation validation)
        {
            registry=null;validation=new MartialRegistryValidation(MartialRegistryReason.None,null);
            if(source==null) {validation=new MartialRegistryValidation(MartialRegistryReason.MissingDefinitions,null);return false;}
            var candidate=new MartialArtsRegistry();
            foreach(var definition in source) {
                var reason=Validate(definition);
                if(reason==MartialRegistryReason.None && candidate.definitions.ContainsKey(definition.Id))reason=MartialRegistryReason.DuplicateId;
                if(reason!=MartialRegistryReason.None) {validation=new MartialRegistryValidation(reason,definition==null?null:definition.Id);return false;}
                candidate.definitions.Add(definition.Id,definition);
            }
            registry=candidate;return true;
        }
        public bool TryGet(string id,out MartialDefinition definition)
        {definition=null;return id!=null && definitions.TryGetValue(id,out definition);}
        public MartialEligibilityResult Evaluate(string id,MartialEligibilityContext context)
        {
            MartialDefinition d;if(!TryGet(id,out d))return new MartialEligibilityResult(null,MartialEligibilityReason.UnknownDefinition);
            var reason=MartialEligibilityReason.None;
            if(context==null)reason=MartialEligibilityReason.MissingContext;
            else if(!context.Valid)reason=MartialEligibilityReason.InvalidContext;
            else if(d.AllowedWeapons.Count>0 && !context.Weapon.HasValue)reason=MartialEligibilityReason.MissingWeapon;
            else if(d.AllowedWeapons.Count>0 && !d.AllowedWeapons.Contains(context.Weapon.Value))reason=MartialEligibilityReason.WeaponMismatch;
            else {
                foreach(var root in d.RequiredRoots)if(!context.HasRoot(root)) {reason=MartialEligibilityReason.RootRequirementNotMet;break;}
                if(reason==MartialEligibilityReason.None)
                    foreach(string fact in d.RequiredUnlocks)if(!context.HasUnlock(fact)) {reason=MartialEligibilityReason.UnlockRequired;break;}
            }
            return new MartialEligibilityResult(d,reason);
        }
    }
}
