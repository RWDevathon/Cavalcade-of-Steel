using RimWorld;

namespace ArtificialBeings
{
    [DefOf]
    public static class ABF_CavalcadeDefOf
    {
        static ABF_CavalcadeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ABF_CavalcadeDefOf));
        }

        public static FactionDef ABF_Faction_Synstruct_Cavalcade;

        public static HistoryEventDef ABF_HistoryEvent_Synstruct_CavalcadeIgnoredUltimatum;
    }
}
