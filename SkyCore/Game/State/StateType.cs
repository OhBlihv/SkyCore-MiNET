using SkyCore.Util;

namespace SkyCore.Game.State
{
    
    public class StateType : Enumeration
    {

        public static readonly StateType Empty           = new StateType(0);  //Pre-Pre Game. No players. (Low Priority)
        public static readonly StateType PreGame         = new StateType(1);  //Pre-Game. Players waiting in Lobby
        public static readonly StateType PreGameStarting = new StateType(2);  //Pre-Game. Full game - countdown in effect
        public static readonly StateType Running         = new StateType(3);  //Main Game
        public static readonly StateType EndGame         = new StateType(4);  //Game Ending -- No spectators can join   
        public static readonly StateType Closing         = new StateType(5);  //Game Shutting Down.

        private const int StateCount = 5;

        //
        
        public int Ordinal { get; private set; }
        
        private StateType(int ordinal)
        {
            Ordinal = ordinal;
        }

        public bool IsStart()
        {
            return Ordinal < 3;
        }

        public bool IsEnd()
        {
            return Ordinal == (StateCount - 1);
        }

        public bool IsJoinable()
        {
            return Ordinal < 2; //Do not include PreGameStarting, since this indicates a full lobby
        }

    }
    
}
