﻿using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Menus;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using StardewValley.Events;
using StardewValley.Locations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MultiplayerTime
{
    public class ModConfig
    {
        public SButton ActivationKey { get; set; } = SButton.F3;
        public bool Active { get; set; } = true;
        public bool UiInfoSuite { get; set; } = false;
    }

    class MultiplayerTimeMod : Mod
    {
        /*********
       ** Public methods
       *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public int LastTimeInterval = -1;
        public int original;
        public bool difference;
        public double timeScale = 1.00;
        public int pausedTickCount = 1;

        public int CurrentDefaultTickInterval => 7000 + (Game1.currentLocation?.getExtraMillisecondsPerInGameMinuteForThisLocation() ?? 0);
        List<IMultiplayerPeer> peers = new List<IMultiplayerPeer>();
        Vector2 PasekPosition = new Vector2(44, 240);
        Vector2 PasekPositionColor = new Vector2(68, 264);
        Vector2 PasekZoomPositionColor = new Vector2(68, 292);
        Color textColor = Game1.textColor;
        Texture2D Pasek;
        Texture2D PasekWithUIS;
        Texture2D PasekZoom;
        Texture2D Black;
        Texture2D Blue;
        Texture2D Green;
        Texture2D Red;
        private ModConfig Config;
        public override void Entry(IModHelper helper)
        {
            Pasek = helper.Content.Load<Texture2D>("assets/Pasek.png", ContentSource.ModFolder);
            PasekWithUIS = helper.Content.Load<Texture2D>("assets/PasekWithUIS.png", ContentSource.ModFolder);
            PasekZoom = helper.Content.Load<Texture2D>("assets/PasekZoom.png", ContentSource.ModFolder);
            Black = helper.Content.Load<Texture2D>("assets/Black.png", ContentSource.ModFolder);
            Blue = helper.Content.Load<Texture2D>("assets/Blue.png", ContentSource.ModFolder);
            Green = helper.Content.Load<Texture2D>("assets/Green.png", ContentSource.ModFolder);
            Red = helper.Content.Load<Texture2D>("assets/Red.png", ContentSource.ModFolder);
            this.Config = (ModConfig)helper.ReadConfig<ModConfig>();
            if (this.Config.Active)
            {
                Helper.Events.GameLoop.UpdateTicked += this.GameEvents_UpdateTick;
                Helper.Events.Display.RenderingHud += this.PreRenderHud;
                Helper.Events.Display.RenderedHud += this.PostRenderHud;
                Helper.Events.Display.Rendered += this.RenderClock;
                Helper.Events.GameLoop.Saved += this.Statement;
            }
            Helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(this.ButtonPressed);
            Helper.Events.GameLoop.Saving += this.Save;
            Helper.Events.GameLoop.SaveLoaded += this.Save;
            Helper.Events.Multiplayer.PeerContextReceived += this.PlayerConnected;
            Helper.Events.Multiplayer.PeerDisconnected += this.PlayerDisconnected;
            if (this.Config.UiInfoSuite)
            {
                PasekPosition.Y += 42;
                PasekPositionColor.Y += 42;
                PasekZoomPositionColor.Y += 42;
            }
        }

        private void ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Context.IsWorldReady)
            {
                if (e.Button == this.Config.ActivationKey)
                {
                    if (this.Config.Active)
                    {
                        this.Config.Active = false;
                        Helper.Events.GameLoop.UpdateTicked -= this.GameEvents_UpdateTick;
                        Helper.Events.Display.RenderingHud -= this.PreRenderHud;
                        Helper.Events.Display.RenderedHud -= this.PostRenderHud;
                        Helper.Events.Display.Rendered -= this.RenderClock;
                        this.Helper.WriteConfig(Config);
                        if (Context.IsWorldReady)
                        {
                            Game1.chatBox.addMessage("Multiplayer time Mod turned off", Color.White);
                            if (difference)
                            {

                                original = Game1.player.MagneticRadius;
                            }
                            else
                            {
                                original = original + Game1.player.MagneticRadius;
                            }
                            Game1.player.MagneticRadius = original;
                            difference = true;
                        }
                    }
                    else
                    {
                        this.Config.Active = true;
                        Helper.Events.GameLoop.UpdateTicked += this.GameEvents_UpdateTick;
                        Helper.Events.Display.RenderingHud += this.PreRenderHud;
                        Helper.Events.Display.RenderedHud += this.PostRenderHud;
                        Helper.Events.Display.Rendered += this.RenderClock;
                        this.Helper.WriteConfig(Config);
                        if (Context.IsWorldReady)
                        {
                            Game1.chatBox.addMessage("Multiplayer time Mod turned on", Color.White);
                            difference = true;
                            LastTimeInterval = -1;
                        }
                    }
                }
                if (e.Button == SButton.MouseLeft && this.Config.Active && Context.IsMultiplayer)
                {
                    if (Game1.options.zoomButtons && new Rectangle((int)(Game1.dayTimeMoneyBox.position.X + PasekZoomPositionColor.X), (int)(Game1.dayTimeMoneyBox.position.Y + PasekZoomPositionColor.Y), 108, 24).Contains(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        foreach (Farmer gracz in Game1.getOnlineFarmers())
                        {
                            bool msg = false;
                            if (Context.IsMainPlayer)
                            {
                                foreach (IMultiplayerPeer peer in peers)
                                {
                                    if (peer.GetMod("mlsof21.ServerTimeSpeed") == null)
                                    {
                                        if (peer.PlayerID == gracz.UniqueMultiplayerID)
                                        {
                                            Game1.chatBox.addMessage(gracz.Name + " does not have Multiplayer Time mod", Color.Red);
                                            msg = true;
                                        }
                                    }
                                }
                            }
                            if (gracz.MagneticRadius != 0 && !msg)
                            {
                                Game1.chatBox.addMessage("Time doesn't freeze because of " + gracz.Name, Color.White);
                            }
                        }
                        this.Helper.Input.Suppress(SButton.MouseLeft);
                    }
                    if (!Game1.options.zoomButtons && new Rectangle((int)(Game1.dayTimeMoneyBox.position.X + PasekPositionColor.X), (int)(Game1.dayTimeMoneyBox.position.Y + PasekPositionColor.Y), 108, 24).Contains(Game1.getMouseX(), Game1.getMouseY()))
                    {
                        foreach (Farmer gracz in Game1.getOnlineFarmers())
                        {
                            bool msg = false;
                            if (Context.IsMainPlayer)
                            {
                                foreach (IMultiplayerPeer peer in peers)
                                {
                                    if (peer.GetMod("mlsof21.ServerTimeSpeed") == null)
                                    {
                                        if (peer.PlayerID == gracz.UniqueMultiplayerID)
                                        {
                                            Game1.chatBox.addMessage(gracz.Name + " does not have Multiplayer Time mod", Color.Red);
                                            msg = true;
                                        }
                                    }
                                }
                            }
                            if (gracz.MagneticRadius != 0 && !msg)
                            {
                                Game1.chatBox.addMessage("Time doesn't freeze because of " + gracz.Name, Color.White);
                            }
                        }
                        this.Helper.Input.Suppress(SButton.MouseLeft);
                    }
                }
            }
        }

        private void Statement(object sender, EventArgs e)
        {
            if (this.Config.Active)
            {
                Game1.chatBox.addMessage("Multiplayer time Mod turned on", Color.White);
            }
            else
            {
                Game1.chatBox.addMessage("Multiplayer time Mod turned off", Color.White);
            }
        }

        private void Save(object sender, EventArgs e)
        {
            Game1.player.MagneticRadius = 128;
            if (Game1.player.rightRing != null && Game1.player.rightRing.Value != null && Game1.player.rightRing.Value.indexInTileSheet == 518)
            {
                Game1.player.MagneticRadius += 64;
            }

            if (Game1.player.leftRing != null && Game1.player.leftRing.Value != null && Game1.player.leftRing.Value.indexInTileSheet == 518)
            {
                Game1.player.MagneticRadius += 64;
            }

            if (Game1.player.rightRing != null && Game1.player.rightRing.Value != null && Game1.player.rightRing.Value.indexInTileSheet == 519)
            {
                Game1.player.MagneticRadius += 128;
            }

            if (Game1.player.leftRing != null && Game1.player.leftRing.Value != null && Game1.player.leftRing.Value.indexInTileSheet == 519)
            {
                Game1.player.MagneticRadius += 128;
            }

            if (Game1.player.rightRing != null && Game1.player.rightRing.Value != null && Game1.player.rightRing.Value.indexInTileSheet == 527)
            {
                Game1.player.MagneticRadius += 128;
            }

            if (Game1.player.leftRing != null && Game1.player.leftRing.Value != null && Game1.player.leftRing.Value.indexInTileSheet == 527)
            {
                Game1.player.MagneticRadius += 128;
            }
            difference = true;
        }

        private void PlayerConnected(object sender, PeerContextReceivedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                peers.Add(e.Peer);
                if (e.Peer.GetMod("mlsof21.ServerTimeSpeed") == null)
                {
                    Game1.chatBox.addMessage(Game1.getOnlineFarmers().FirstOrDefault(p => p.UniqueMultiplayerID == e.Peer.PlayerID)?.Name ?? e.Peer.PlayerID.ToString() + " does not have Multiplayer Time mod", Color.Red);
                }
            }
        }

        private void PlayerDisconnected(object sender, PeerDisconnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                peers.Remove(e.Peer);
            }
        }

        private void PreRenderHud(object sender, EventArgs e)
        {
            if (shouldTimeSlow())
            {
                Game1.textColor *= 0f;
                Game1.dayTimeMoneyBox.timeShakeTimer = 0;
            }
            if (Context.IsMultiplayer && !Game1.isFestival())
            {
                drawPasek(Game1.spriteBatch);
            }
        }

        private void PostRenderHud(object sender, EventArgs e)
        {
            if (shouldTimeSlow())
            {
                drawfade(Game1.spriteBatch);
            }
            Game1.textColor = textColor;
        }

        private void RenderClock(object sender, EventArgs e)
        {
            if (Context.IsWorldReady)
            {
                if (!Game1.isFestival() && !(Game1.farmEvent != null && (Game1.farmEvent is FairyEvent || Game1.farmEvent is WitchEvent || Game1.farmEvent is SoundInTheNightEvent)) && (Game1.eventUp || Game1.currentMinigame != null || Game1.activeClickableMenu is AnimalQueryMenu || Game1.activeClickableMenu is PurchaseAnimalsMenu || Game1.activeClickableMenu is CarpenterMenu || Game1.freezeControls))
                {
                    Game1.textColor *= 0f;
                    Game1.dayTimeMoneyBox.timeShakeTimer = 0;
                    Game1.spriteBatch.End();
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    Game1.dayTimeMoneyBox.draw(Game1.spriteBatch);
                    Game1.textColor = textColor;
                    drawfade(Game1.spriteBatch);
                    if (Context.IsMultiplayer)
                    {
                        drawPasek(Game1.spriteBatch);
                    }
                }
            }
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (Context.IsWorldReady)
            {
                Monitor.Log($"GameTimeInterval: {Game1.gameTimeInterval}, TickLength: {7000 + Game1.currentLocation?.getExtraMillisecondsPerInGameMinuteForThisLocation()}", LogLevel.Debug);
                if (shouldTimeSlow())
                {
                    timeScale = TimeSlowScale();
                    if (pausedTickCount >= Game1.getOnlineFarmers().Count)
                    {
                        pausedTickCount = 1;
                    }
                    else
                    {
                        pausedTickCount++;
                    }
                }
                else
                {
                    timeScale = 1;
                }
                if (difference)
                {
                    original = Game1.player.MagneticRadius;
                }
                else
                {
                    original = original + Game1.player.MagneticRadius;
                }

                if (!Context.IsPlayerFree || (Game1.currentMinigame != null && Game1.currentMinigame.minigameId() == "PrairieKing") || Game1.player.isEating || Game1.freezeControls || Game1.activeClickableMenu is BobberBar)
                {
                    Game1.player.MagneticRadius = 0;
                    difference = false;
                }

                if (Context.IsPlayerFree && !Game1.player.isEating && Game1.currentMinigame == null && !Game1.freezeControls)
                {
                    Game1.player.MagneticRadius = original;
                    difference = true;
                }

                if (Context.IsMainPlayer && shouldTimeSlow())
                {
                    var npcMovementPause = ShouldNpcsMove() ? 0 : 1;
                    //Monitor.Log($"NPC Movement Pause: {npcMovementPause}, PausedTickCount: {pausedTickCount}", LogLevel.Debug);
                    foreach (GameLocation location in Game1.locations)
                    {
                        foreach (Character NPCs in location.characters)
                        {
                            if (NPCs is NPC)
                            {
                                (NPCs as NPC).movementPause = npcMovementPause;
                            }
                        }
                    }
                    foreach (Farmer farmer in Game1.getOnlineFarmers())
                    {
                        if (farmer.currentLocation.currentEvent != null)
                        {
                            if (farmer.currentLocation is Farm)
                            {
                                foreach (FarmAnimal animal in (farmer.currentLocation as Farm).getAllFarmAnimals())
                                {
                                    animal.pauseTimer = 100;
                                }
                            }
                            if (farmer.currentLocation is AnimalHouse)
                            {
                                foreach (FarmAnimal animal in (farmer.currentLocation as AnimalHouse).animals.Values)
                                {
                                    animal.pauseTimer = 100;
                                }
                            }
                            foreach (Character Monsters in farmer.currentLocation.characters)
                            {
                                if (Monsters != null && Monsters is NPC && Monsters is Monster)
                                {
                                    if (!(Monsters is Bug))
                                    {
                                        (Monsters as Monster).Halt();
                                    }
                                    if (Monsters is Bat || Monsters is Ghost || Monsters is DustSpirit)
                                    {
                                        (Monsters as Monster).xVelocity = 0f;
                                        (Monsters as Monster).yVelocity = 0f;
                                        if (Monsters is DustSpirit)
                                        {
                                            (Monsters as Monster).yJumpVelocity = 0f;
                                        }
                                    }
                                    if (Monsters is GreenSlime || Monsters is SquidKid)
                                    {
                                        (Monsters as Monster).moveTowardPlayer(0);
                                    }
                                    if (Monsters is Fly || Monsters is Serpent)
                                    {
                                        (Monsters as Monster).setInvincibleCountdown(100);
                                        (Monsters as Monster).stopGlowing();
                                    }
                                    (Monsters as Monster).Speed = (int)(Monsters.Speed * timeScale);
                                }
                            }
                        }
                    }
                }

                if (Context.IsMainPlayer && !shouldTimeSlow())
                {
                    foreach (Farmer farmer in Game1.getOnlineFarmers())
                    {
                        foreach (Character Monsters in farmer?.currentLocation?.characters)
                        {
                            if (Monsters is NPC && Monsters is Monster && (Monsters as Monster).Speed == 0)
                            {
                                (Monsters as Monster).Speed = Convert.ToInt32(monsterInfo((Monsters as Monster).Name)[10]);
                                if (Monsters is GreenSlime || Monsters is SquidKid)
                                {
                                    (Monsters as Monster).moveTowardPlayer(Convert.ToInt32(monsterInfo((Monsters as Monster).Name)[9]));
                                }
                            }
                        }
                    }
                }

                if (Game1.currentLocation != null)
                {
                    for (int k = Game1.currentLocation.TemporarySprites.Count - 1; k >= 0; k--)
                    {
                        if (Game1.currentLocation.TemporarySprites[k].bombRadius > 0 && !shouldTimeSlow())
                        {
                            Game1.currentLocation.TemporarySprites[k].paused = false;
                        }
                        if (Game1.currentLocation.TemporarySprites[k].bombRadius > 0 && shouldTimeSlow())
                        {
                            Game1.currentLocation.TemporarySprites[k].paused = true;
                        }
                    }
                }

                if (Context.IsMainPlayer)
                {
                    if (!shouldTimeSlow())
                    {
                        LastTimeInterval = -1;
                        return;
                    }

                    if (LastTimeInterval <= -1)
                    {
                        if (Game1.gameTimeInterval > 6800)
                        {
                            LastTimeInterval = 6800;
                        }

                        else
                        {
                            LastTimeInterval = Game1.gameTimeInterval;
                        }
                    }
                    else
                    {
                        int delta;
                        if (Game1.gameTimeInterval < LastTimeInterval)
                        {
                            delta = Game1.gameTimeInterval;
                            LastTimeInterval = 0;
                        }
                        else
                        {
                            delta = Game1.gameTimeInterval - LastTimeInterval;
                        }

                        Game1.gameTimeInterval = LastTimeInterval + (int)(delta * timeScale);
                        LastTimeInterval = Game1.gameTimeInterval;
                    }
                }
                return;
            }
        }

        private string[] monsterInfo(string name)
        {
            return Game1.content.Load<Dictionary<string, string>>("Data\\Monsters")[name].Split(new char[]
            {
                '/'
            });
        }

        private bool ShouldPause()
        {
            if (!Context.IsPlayerFree || (Game1.currentMinigame != null && Game1.currentMinigame.minigameId() == "PrairieKing") || Game1.player.isEating || Game1.freezeControls || Game1.activeClickableMenu is BobberBar)
                return true;
            return false;
        }

        private bool shouldTimeSlow()
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.MagneticRadius == 0)
                {
                    return true;
                }
                if(farmer.currentLocation is MineShaft)
                {
                    int mineLevel = GetMineLevel(farmer.currentLocation?.Name);

                    if (mineLevel > 120) 
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private int GetMineLevel(string mineName)
        {
            var number = Regex.Match(mineName, @"\d+").Value;

            return int.Parse(number);
        }

        private double TimeSlowScale()
        {
            double pausedFarmers = 0;
            double totalFarmers = Game1.getOnlineFarmers().Count;
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.MagneticRadius == 0)
                {
                    pausedFarmers++;
                }
            }

            //Monitor.Log($"Total farmers: {totalFarmers}, paused: {pausedFarmers}", LogLevel.Debug);

            return (totalFarmers - pausedFarmers) / totalFarmers;
        }

        private bool ShouldNpcsMove()
        {
            int pausedFarmers = 0;
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.MagneticRadius == 0)
                {
                    pausedFarmers++;
                }
            }

            if(pausedTickCount > pausedFarmers && pausedTickCount <= Game1.getOnlineFarmers().Count)
            {
                return true;
            }

            return false;
        }

        private void drawfade(SpriteBatch b)
        {
            string text = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) + ". " + Game1.dayOfMonth;
            Vector2 dayPosition = new Vector2((float)Math.Floor(183.5f - Game1.dialogueFont.MeasureString(text).X / 2), (float)18);
            b.DrawString(Game1.dialogueFont, text, Game1.dayTimeMoneyBox.position + dayPosition, textColor);
            string timeofDay = (Game1.timeOfDay < 1200 || Game1.timeOfDay >= 2400) ? " am" : " pm";
            string zeroPad = (Game1.timeOfDay % 100 == 0) ? "0" : "";
            string hours = (Game1.timeOfDay / 100 % 12 == 0) ? "12" : string.Concat(Game1.timeOfDay / 100 % 12);
            string time = string.Concat(new object[]
            {
                hours,
                ":",
                Game1.timeOfDay % 100,
                zeroPad,
                timeofDay
            });
            Vector2 timePosition = new Vector2((float)Math.Floor(183.5 - Game1.dialogueFont.MeasureString(time).X / 2), (float)108);
            bool nofade = shouldTimeSlow() || Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0 > 1000.0;
            b.DrawString(Game1.dialogueFont, time, Game1.dayTimeMoneyBox.position + timePosition, (Game1.timeOfDay >= 2400) ? Color.Red : (textColor * (nofade ? 1f : 0.5f)));
        }

        private void drawPasek(SpriteBatch b)
        {
            int width = (int)(108 - (Game1.getOnlineFarmers().Count - 1) * 4) / Game1.getOnlineFarmers().Count;
            int i = 0;
            if (this.Config.UiInfoSuite)
            {
                b.Draw(PasekWithUIS, Game1.dayTimeMoneyBox.position + PasekPosition + new Vector2(0, -42), null, Color.White, 0.0f, Vector2.Zero, 4, SpriteEffects.None, 0.99f);
            }
            if (Game1.options.zoomButtons)
            {
                b.Draw(PasekZoom, Game1.dayTimeMoneyBox.position + PasekPosition, null, Color.White, 0.0f, Vector2.Zero, 4, SpriteEffects.None, 0.99f);
                foreach (Farmer gracz in Game1.getOnlineFarmers())
                {
                    if (gracz.MagneticRadius != 0)
                    {
                        b.Draw(Blue, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                        if (Context.IsMainPlayer)
                        {
                            foreach (IMultiplayerPeer peer in peers)
                            {
                                if (peer.GetMod("mlsof21.ServerTimeSpeed") == null)
                                {
                                    if (peer.PlayerID == gracz.UniqueMultiplayerID)
                                    {
                                        b.Draw(Red, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        b.Draw(Green, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    if (i != Game1.getOnlineFarmers().Count - 1)
                    {
                        b.Draw(Black, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4) + width, 0), new Rectangle(i * (width + 4) + width, 0, 4, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    i++;
                }
            }
            else
            {
                b.Draw(Pasek, Game1.dayTimeMoneyBox.position + PasekPosition, null, Color.White, 0.0f, Vector2.Zero, 4, SpriteEffects.None, 0.99f);
                foreach (Farmer gracz in Game1.getOnlineFarmers())
                {
                    if (gracz.MagneticRadius != 0)
                    {
                        b.Draw(Blue, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                        if (Context.IsMainPlayer)
                        {
                            foreach (IMultiplayerPeer peer in peers)
                            {
                                if (peer.GetMod("mlsof21.ServerTimeSpeed") == null)
                                {
                                    if (peer.PlayerID == gracz.UniqueMultiplayerID)
                                    {
                                        b.Draw(Red, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        b.Draw(Green, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    if (i != Game1.getOnlineFarmers().Count - 1)
                    {
                        b.Draw(Black, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4) + width, 0), new Rectangle(i * (width + 4) + width, 0, 4, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    i++;
                }
            }
        }
    }
}