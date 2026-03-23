using Fusion;

namespace Crazy_Lobby.Item
{
    public abstract class Items : NetworkBehaviour
    {
        public float Timer {get; protected set;}
        public string Description {get; protected set;}
        public string Ability {get; protected set;}
        public Items(string _ability, float _timer, string _des)
        {
            Timer = _timer;
            Ability = _ability;
            Description = _des;
        }
        public abstract void Use(PlayerController player);
    }
}