using AlienRace;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ArtificialBeings
{
    // This implementation is not perfect, as it scans all player pawns every long tick. A more ideal solution would involve only checking when pawns are added/removed from the player faction.
    // In favor of choosing a workable, decent solution over striving for a perfect one, this will do for now.
    public class WorldComponent_Cavalcade : WorldComponent
    {
        public bool currentlyPermanentlyHostile = false;

        public int hoursSinceIntolerableOrganicDetected = 0;

        private bool postInit = false;

        private ABF_CavalcadeFactionExtension factionExtension;

        private Faction cavalcadeFaction;

        public Faction CavalcadeFaction
        {
            get
            {
                if (cavalcadeFaction == null)
                {
                    cavalcadeFaction = world.factionManager.FirstFactionOfDef(ABF_CavalcadeDefOf.ABF_Faction_Synstruct_Cavalcade);
                    factionExtension = cavalcadeFaction.def.GetModExtension<ABF_CavalcadeFactionExtension>();
                }
                return cavalcadeFaction;
            }
        }

        public WorldComponent_Cavalcade(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            // If the game was loaded, the permanent enemy on the faction def needs to be reset, but can't be done from FinalizeInit (Pawns not loaded yet).
            if (postInit)
            {
                ScanForOrganics();
                postInit = false;
            }
            // Only scan once every in-game hour. Do nothing if the faction is defeated or cannot be found.
            if (GenTicks.TicksGame % GenDate.TicksPerHour == 0 && CavalcadeFaction != null && !CavalcadeFaction.defeated)
            {
                ScanForOrganics();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hoursSinceIntolerableOrganicDetected, "ABF_CavalcadeHoursSinceIntolerableOrganicDetected", 0);
            Scribe_Values.Look(ref currentlyPermanentlyHostile, "ABF_CavalcadeCurrentlyPermanentlyHostile", false);
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            // If generating the world for the first time, mark the faction as permanently hostile but make sure the def is not.
            // Player pawns haven't generated yet. Once the first scan is complete, it'll be correct.
            if (!fromLoad)
            {
                CavalcadeFaction.def.permanentEnemy = false;
                currentlyPermanentlyHostile = true;
            }
            postInit = true;
        }

        private void ScanForOrganics()
        {
            // If the faction is currently permanently hostile, then it will only become not hostile if there are no organics remaining. Make sure the def is still set correctly.
            if (currentlyPermanentlyHostile)
            {
                foreach (Pawn pawn in world.PlayerPawnsForStoryteller)
                {
                    if (!ABF_Utils.IsArtificial(pawn) && pawn.RaceProps.IsFlesh && !(pawn.def is ThingDef_AlienRace alienRace && !alienRace.alienRace.compatibility.IsFlesh))
                    {
                        CavalcadeFaction.def.permanentEnemy = true;
                        currentlyPermanentlyHostile = true;
                        return;
                    }
                }
                // No organic pawns found. The faction will consider peace an option, but will not take the first move.
                CavalcadeFaction.def.permanentEnemy = false;
                currentlyPermanentlyHostile = false;
                hoursSinceIntolerableOrganicDetected = 0;
            }
            else if (!postInit)
            {

                // Reputational damage. Only matters if the faction's approval is not already at -100. Only applies once per day.
                // If the number is greater than zero, an intolerable organic of any kind was found.
                int reputationDamage = 0;
                foreach (Pawn pawn in world.PlayerPawnsForStoryteller)
                {
                    if (!ABF_Utils.IsArtificial(pawn) && pawn.RaceProps.IsFlesh && !(pawn.def is ThingDef_AlienRace alienRace && !alienRace.alienRace.compatibility.IsFlesh))
                    {
                        if (pawn.IsFreeColonist)
                        {
                            reputationDamage += factionExtension.reputationalDamageForColonistPerDay;
                        }
                        else if (pawn.IsSlaveOfColony)
                        {
                            reputationDamage += factionExtension.reputationalDamageForSlavePerDay;
                        }
                        else if (pawn.IsPrisonerOfColony)
                        {
                            reputationDamage += factionExtension.reputationalDamageForPrisonerPerDay;
                        }
                        else if (pawn.IsAnimal)
                        {
                            reputationDamage += factionExtension.reputationalDamageForAnimalPerDay;
                        }
                        else
                        {
                            Log.Warning($"[ABF:CoS] {pawn.LabelShortCap} is not of an expected type and is not being accounted for in cavalcade reputation checking!");
                        }
                    }
                }

                if (reputationDamage == 0)
                {
                    hoursSinceIntolerableOrganicDetected = 0;
                    return;
                }
                // If an intolerable organic was newly found, send a warning letter to the player.
                else if (hoursSinceIntolerableOrganicDetected == 0)
                {
                    Find.LetterStack.ReceiveLetter("ABF_CavalcadeUltimatum".Translate(), "ABF_CavalcadeUltimatumDesc".Translate(factionExtension.hoursBeforeWarDeclaration, CavalcadeFaction.NameColored), LetterDefOf.NeutralEvent);
                }
                hoursSinceIntolerableOrganicDetected++;

                // Reputation damage only occurs if there is reputation to lose and only once per day.
                if (CavalcadeFaction.GoodwillWith(Faction.OfPlayer) != -100 && reputationDamage > 0 && hoursSinceIntolerableOrganicDetected > 0 && hoursSinceIntolerableOrganicDetected % 24 == 0)
                {
                    CavalcadeFaction.TryAffectGoodwillWith(Faction.OfPlayer, -reputationDamage, canSendMessage: true, canSendHostilityLetter: true, ABF_CavalcadeDefOf.ABF_HistoryEvent_Synstruct_CavalcadeIgnoredUltimatum);
                }

                // Time is up and the player isn't allied. The faction will declare war and become "permanently" hostile until all intolerable organics are gone.
                if (CavalcadeFaction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Ally && hoursSinceIntolerableOrganicDetected >= factionExtension.hoursBeforeWarDeclaration && reputationDamage > 0)
                {
                    CavalcadeFaction.TryAffectGoodwillWith(Faction.OfPlayer, -200, canSendMessage: true, canSendHostilityLetter: false, ABF_CavalcadeDefOf.ABF_HistoryEvent_Synstruct_CavalcadeIgnoredUltimatum);
                    Find.LetterStack.ReceiveLetter("ABF_CavalcadeDeclarationOfWar".Translate(), "ABF_CavalcadeDeclarationOfWarDesc".Translate(CavalcadeFaction.NameColored), LetterDefOf.NegativeEvent);
                    CavalcadeFaction.def.permanentEnemy = true;
                    currentlyPermanentlyHostile = true;
                }
            }
        }
    }
}
