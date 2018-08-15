using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using Discord.Rest;
using System.Collections.Concurrent;

namespace DiscordRpg
{
    public class Program
    {
        static void Main(string[] args)
        {
            AssetManager.Load("assets");
            Server s = new Server("<DISCORD BOT KEY>");
            s.StartMainLoop().GetAwaiter().GetResult();
        }
    }
    public class PlayerCommandListener
    {
        HashSet<ulong> subd = new HashSet<ulong>();
        Model model;
        public PlayerCommandListener(Model m)
        {
            model = m;
            m.Client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg.Author.Id!=model.Client.CurrentUser.Id&&model.Players.TryGetValue(arg.Author.Id,out Player p))
            {
                if(arg.Content == "!look")
                {
                    if(model.Rooms.TryGetValue(arg.Channel.Id,out Room m)){
                        Dictionary<int, NPCInstance> look = new Dictionary<int, NPCInstance>();
                        StringBuilder lookbuilder = new StringBuilder("You see:\n");
                        int en = 0;
                        foreach (var i in m.Npcs)
                        {
                            
                            lookbuilder.Append(en+1+". "+i.Npc.Name+"\n");
                            look.Add(en++, i);
                        }
                        p.LastLook = look;
                        p.LastLookTime = DateTime.Now;
                        await m.Channel.SendMessageAsync(lookbuilder.ToString());
                    }
                }
                else if (arg.Content.ToLower().StartsWith("!interact "))
                {
                    int selection = int.Parse(arg.Content.Split(' ')[1]);
                    if (DateTime.Now.Subtract(p.LastLookTime).TotalSeconds < 30)
                    {
                        if (selection >= 1 && selection <= p.LastLook.Count)
                        {
                            if (p.LastLook.TryGetValue(selection-1, out NPCInstance npc))
                            {
                                npc.StartInteraction(p,arg.Id);
                            }
                        }
                    }
                }else if (arg.Content.ToLower() == "!inv")
                {
                    StringBuilder b = new StringBuilder("Inventory:\n");
                    for(int i = 0; i < p.Items.Count; i++)
                    {
                        
                        b.Append(i + 1 + ". " + p.Items[i].Item.Name);
                        if (i < p.Items.Count - 1)
                        {
                            b.Append("\n");
                        }
                    }
                    await arg.Channel.SendMessageAsync(b.ToString());
                }else if (arg.Content.ToLower().StartsWith("!use "))
                {
                    string argful = arg.Content.ToLower().Remove(0, 5);
                    string[] args = argful.Split(' ');
                    if(int.TryParse(args[0],out int i))
                    {
                        if (i < 1 || i > p.Items.Count)
                        {
                            await arg.Channel.SendMessageAsync("Valid item choices are 1 - " + p.Items.Count);
                            return;
                        }
                        List<Player> targets = new List<Player>() ;
                        foreach(var tar in arg.MentionedUsers)
                        {
                            if(model.Players.TryGetValue(tar.Id,out Player target))
                            {
                                targets.Add(target);
                            }
                            else
                            {
                                await arg.Channel.SendMessageAsync(tar.Username + " is not a player...");
                                return;
                            }
                        }
                        if (!p.Items[i-1].TryApply(targets))
                        {
                            var rem = new TimeSpan(0,0,p.Items[i-1].Item.Cooldown)-DateTime.Now.Subtract(p.Items[i - 1].Lastused);
                            await arg.Channel.SendMessageAsync("Item use failed.. Cooldown remaining "+Math.Floor(rem.TotalMinutes)+":"+rem.Seconds);
                            return;
                        }
                    }
                }else if (arg.Content.ToLower().StartsWith("!stats"))
                {
                    if(p.Mainguildid!= (arg.Channel as IGuildChannel).GuildId)
                    {
                        await arg.Channel.SendMessageAsync("This is not your main guild..");
                        return;
                    }
                    if (model.Guilds.TryGetValue((arg.Channel as IGuildChannel).GuildId,out RpgGuild g))
                    {
                        StringBuilder b = new StringBuilder();
                        b.Append("Name".PadRight(20) + "|" + "Health".PadRight(20)+"\n");
                        foreach(var i in g.Players)
                        {
                            b.Append(i.User.Username.PadRight(20, ' ') + "|" + i.Health.ToString().PadRight(6));
                            if (i != g.Players.Last())
                            {
                                b.Append("\n");
                            }
                        }
                        await arg.Channel.SendMessageAsync(b.ToString());
                    }
                    else
                    {
                        Console.WriteLine("GUILD NOT FOUND");
                    }
                }
            }
            model.PassMessage(arg);
           
        }
    }
    public class Model
    {

        ConcurrentDictionary<ulong, RpgGuild> guilds;
        ConcurrentDictionary<ulong, Player> players;
        ConcurrentDictionary<ulong, Room> rooms;
        DiscordSocketClient client;
        Sqlite3DB db;
        JSBaseApi api;
        PlayerCommandListener command;
        public Model(DiscordSocketClient client, Sqlite3DB db)
        {
            this.client = client;
            this.db = db;
            guilds = new ConcurrentDictionary<ulong, RpgGuild>();
            players = new ConcurrentDictionary<ulong, Player>();
            rooms = new ConcurrentDictionary<ulong, Room>();
            command = new PlayerCommandListener(this);
            client.MessageReceived += Client_MessageReceived;
            api = new JSBaseApi(this);
        }

        private Task Client_MessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        public void PassMessage(IMessage m)

        {
            if (Players.TryGetValue(m.Author.Id, out Player p))
            {
                p.PassMessage(m);
            }
            if ((m.Channel as IGuildChannel)!=null&&guilds.TryGetValue((m.Channel as IGuildChannel).GuildId,out RpgGuild g))
            {
                g.PassMessage(m);
            }
            
        }
        public ConcurrentDictionary<ulong, RpgGuild> Guilds { get => guilds; set => guilds = value; }
        public ConcurrentDictionary<ulong, Player> Players { get => players; set => players = value; }
        public DiscordSocketClient Client { get => client; set => client = value; }
        public Sqlite3DB Db { get => db; set => db = value; }
        public ConcurrentDictionary<ulong, Room> Rooms { get => rooms; set => rooms = value; }
        public JSBaseApi Api { get => api; set => api = value; }
    }
    public class Server
    {
        DiscordSocketClient client;
        
        Model model;
        Sqlite3DB db;
        string key;
        public Server(string key)
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                //LogLevel = LogSeverity.Debug,
                //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
            });
            this.key = key;
          
            client.LoggedIn += Client_Connected;
            client.Log += Client_Log;
            client.MessageReceived += Client_MessageReceived;
            client.ReactionAdded += Client_ReactionAdded;
            client.UserIsTyping += Client_UserIsTyping;
            client.GuildAvailable += Client_GuildAvailable;
            
            client.LoginAsync(TokenType.Bot, key).GetAwaiter().GetResult();
            client.StartAsync();
        }

        private Task Client_GuildAvailable(SocketGuild arg)
        {
            Console.WriteLine(arg.Name);
            var g = db.LoadGuild(model, arg.Id);
            Console.WriteLine(g.Players.Count);
            model.Guilds.TryAdd(g.Guild.Id, g);
            return Task.CompletedTask;
        }

        private Task Client_Log(LogMessage arg)
        {
            return Task.CompletedTask;
        }

        private Task Client_Connected()
        {
            db = new Sqlite3DB("text.sqlite3", client);
            model = db.LoadModel(client);
            return Task.CompletedTask;
        }

        private Task Client_UserIsTyping(SocketUser arg1, ISocketMessageChannel arg2)
        {
            return Task.CompletedTask;
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            return Task.CompletedTask;
        }

        private Task Client_MessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        public Task StartMainLoop()
        {
           
            
            return Task.Run(() => Run());
        }


        public async void Run()
        {
            while (true)
            {
                foreach(var i in model.Guilds)
                {
                    i.Value.SpawnNpcs();
                }
            }
        }
    }
    public class RpgGuild
    {
        IGuild guild;
        string state;
        List<Player> players;
        List<Room> rooms;
        Model model;
        static int maxNpcs = 3;
        public ulong Guildid { get => guild.Id; }
        public IGuild Guild { get => guild; set => guild = value; }
        public string State { get => state; set => state = value; }
        public List<Player> Players { get => players; set => players = value; }
        public List<Room> Rooms { get => rooms; set => rooms = value; }

        public delegate void OnMessageDel(RpgGuild g,IMessage message);
        public event OnMessageDel OnMessage;
        
        public RpgGuild(Model m,IGuild guild)
        {
            this.guild = guild;
            players = new List<Player>();
            rooms = new List<Room>();
            this.model = m;
       
        }
        public void Delete()
        {

        }
        public void SpawnNpcs()
        {
            if (CountNpcs() >= maxNpcs)
            {
                return;
            }
            Random r = new Random(Environment.TickCount);
            foreach(var i in AssetManager.NPCs.Npcs)
            {
                if (i.Value.IsAutoSpawn && !HasNpc(i.Value))
                {
                    var rm = rooms[r.Next(rooms.Count)];
                    rm.AddNpc(AssetManager.NPCs.CreateInstance(i.Value.Id,rm,this));
                    Console.WriteLine("NPC Instance spawned in: " + guild.Name);
                }
                
            }
        }
        public bool HasNpc(NPC npc)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                foreach(var n in rooms[i].Npcs)
                {
                    if (n.Npc == npc)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public int CountNpcs()
        {
            int count=0;
            for(var i = 0; i < rooms.Count; i++)
            {
                count += rooms[i].Npcs.Count;
            }
            return count;
        }
        public void PassMessage(IMessage m)
        {
            OnMessage(this, m);
            foreach(Room r in rooms)
            {
                if(r.Channel.Id == m.Channel.Id)
                {
                    r.PassMessage(m);
                }
            }
        }

    }
   
    public class Player
    {
        ulong mainguildid;
        IUser user;
        List<ItemInstance> items;
        List<EffectInstance> effects;
        int health,gold;
        string state;
        Model model;
        Dictionary<int, NPCInstance> lastLook;
        DateTime lastLookTime;
        public ulong Userid { get => user.Id; }
        public ulong Mainguildid { get => mainguildid; set => mainguildid = value; }
        public IUser User { get => user; set => user = value; }
        
        internal List<EffectInstance> Effects { get => effects; set => effects = value; }
        public int Health { get => health; set => health = value; }
        public string State { get => state; set => state = value; }
        public int Health1 { get => health; set => health = value; }
        public int Gold { get => gold; set => gold = value; }
        public Dictionary<int, NPCInstance> LastLook { get => lastLook; set => lastLook = value; }
        public DateTime LastLookTime { get => lastLookTime; set => lastLookTime = value; }
        public List<ItemInstance> Items { get => items; set => items = value; }
        public Model Model { get => model; set => model = value; }

        public delegate void OnMessageDel(Player p,IMessage message);
        public event OnMessageDel OnMessage;
        public delegate void OnEnterVoiceChannelDel(Player p);
        public event OnEnterVoiceChannelDel OnEnterVoiceChannel;
        public delegate void OnStartTypingDel(Player p,IChannel channel);
        public event OnStartTypingDel OnStartTyping;
        public Player(Model model,ulong mainguildid,int health,IUser user)
        {
            this.user = user;
            this.health = health;
            this.effects = new List<EffectInstance>();
            this.items = new List<ItemInstance>();
            this.gold = 0;
            this.model = model;
        }
        public void AddEffect(EffectInstance i)
        {

        }
        public void RemoveEffect(ulong id)
        {
            
        }
        public void AddItem(ItemInstance i)
        {
            Console.WriteLine("Adding item to player:" + i.Item.Name);
            items.Add(i);
        }
        public void RemoveItem(ulong id)
        {
            items.RemoveAll((item) => item.Id == id);
        }
        
        public void PassMessage(IMessage m)
        {
            OnMessage(this, m);
        }
        public void Delete()
        {

        }
    }
    public class Room
    {
        ITextChannel channel;
        ulong channelid;
        List<NPCInstance> npcs;
        Model model;
        string state;
        RpgGuild guild;
        public ITextChannel Channel { get => channel; set => channel = value; }
        public ulong Channelid { get => channelid; set => channelid = value; }
        public List<NPCInstance> Npcs { get => npcs; set => npcs = value; }
        public string State { get => state; set => state = value; }
        public RpgGuild Guild { get => guild; set => guild = value; }

        public delegate void OnMessageDel(Room r,IMessage message);
        public event OnMessageDel OnMessage;
        public Room(Model m,RpgGuild guild,ITextChannel channel, List<NPCInstance> npcs)
        {
            this.guild = guild;
            this.channel = channel;
            this.channelid = channel.Id;
            this.npcs = npcs;
            model = m;
        }
        public Room(Model m,RpgGuild guild,ITextChannel channel) : this(m,guild,channel, new List<NPCInstance>())
        {

        }
        public void AddNpc(NPCInstance npc)
        {
            npcs.Add(npc);
            Console.WriteLine("npc: " + npc.Id + " added to " + channel.Name);
        }
        public void RemoveNPC(ulong id)
        {

        }
        public void Delete()
        {

        }

        public void PassMessage(IMessage m)
        {
            foreach(var i in npcs)
            {
                i.InteractionMessage(m);
            }
            OnMessage(this, m);
        }
    }
    public class NPCInstance
    {
        
        string state;
        ulong id;
        NPC npc;
        Room room;
        RpgGuild guild;
        InteractionInstance currentInteraction;
        public NPCInstance(string state, ulong id, NPC npc, Room room, RpgGuild guild)
        {
            this.state = state;
            this.id = id;
            this.npc = npc;
            this.room = room;
            this.guild = guild;
        }
        public void InteractionMessage(IMessage message)
        {

        }
        public InteractionInstance StartInteraction(Player p, ulong imid)
        {
            InteractionInstance i = new InteractionInstance(this,p, npc.Interaction,  room,imid);
            this.currentInteraction = i;
            Console.WriteLine("started interaction");
            return i;
        }
        public string State { get => state; set => state = value; }
        public ulong Id { get => id; set => id = value; }
        public NPC Npc { get => npc; set => npc = value; }
        public Room Room { get => room; set => room = value; }
        public RpgGuild Guild { get => guild; set => guild = value; }
    }
    public class NPC
    {
        int id;
        InteractionTree interaction;
        string name;
        bool isAutoSpawn;

        public NPC(int id, InteractionTree interaction, string name, bool isAutoSpawn)
        {
            this.id = id;
            this.interaction = interaction;
            this.name = name;
            this.isAutoSpawn = isAutoSpawn;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public bool IsAutoSpawn { get => isAutoSpawn; set => isAutoSpawn = value; }
        internal InteractionTree Interaction { get => interaction; set => interaction = value; }

        public void Delete()
        {

        }
    }
    
    
   
    

}
