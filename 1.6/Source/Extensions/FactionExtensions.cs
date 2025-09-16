using Verse;

namespace ArtificialBeings
{
    // ModExtension allowing for xml-based control over how the Cavalcade faction reacts to organics.
    // Only checked for the Cavalcade faction def, and only for the first found instance of it.
    public class ABF_CavalcadeFactionExtension : DefModExtension
    {
        // Determines how much faction relations is lost per day, per pawn, when the player possesses organics of the corresponding type.
        // If set to 0, then the faction will consider them tolerable.
        public int reputationalDamageForPrisonerPerDay;
        public int reputationalDamageForSlavePerDay;
        public int reputationalDamageForColonistPerDay;
        public int reputationalDamageForAnimalPerDay;

        // Hours before reputational damage will begin to accrue for having an intolerable organic in the player faction.
        public int hoursBeforeReputationalDamageBegins;

        // Hours before patience is lost and the faction declares war on the player for having intolerable organics.
        public int hoursBeforeWarDeclaration;
    }
}
