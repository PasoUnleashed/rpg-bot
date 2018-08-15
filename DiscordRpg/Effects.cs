using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRpg
{
    public class ItemBehaviour
    {
        Effect[] effects;
        int id;

        public ItemBehaviour(Effect[] effects, int id)
        {
            this.effects = effects;
            this.id = id;
        }

        public Effect[] Effects { get => effects; set => effects = value; }
        public int Id { get => id; set => id = value; }
    }

    public abstract class Effect
    {

        int id;

        public Effect(int id)
        {
            this.id = id;
        }

        public int Id { get => id; set => id = value; }
        public abstract void Apply(Player p);
        
    }
    public class CombatEffect : Effect
    {
        int amount, totalTicks, totalTime;

        public CombatEffect(int amount, int totalTicks, int totalTime,int id) : base(id)
        {
            this.amount = amount;
            this.totalTicks = totalTicks;
            this.totalTime = totalTime;

        }

        public int Amount { get => amount; set => amount = value; }
        public int TotalTicks { get => totalTicks; set => totalTicks = value; }
        public int TotalTime { get => totalTime; set => totalTime = value; }

        public override void Apply(Player p)
        {
            Console.WriteLine("Damage effect applied to "+p.User.Username+" for "+amount);
            p.Health += amount;
        }
    }
    public class VoiceEntryEffect : Effect
    {
        string mp3;

        public VoiceEntryEffect(string mp3,int id) : base(id)
        {
        }

        public string Mp3 { get => mp3; set => mp3 = value; }

        public override void Apply(Player p)
        {
            throw new NotImplementedException();
        }
    }
    public class ScriptEffect : Effect
    {
        int scriptid;

        public ScriptEffect(int scriptid,int id) : base(id)
        {
        }

        public int Scriptid { get => scriptid; set => scriptid = value; }

        public override void Apply(Player p)
        {
            throw new NotImplementedException();
        }
    }
    public class EffectInstance
    {
        ulong id;
        DateTime applied;
        string state;
        ESMachine machine;
        Effect type;
        Player caster;
        Player target;
    }
}
    