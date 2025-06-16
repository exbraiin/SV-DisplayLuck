using Exbrain.DisplayLuck.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Text;

namespace Exbrain.DisplayLuck;

internal sealed class ModEntry : Mod
{
    private const int FARMER_SKILLS = 5;

    private const string SPECIAL_QUEST_EVENT_ID = "15389722";
    private const string SPECIAL_QI_QUEST_EVENT_ID = "10040609";

    private ModConfig? _config;
    private HUDMessage? stackMessage;

    private bool showInfo = false;
    private string textEvents = string.Empty;
    private readonly int[] currentXp = new int[FARMER_SKILLS];
    private readonly int[] missingXp = new int[FARMER_SKILLS];
    private readonly GameTime gameTimeZero = new GameTime();

    public override void Entry(IModHelper helper)
    {
        _config = Helper.ReadConfig<ModConfig>();
        helper.Events.Input.ButtonReleased += Input_ButtonReleased;
        helper.Events.Display.RenderingHud += Display_RenderingHud;
        helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
        helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
        helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;
        helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
#if DEBUG
        helper.Events.Display.MenuChanged += Display_MenuChanged;
#endif
    }

    private void Display_MenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (Game1.activeClickableMenu == null) return;
        if (Game1.activeClickableMenu is not GameMenu menu) return;

        var page = menu.pages[2];
        if (page is SocialPage social)
        {
            var x = social.xPositionOnScreen;
            var y = social.yPositionOnScreen;
            var w = social.width;
            var h = social.height;

            Console.WriteLine("Replacing Menu!");
            menu.pages[1] = new BetterSocialPage(x, y, w, h);
        }
    }

    private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (_config == null) return;
        ModConfig.CreateMenu(Helper, ModManifest, () => _config, (v) => _config = v);
    }

    private static void ExportTexture(Texture2D texture)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var name = Path.GetFileName(texture.Name);
        var path = Path.Combine(desktop, name);
        var stream = File.OpenWrite(path);
        texture.SaveAsPng(stream, texture.Width, texture.Height);
    }

    private void Display_RenderingHud(object? sender, RenderingHudEventArgs e)
    {
        if (!Game1.hasLoadedGame || _config == null || !showInfo) return;

        var events = textEvents.Trim();
        var totalSize = new Vector2();
        var eLayout = Game1.smallFont.MeasureString(events);
        totalSize += eLayout;

        var w = 0f;
        if (_config.ShowSkillMissingXp)
        {
            totalSize.Y += 16;
            for (var i = 0; i < missingXp.Length; ++i)
            {
                if (missingXp[i] == 0) continue;
                var l0 = Game1.smallFont.MeasureString($"{missingXp[i]}xp");
                var l1 = Game1.smallFont.MeasureString($" {(SkillType)i}");
                w = Math.Max(w, l0.X);
                totalSize.X = Math.Max(totalSize.X, l0.X + l1.X);
                totalSize.Y += l0.Y;
            }
        }

        Utils.DrawBox(new Rectangle(0, 0, (int)totalSize.X + 64 + 32, (int)totalSize.Y + 64 + 32));
        var h = 0f;
        Utility.drawTextWithShadow(Game1.spriteBatch, events, Game1.smallFont, new Vector2(48, 48), Color.Black);
        h += eLayout.Y;

        if (_config.ShowSkillMissingXp)
        {
            for (var i = 0; i < missingXp.Length; ++i)
            {
                if (missingXp[i] == 0) continue;
                var l = Game1.smallFont.MeasureString($"{missingXp[i]}xp");
                var x = Math.Max(0, w - l.X);
                Utility.drawTextWithShadow(Game1.spriteBatch, $"{missingXp[i]}xp", Game1.smallFont, new Vector2(48 + x, 48 + h + 16), Color.Black);
                Utility.drawTextWithShadow(Game1.spriteBatch, $" {(SkillType)i}", Game1.smallFont, new Vector2(48 + w, 48 + h + 16), Color.Black);
                h += l.Y;
            }
        }
    }

    private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        UpdateMissingXp();
        for (var i = 0; i < FARMER_SKILLS; ++i)
        {
            currentXp[i] = Game1.player.experiencePoints[i];
        }
    }

    private void GameLoop_DayStarted(object? sender, DayStartedEventArgs e)
    {
        textEvents = GenerateEventsText(_config);
    }

    private void GameLoop_OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (_config == null) return;
        if (!Context.IsWorldReady) return;


        if (_config.ShowSkillMissingXp) UpdateMissingXp();
        if (_config.ShareSkillXp) UpdateSharedSkillXp();
    }

    private void Input_ButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        if (_config == null) return;

        if (e.Button == _config.CalendarButton) OpenOrCloseBoard(() => new Billboard());
        else if (e.Button == _config.BillboardButton) OpenOrCloseBoard(() => new Billboard(true));
        else if (e.Button == _config.SpecialOrdersButton && Game1.player.eventsSeen.Contains(SPECIAL_QUEST_EVENT_ID)) OpenOrCloseBoard(() => new SpecialOrdersBoard());
        else if (e.Button == _config.QiOrdersButton && Game1.player.eventsSeen.Contains(SPECIAL_QI_QUEST_EVENT_ID)) OpenOrCloseBoard(() => new SpecialOrdersBoard("Qi"));
        else if (Context.IsPlayerFree)
        {
            if (e.Button == _config.ShowInfoButton) showInfo = !showInfo;
            else if (e.Button == _config.StackButton) StackToNearbyChests(_config);
        }
    }

    private static void OpenOrCloseBoard(Func<IClickableMenu> onOpen)
    {
        if (Context.IsPlayerFree)
        {
            Game1.activeClickableMenu = onOpen();
        }
        else if (Game1.activeClickableMenu != null)
        {
            Game1.activeClickableMenu.exitThisMenu();
            Game1.activeClickableMenu = null;
        }
    }

    private static bool IsSpecialBoardAvailable(string type = "")
    {
        if (type != string.Empty && type != "Qi") return false;
        if (type == "Qi" && !Game1.player.eventsSeen.Contains(SPECIAL_QI_QUEST_EVENT_ID)) return false;
        if (type == string.Empty && !Game1.player.eventsSeen.Contains(SPECIAL_QUEST_EVENT_ID)) return false;

        if (Game1.player.team.GetAvailableSpecialOrder(0, type) == null) return false;
        if (Game1.player.team.GetAvailableSpecialOrder(1, type) == null) return false;
        if (Game1.player.team.acceptedSpecialOrderTypes.Contains(type)) return false;

        return true;
    }

    private static double MapRange(double inSrc, double inDst, double outSrc, double outDst, double t)
    {
        return outSrc + ((outDst - outSrc) / (inDst - inSrc)) * (t - inSrc);
    }

    private static string GetTodaysRecipe()
    {
        var cooking = DataLoader.Tv_CookingChannel(Game1.temporaryContent);
        int week = (int)(Game1.stats.DaysPlayed % 224 / 7);
        if (week == 0) return string.Empty;

        if (SDate.Now().DayOfWeek != DayOfWeek.Sunday)
        {
            /*
            var team = Game1.player.team;
            if (!team.lastDayQueenOfSauceRerunUpdated.Equals(Game1.Date.TotalDays))
            {
                // team.lastDayQueenOfSauceRerunUpdated.Set(Game1.Date.TotalDays);
                // team.queenOfSauceRerunWeek.Set(GetRerunWeek(dictionary));
            }
            week = team.queenOfSauceRerunWeek.Value;
            Console.WriteLine(team.queenOfSauceRerunWeek.Value);
            Console.WriteLine(team.lastDayQueenOfSauceRerunUpdated.Value);
            */
            return string.Empty;
        }

        if (cooking?.TryGetValue(week.ToString(), out var desc) ?? false)
        {
            return desc?.Split('/').FirstOrDefault() ?? string.Empty;
        }
        return string.Empty;
    }

    private void StackToNearbyChests(ModConfig config)
    {
        var location = Game1.currentLocation;
        var objects = location.objects.Values;
        var stackDistance = config.StackChestDistance > 0 ? config.StackChestDistance * config.StackChestDistance : int.MaxValue;

        var amount = 0;
        foreach (var storage in objects)
        {
            if (storage is not Chest chest) continue;
            var chestDistance = Vector2.DistanceSquared(chest.TileLocation, Game1.player.Tile);
            if (chestDistance > stackDistance) continue;

            foreach (var slot in Game1.player.Items)
            {
                if (slot == null) continue;

                var hasItem = chest.Items.Any((i) => i != null && i.ItemId == slot.ItemId && i.Quality == slot.Quality);
                if (!hasItem) continue;

                var before = slot.Stack;
                var result = chest.addItem(slot);
                amount += (before - (result?.Stack ?? 0));
                if (result == null) Game1.player.removeItemFromInventory(slot);
            }
        }

        if (amount < 1) return;

        stackMessage ??= new HUDMessage("") { noIcon = true, timeLeft = -1f, transparency = -1f };
        var wasRemoved = stackMessage.update(gameTimeZero);

        stackMessage.timeLeft = 1000f;
        stackMessage.transparency = 1f;
        stackMessage.message = $"{amount} Items Stored!";
        if (wasRemoved) Game1.addHUDMessage(stackMessage);
    }

    private static string GenerateEventsText(ModConfig? config)
    {
        if (config == null) return string.Empty;
        var builder = new StringBuilder();

        var luck = (int)MapRange(-0.1, 0.1, -100, 100, Game1.player.DailyLuck);
        builder.AppendLine($"{luck}% Luck");

        builder.AppendLine($"{Game1.weatherForTomorrow} Tomorrow");

        var recipe = GetTodaysRecipe();
        if (recipe != string.Empty && !Game1.player.knowsRecipe(recipe))
        {
            builder.AppendLine($"{recipe} TV Recipe");
        }

        var npc = Utility.getTodaysBirthdayNPC();
        if (npc != null)
        {
            var calendar = npc.GetData()?.Calendar;
            var show = calendar == CalendarBehavior.AlwaysShown ||
                (calendar == CalendarBehavior.HiddenUntilMet && Game1.player.friendshipData.ContainsKey(npc.Name));
            if (show)
            {
                builder.AppendLine($"{npc.Name} Birthday ({config.CalendarButton})");
            }
        }

        var quest = Game1.questOfTheDay;
        if (quest != null && quest.currentObjective != null && quest.currentObjective.Length > 0)
        {
            var quests = Game1.stats.Get("BillboardQuestsDone");
            builder.AppendLine($"New Board Quest!{(quests == 2 ? " =" : string.Empty)} ({config.BillboardButton})");
        }

        if (IsSpecialBoardAvailable())
        {
            builder.AppendLine($"New Special Quest! ({config.SpecialOrdersButton})");
        }

        if (IsSpecialBoardAvailable("Qi"))
        {
            builder.AppendLine($"New Qi Special Quest! ({config.QiOrdersButton})");
        }

        return builder.AppendLine().ToString();
    }

    private void UpdateMissingXp()
    {
        for (var i = 0; i < FARMER_SKILLS; ++i)
        {
            var lvl = Game1.player.GetSkillLevel(i);
            var lvlNext = Math.Min(lvl + 1, 10);
            var lvlCap = Farmer.getBaseExperienceForLevel(lvlNext);

            var lvlXp = Game1.player.experiencePoints[i];
            var lvlXpMissing = Math.Max(0, lvlCap - lvlXp);

            missingXp[i] = lvlXpMissing;
        }
    }

    private void UpdateSharedSkillXp()
    {
        for (var i = 0; i < FARMER_SKILLS; ++i)
        {
            var xp = Game1.player.experiencePoints[i];
            var gain = Math.Max(0, xp - currentXp[i]);
            currentXp[i] = xp;

            if (gain <= 0) continue;
            for (var k = 0; k < FARMER_SKILLS; ++k)
            {
                if (i == k) continue;
                Game1.player.gainExperience(k, gain);
                currentXp[k] = Game1.player.experiencePoints[k];
            }
        }
    }

    public static class Utils
    {
        public static void DrawBox(Rectangle box)
        {
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X + 32, box.Y + 32, box.Width - 64, box.Height - 64), new Rectangle(64, 128, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X, box.Y, 64, 64), new Rectangle(0, 0, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X + box.Width - 64, box.Y, 64, 64), new Rectangle(192, 0, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X + box.Width - 64, box.Y + box.Height - 64, 64, 64), new Rectangle(192, 192, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X, box.Y + box.Height - 64, 64, 64), new Rectangle(0, 192, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X + 64, box.Y, box.Width - 128, 64), new Rectangle(128, 0, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X + box.Width - 64, box.Y + 64, 64, box.Height - 128), new Rectangle(192, 128, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X + 64, box.Y + box.Height - 64, box.Width - 128, 64), new Rectangle(128, 192, 64, 64), Color.White);
            Game1.spriteBatch.Draw(Game1.menuTexture, new Rectangle(box.X, box.Y + 64, 64, box.Height - 128), new Rectangle(0, 128, 64, 64), Color.White);
        }
    }
}

