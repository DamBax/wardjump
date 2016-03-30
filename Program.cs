using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace WardJump
{
    public static class Program
    {
        public static Menu Menu;
        public static Spell.Targeted W;
        public static Vector2 JumpPos;
        private static Vector3 lastWardPos;
        private static float wcasttime;
        private static readonly bool castWardAgain = true;
        public static int LastWard, LastQ2;
        private static bool reCheckWard = true;


        private static Vector3 mouse = Game.CursorPos;

        private enum WCastStage
        {
            First,

            Second,

            Cooldown
        }
        public static bool jumped;
        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }
        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.LeeSin)
            {
                return;
            }

            Menu = MainMenu.AddMenu("Lee WardJumper", "wardjumper");
            Menu.AddGroupLabel("Ward Jump Settings");
            Menu.Add("ElLeeSin.Wardjump", new KeyBind("Wardjump :", false, KeyBind.BindTypes.HoldActive, 'G'));
            Menu.Add("ElLeeSin.Wardjump.MaxRange", new CheckBox("Ward jump on max range", false));
            Menu.Add("ElLeeSin.Wardjump.Mouse", new CheckBox("Jump to mouse"));
            Menu.Add("ElLeeSin.Wardjump.Minions", new CheckBox("Jump to minions"));
            Menu.Add("ElLeeSin.Wardjump.Champions", new CheckBox("Jump to champions"));


            W = new Spell.Targeted(SpellSlot.W, 700);

            Game.OnTick += OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
           
         
        }

        public static void Orbwalk(Vector3 pos, AIHeroClient target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private static void OnTick(EventArgs args)
        {
            if (ElLeeSinWardjump)
            {
                Orbwalk(Game.CursorPos);
                WardjumpToMouse();
            }
  
        }



        public static bool ElLeeSinWardjump { get { return Menu["ElLeeSin.Wardjump"].Cast<KeyBind>().CurrentValue; } }
        public static bool ElLeeSinWardjumpMaxRange { get { return Menu["ElLeeSin.Wardjump.MaxRange"].Cast<CheckBox>().CurrentValue; } }
        public static bool ElLeeSinWardjumpMouse { get { return Menu["ElLeeSin.Wardjump.Mouse"].Cast<CheckBox>().CurrentValue; } }
        public static bool ElLeeSinWardjumpMinions { get { return Menu["ElLeeSin.Wardjump.Minions"].Cast<CheckBox>().CurrentValue; } }
        public static bool ElLeeSinWardjumpChampions { get { return Menu["ElLeeSin.Wardjump.Champions"].Cast<CheckBox>().CurrentValue; } }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        private static Vector3 InterceptionPoint(List<AIHeroClient> heroes)
        {
            var result = new Vector3();
            foreach (var hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return myHero.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }

        public static InventorySlot FindBestWardItem()
        {
            try
            {
                var slot = GetWardSlot();
                if (slot == default(InventorySlot))
                {
                    return null;
                }

                var sdi = GetItemSpell(slot);
                if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
                {
                    return slot;
                }
                return slot;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public static InventorySlot GetWardSlot()
        {
            var wardIds = new[] { 2045, 2049, 2050, 2301, 2302, 2303, 3340, 3361, 3362, 3711, 1408, 1409, 1410, 1411, 2043 };
            return (from wardId in wardIds where Item.CanUseItem(wardId) select ObjectManager.Player.InventoryItems.FirstOrDefault(slot => slot.Id == (ItemId)wardId)).FirstOrDefault();
        }

        private static WCastStage WStage
        {
            get
            {
                if (!W.IsReady())
                {
                    return WCastStage.Cooldown;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "blindmonkwtwo" ? WCastStage.Second : WCastStage.First);
            }
        }

        private static void CastW(Obj_AI_Base obj)
        {
            if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
            {
                return;
            }

            W.Cast(obj);
            wcasttime = Environment.TickCount;
        }

        private static void WardJump(Vector3 pos, bool m2M = true, bool maxRange = false, bool reqinMaxRange = false, bool minions = true, bool champions = true)
        {
            if (WStage != WCastStage.First)
            {
                return;
            }

            var basePos = myHero.Position.To2D();
            var newPos = (pos.To2D() - myHero.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.To2D();
                }
                else if (maxRange || myHero.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (myHero.Distance(pos)));
                }
            }

            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Core.DelayAction(delegate
                {
                    if (JumpPos != new Vector2())
                    {
                        JumpPos = new Vector2();
                        reCheckWard = true;
                    }
                }, 20);
            }

            if (m2M)
            {
                Orbwalk(pos);
            }

            if (!W.IsReady() || WStage != WCastStage.First || reqinMaxRange && myHero.Distance(pos) > W.Range)
            {
                return;
            }

            if (minions || champions)
            {
                if (champions)
                {
                    var champs = (from champ in ObjectManager.Get<AIHeroClient>() where champ.IsAlly && champ.Distance(myHero) < W.Range && champ.Distance(pos) < 200 && !champ.IsMe select champ).ToList();
                    if (champs.Count > 0 && WStage == WCastStage.First)
                    {
                        if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(champs[0]);
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 = (from minion in ObjectManager.Get<Obj_AI_Minion>() where minion.IsAlly && minion.Distance(myHero) < W.Range && minion.Distance(pos) < 200 && !minion.Name.ToLower().Contains("ward") select minion).ToList();
                    if (minion2.Count > 0 && WStage == WCastStage.First)
                    {
                        if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(minion2[0]);
                        return;
                    }
                }
            }

            var isWard = false;
            foreach (var ward in ObjectManager.Get<Obj_AI_Base>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                    {
                        return;
                    }

                    CastW(ward);
                    wcasttime = Environment.TickCount;
                }
            }

            if (!isWard && castWardAgain)
            {
                if (Game.Time - LastWard >= 3)
                {
                    var ward = FindBestWardItem();
                    if (ward != null || WStage != WCastStage.First)
                    {
                        if (ward != null)
                        {
                            myHero.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());
                        }

                        lastWardPos = JumpPos.To3D();
                        LastWard = (int)Game.Time;
                    }
                }
            }
        }

        private static void WardjumpToMouse()
        {
            WardJump(Game.CursorPos, ElLeeSinWardjumpMouse, ElLeeSinWardjumpMaxRange, false, ElLeeSinWardjumpMinions, ElLeeSinWardjumpChampions);
        }

    }
     
     
    }
