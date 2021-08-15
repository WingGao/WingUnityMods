using NEON.Game.PowerUps;

namespace NEON.Game.Managers
{
    public class patch_NEONPlayerState : NEONPlayerState
    {
        public extern void orig_AddBeliefByTypeAndHandlingFullBelief(BeliefType Type, int BeliefValue);

        public void AddBeliefByTypeAndHandlingFullBelief(BeliefType Type = BeliefType.Athene, int BeliefValue = 1)
        {
            BeliefValue *= 10; //放大10倍
            orig_AddBeliefByTypeAndHandlingFullBelief(Type, BeliefValue);
        }
    }
}