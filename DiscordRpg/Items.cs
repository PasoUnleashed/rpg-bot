using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRpg
{

    public class Item
    {
        int id;
        string name;

        int minTargetCount, maxTargetCount;
        int cooldown;
        ItemBehaviour behaviour;
        public Item(int id, string name, int minTargetCount, int maxTargetCount, int cooldown, ItemBehaviour behaviour)
        {
            this.id = id;
            this.name = name;
            this.minTargetCount = minTargetCount;
            this.maxTargetCount = maxTargetCount;
            this.cooldown = cooldown;
            this.behaviour = behaviour;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public int MinTargetCount { get => minTargetCount; set => minTargetCount = value; }
        public int MaxTargetCount { get => maxTargetCount; set => maxTargetCount = value; }
        public int Cooldown { get => cooldown; set => cooldown = value; }
        public ItemBehaviour Behaviour { get => behaviour; set => behaviour = value; }
        
    }

    public class ItemInstance
    {
        Item item;
        ulong id;
        string state;
        Player owner;
        DateTime lastused;

        public ItemInstance(Item item, ulong id, Player owner)
        {
            this.item = item;
            lastused = DateTime.MinValue;
            this.id = id;
            this.owner = owner;
        }

        public ulong Id { get => id; set => id = value; }
        public string State { get => state; set => state = value; }
        public Player Owner { get => owner; set => owner = value; }
        public DateTime Lastused { get => lastused; set => lastused = value; }
        public Item Item { get => item; set => item = value; }
        public bool TryApply(List<Player> targets)
        {
            if(lastused!=null && DateTime.Now.Subtract(lastused).TotalSeconds > item.Cooldown)
            {
                Console.WriteLine("Applying");
                foreach(var p in targets)
                {
                    foreach(var e in item.Behaviour.Effects)
                    {
                        Console.WriteLine("Applied.");
                        e.Apply(p);
                    }
                }
                lastused = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
