using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordRpg
{
    public abstract class InteractionNode
    {
        private int next;
        protected InteractionTree tree;
        private int iD;
        protected bool requiresInput = false;
        public int ID { get => iD; set => iD = value; }
        public int Next { get => next; set => next = value; }
        public InteractionTree Tree { get => tree; set => tree = value; }
        

        public abstract string GetEntry(InteractionInstance i,Player p);
        public InteractionNode(int id, int next,InteractionTree tree)
        {
            ID = id;
            this.next = next;
            this.tree = tree;
        }
        public virtual bool RequiresInput(InteractionInstance i)
        {
            return requiresInput;
        }
        public virtual InteractionNode GetNext(InteractionInstance i,IMessage input)
        {
            if(next == -1)
            {
                return new EndNode(0,0,null);
            }
            else
            {
                return tree.Nodes[next];
            }
        }
    }
    public class ConversationNode : InteractionNode
    {
        string[] options;
        int[] nexts;
        string entry;
        public ConversationNode(string entry,string[] options, int[] nexts,int id, InteractionTree tree) : base(id, -1, tree)
        {
            this.options = options;
            this.nexts = nexts;
            this.entry = entry;
            this.requiresInput = true;

        }

        public string[] Options { get => options; set => options = value; }
        public int[] Nexts { get => nexts; set => nexts = value; }

        public override string GetEntry(InteractionInstance i, Player p)
        {
            return entry + "\n" + OptionsToString() ;
        }
        public override InteractionNode GetNext(InteractionInstance i, IMessage input)
        {
            if (!checkNumber(input.Content))
            {
                input.Channel.SendMessageAsync("Sorry that's not a valid choice");
                return this;
            }
            else
            {
                var num = int.Parse(input.Content);
                if (num > nexts.Length||num<1)
                {
                    input.Channel.SendMessageAsync("Sorry that's not a valid choice");
                    return this;
                }
                else
                {
                    if(nexts[num-1] == -1)
                    {
                        return new EndNode(-1, -1, tree);
                    }
                    return tree.Nodes[Nexts[num - 1]];
                }
            }
        }
        public string OptionsToString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < options.Length; i++)
            {
                sb.Append(i + 1);
                sb.Append(". ");
                sb.Append(options[i]);
                if (i < options.Length - 1)
                {
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }
        bool checkNumber(string x)
        {
            foreach(var i in x)
            {
                if (!Char.IsNumber(i))
                {
                    return false;
                }
            }
            return true;
        }
    }
    public class ScriptNode : InteractionNode
    {
        int scriptid;
        string state;
        public override bool RequiresInput(InteractionInstance i)
        {
            return getMachine(ID,i).RequiresInput();
        }


        public ScriptNode(int scriptid,int id, int next, InteractionTree tree) : base(id, next, tree)
        {
            this.scriptid = scriptid;
            this.requiresInput = true;
        }

        public int Scriptid { get => scriptid; set => scriptid = value; }
        public string State { get => state; set => state = value; }

        public override string GetEntry(InteractionInstance i, Player p)
        {
            ISMachine mc = getMachine(this.ID, i);
            return mc.GetEntry(p);
        }
        public override InteractionNode GetNext(InteractionInstance i, IMessage msg)
        {
            ISMachine mc = getMachine(this.ID, i);
            int next = mc.GetNext(msg);
            if (next == -1)
            {
                return new EndNode(0, 0, null);
            }
            else
            {
                return this.tree.Nodes[next];
            }
        }
        public ISMachine getMachine(int id, InteractionInstance i) {
            if (i.Machines.TryGetValue(ID, out ISMachine mac))
            {
                return mac;
               
            }
            else
            {
                ISMachine m = new ISMachine(i.Player.Model.Api,ID,scriptid, i);
                i.Machines.Add(id, m);
                return m;
            }

        }
    }
    public class RewardNode : TextNode
    {
        int rewardId;
        int quantity;
        string entry;
        public RewardNode(string entry,int rewardId,int quantity, string text, int id, int next, InteractionTree tree) : base(text, id, next, tree)
        {
            this.rewardId = rewardId;
            this.quantity = quantity;
            this.entry = entry;
        }

        public override string GetEntry(InteractionInstance ic, Player p)
        {
            Console.WriteLine("HERE");
            var i = AssetManager.Items.CreateInstance(rewardId, p);
            return entry+"\n\nYou received an item  "+p.Items.Count+"/??";
        }
        
    }
    public class TextNode : InteractionNode
    {
        string text;

        public TextNode(string text,int id, int next, InteractionTree tree) : base(id, next, tree)
        {
            this.text = text;
        }

        public override string GetEntry(InteractionInstance i, Player p)
        {
            return text;
        }
    }
    public class EndNode : InteractionNode
    {
        public EndNode(int id, int next, InteractionTree tree) : base(id, next, tree)
        {
        }

        public override string GetEntry(InteractionInstance i, Player p)
        {
            return "*You Leave*";
        }
    }
    public abstract class EffectNode : InteractionNode
    {
        public EffectNode(int id, int next, InteractionTree tree) : base(id, next, tree)
        {
        }
    }
    
    public class InteractionTree
    {
        int id;
        InteractionNode root;
        InteractionNode[] nodes;
        
        public InteractionTree(int size)
        {
            nodes = new InteractionNode[size];
        }

        public int Id { get => id; set => id = value; }
        internal InteractionNode Root { get => root; set => root = value; }
        internal InteractionNode[] Nodes { get => nodes; set => nodes = value; }
    }
    public class InteractionInstance
    {
        static ulong _id = 0;
        Player player;
        ulong instance_id = _id++;
        string state;
        InteractionTree tree;
        Room room;
        InteractionNode current, previous;
        bool isTimedOut;
        DateTime lastResponse;
        ulong intermessageid;
        NPCInstance npc;
        Dictionary<int, ISMachine> machines = new Dictionary<int, ISMachine>();
        public InteractionInstance(NPCInstance npc,Player player, InteractionTree tree,  Room room, ulong imid)
        {
            this.player = player;
            this.npc = npc;
            this.tree = tree;
            this.room = room;
            player.OnMessage += Player_OnMessage;
            current = tree.Nodes[0];
            room.Channel.SendMessageAsync(current.GetEntry(this,player));
            intermessageid = imid;
        }

        private void Player_OnMessage(Player p, IMessage message)
        {
            if (message.Id == intermessageid)
            {
                return;
            }
            if (message.Channel.Id == room.Channel.Id)
            {
                Next(message);
            }
        }

        public Player Player { get => player; set => player = value; }
        public InteractionTree Tree { get => tree; set => tree = value; }
        public Room Room { get => room; set => room = value; }
        public InteractionNode Current { get => current; set => current = value; }
        public InteractionNode Previous { get => previous; set => previous = value; }
        public bool IsTimedOut { get => isTimedOut; set => isTimedOut = value; }
        public DateTime LastResponse { get => lastResponse; set => lastResponse = value; }
        public ulong Instance_id { get => instance_id; set => instance_id = value; }
        public Dictionary<int, ISMachine> Machines { get => machines; set => machines = value; }
        public NPCInstance Npc { get => npc; set => npc = value; }
        public string State { get => state; set => state = value; }

        public void Next(IMessage input)
        {
            if (!(current is EndNode))
            {

                var next = current.GetNext(this,input);
                if (next != current)
                {
                    previous = current;
                }
                current = next;
                if (!(next is EndNode))
                {
                    string e =next.GetEntry(this, player);
                    Console.WriteLine("got entry: " + e);
                    input.Channel.SendMessageAsync(e).GetAwaiter().GetResult();
                    if (!current.RequiresInput(this))
                    {
                        Next(input);
                    }
                }

            }
        }
    }
}
