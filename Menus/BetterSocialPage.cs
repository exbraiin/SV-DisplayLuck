using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace Exbrain.DisplayLuck.Menus;

public class BetterSocialPage : IClickableMenu
{
    //
    // Summary:
    //     An entry on the social page.
    public class SocialEntry
    {
        //
        // Summary:
        //     The backing field for StardewValley.Menus.SocialPage.SocialEntry.IsMarriedToAnyone.
        private bool? CachedIsMarriedToAnyone;

        //
        // Summary:
        //     The character instance.
        public Character Character;

        //
        // Summary:
        //     The unique multiplayer ID for a player, or the internal name for an NPC.
        public readonly string InternalName;

        //
        // Summary:
        //     The translated display name.
        public readonly string DisplayName;

        //
        // Summary:
        //     Whether the current player has met this character.
        public readonly bool IsMet;

        //
        // Summary:
        //     Whether players can romance this character.
        public readonly bool IsDatable;

        //
        // Summary:
        //     How the NPC is shown on the social tab.
        public readonly SocialTabBehavior SocialTabBehavior;

        //
        // Summary:
        //     Whether this character is a child.
        public readonly bool IsChild;

        //
        // Summary:
        //     Whether this character is a player.
        public readonly bool IsPlayer;

        //
        // Summary:
        //     The character's gender identity.
        public readonly Gender Gender;

        //
        // Summary:
        //     The current player's heart level with this character.
        public readonly int HeartLevel;

        //
        // Summary:
        //     The current player's friendship data with the character, if any.
        public readonly Friendship Friendship;

        //
        // Summary:
        //     The NPC's character data, if applicable.
        public readonly CharacterData Data;

        //
        // Summary:
        //     The order in which the current player met this NPC, if applicable.
        public int? OrderMet;

        //
        // Summary:
        //     Construct an instance.
        //
        // Parameters:
        //   player:
        //     The player for which to create an entry.
        //
        //   friendship:
        //     The current player's friendship with this character.
        public SocialEntry(Farmer player, Friendship friendship)
        {
            Character = player;
            InternalName = player.UniqueMultiplayerID.ToString();
            DisplayName = player.Name;
            IsMet = true;
            IsPlayer = true;
            Gender = player.Gender;
            Friendship = friendship;
        }

        //
        // Summary:
        //     Construct an instance.
        //
        // Parameters:
        //   npc:
        //     The NPC for which to create an entry.
        //
        //   friendship:
        //     The current player's friendship with this character.
        //
        //   data:
        //     The NPC's character data, if applicable.
        //
        //   overrideDisplayName:
        //     The translated display name, or null to get it from npc.
        public SocialEntry(NPC npc, Friendship friendship, CharacterData data, string overrideDisplayName = null)
        {
            Character = npc;
            InternalName = npc.Name;
            DisplayName = overrideDisplayName ?? npc.displayName;
            IsMet = friendship != null || npc is Child;
            IsDatable = data?.CanBeRomanced ?? false;
            SocialTabBehavior = data?.SocialTab ?? SocialTabBehavior.AlwaysShown;
            IsChild = npc is Child;
            Gender = npc.Gender;
            HeartLevel = (friendship?.Points ?? 0) / 250;
            Friendship = friendship;
            Data = data;
        }

        //
        // Summary:
        //     Get whether the current player is dating this character.
        public bool IsDatingCurrentPlayer()
        {
            return Friendship?.IsDating() ?? false;
        }

        //
        // Summary:
        //     Get whether the current player is married to this character.
        public bool IsMarriedToCurrentPlayer()
        {
            return Friendship?.IsMarried() ?? false;
        }

        //
        // Summary:
        //     Get whether the current player is a roommate with this character.
        public bool IsRoommateForCurrentPlayer()
        {
            return Friendship?.IsRoommate() ?? false;
        }

        //
        // Summary:
        //     Get whether the current player is married to this character.
        public bool IsDivorcedFromCurrentPlayer()
        {
            return Friendship?.IsDivorced() ?? false;
        }

        //
        // Summary:
        //     Get whether this character is married to any player.
        public bool IsMarriedToAnyone()
        {
            if (!CachedIsMarriedToAnyone.HasValue)
            {
                if (IsMarriedToCurrentPlayer())
                {
                    CachedIsMarriedToAnyone = true;
                }
                else
                {
                    foreach (Farmer allFarmer in Game1.getAllFarmers())
                    {
                        if (allFarmer.spouse == InternalName && allFarmer.isMarriedOrRoommates())
                        {
                            CachedIsMarriedToAnyone = true;
                            break;
                        }
                    }

                    if (!CachedIsMarriedToAnyone.HasValue)
                    {
                        CachedIsMarriedToAnyone = false;
                    }
                }
            }

            return CachedIsMarriedToAnyone.Value;
        }
    }

    public const int slotsOnPage = 5;

    private string hoverText = "";

    private ClickableTextureComponent upButton;
    private ClickableTextureComponent downButton;
    private ClickableTextureComponent scrollBar;
    private Rectangle scrollBarRunner;
    private Rectangle _fullPageRect;
    private Rectangle _boardAreaRect;
    private Rectangle _scrollAreaRect;

    private readonly BetterNpcBoardPage _boardPage;

    //
    // Summary:
    //     The players and social NPCs shown in the list.
    public readonly List<SocialEntry> SocialEntries;

    //
    // Summary:
    //     The character portrait components.
    private readonly List<ClickableTextureComponent> sprites = new List<ClickableTextureComponent>();

    //
    // Summary:
    //     The index of the StardewValley.Menus.SocialPage.SocialEntries entry shown at
    //     the top of the scrolled view.
    private int slotPosition;

    //
    // Summary:
    //     The number of players shown in the list.
    private int numFarmers;

    //
    // Summary:
    //     The clickable slots over which character info is drawn.
    public readonly List<ClickableTextureComponent> characterSlots = new List<ClickableTextureComponent>();

    private bool scrolling;

    public BetterSocialPage(int x, int y, int width, int height)
        : base(x, y, width, height)
    {
        var dividerWidth = 12;
        _fullPageRect = new Rectangle(xPositionOnScreen + borderWidth, yPositionOnScreen + borderWidth + 64, width - borderWidth * 2, height - borderWidth * 2 - 64);
        _scrollAreaRect = new Rectangle(_fullPageRect.X, _fullPageRect.Y, 256, _fullPageRect.Height);
        _boardAreaRect = new Rectangle(_fullPageRect.X + 256 + dividerWidth, _fullPageRect.Y, _fullPageRect.Width - 256 - dividerWidth, _fullPageRect.Height);
        _boardPage = new BetterNpcBoardPage(_boardAreaRect);

        SocialEntries = FindSocialCharacters();
        numFarmers = SocialEntries.Count((p) => p.IsPlayer);
        CreateComponents();
        slotPosition = 0;
        for (int i = 0; i < SocialEntries.Count; i++)
        {
            if (!SocialEntries[i].IsPlayer)
            {
                slotPosition = i;
                break;
            }
        }

        setScrollBarToCurrentIndex();
        UpdateSlots();
    }

    //
    // Summary:
    //     Find all social NPCs which should be shown on the social page.
    public List<SocialEntry> FindSocialCharacters()
    {
        List<SocialEntry> list = new List<SocialEntry>();
        Dictionary<string, SocialEntry> dictionary = new Dictionary<string, SocialEntry>();
        List<SocialEntry> list2 = new List<SocialEntry>();
        foreach (NPC allNpc in GetAllNpcs())
        {
            if (!Game1.player.friendshipData.TryGetValue(allNpc.Name, out var value))
            {
                value = null;
            }

            if (allNpc is Child)
            {
                list2.Add(new SocialEntry(allNpc, value, null, allNpc.displayName));
            }
            else
            {
                if (!allNpc.CanSocialize)
                {
                    continue;
                }

                CharacterData data = allNpc.GetData();
                string overrideDisplayName = allNpc.displayName;
                switch (data?.SocialTab)
                {
                    case SocialTabBehavior.HiddenUntilMet:
                        if (value == null)
                        {
                            continue;
                        }

                        break;
                    case SocialTabBehavior.UnknownUntilMet:
                        if (value == null)
                        {
                            overrideDisplayName = "???";
                        }

                        break;
                    case SocialTabBehavior.AlwaysShown:
                        if (value == null)
                        {
                            Game1.player.friendshipData.Add(allNpc.Name, value = new Friendship());
                        }

                        break;
                    case SocialTabBehavior.HiddenAlways:
                        continue;
                }

                dictionary[allNpc.Name] = new SocialEntry(allNpc, value, data, overrideDisplayName);
            }
        }

        int num = 0;
        foreach (KeyValuePair<string, Friendship> pair in Game1.player.friendshipData.Pairs)
        {
            if (dictionary.TryGetValue(pair.Key, out var value2))
            {
                value2.OrderMet = num++;
            }
        }

        foreach (Farmer allFarmer in Game1.getAllFarmers())
        {
            if (!allFarmer.IsLocalPlayer && (allFarmer.IsMainPlayer || allFarmer.isCustomized.Value))
            {
                Friendship friendship = Game1.player.team.GetFriendship(Game1.player.UniqueMultiplayerID, allFarmer.UniqueMultiplayerID);
                list.Add(new SocialEntry(allFarmer, friendship));
            }
        }

        List<SocialEntry> list3 = new List<SocialEntry>();
        list3.AddRange(list);
        list3.AddRange(from entry in dictionary.Values
                       orderby entry.Friendship?.Points descending, entry.OrderMet, entry.DisplayName
                       select entry);
        list3.AddRange(list2.OrderBy((p) => p.DisplayName));
        return list3;
    }

    //
    // Summary:
    //     Get all child or villager NPCs from the world and friendship data.
    public IEnumerable<NPC> GetAllNpcs()
    {
        HashSet<string> nonSocial = new HashSet<string>();
        Dictionary<string, NPC> found = new Dictionary<string, NPC>();
        Utility.ForEachCharacter(delegate (NPC npc)
        {
            if (npc is Child)
            {
                found[npc.Name + "$$child"] = npc;
            }
            else if (npc.IsVillager)
            {
                NPC value;
                if (!npc.CanSocialize)
                {
                    nonSocial.Add(npc.Name);
                }
                else if (found.TryGetValue(npc.Name, out value) && npc != value)
                {
                    bool flag = true;
                    if (Game1.IsClient)
                    {
                        bool num = value.currentLocation.IsActiveLocation();
                        bool flag2 = npc.currentLocation.IsActiveLocation();
                        if (num != flag2)
                        {
                            if (flag2)
                            {
                                found[npc.Name] = npc;
                            }

                            flag = false;
                        }
                    }

                    if (flag)
                    {
                        Console.WriteLine($"The social page found conflicting NPCs with name {npc.Name} (one at {value.currentLocation?.NameOrUniqueName} {value.TilePoint}, the other at {npc.currentLocation?.NameOrUniqueName} {npc.TilePoint}); only the first will be shown.");
                    }
                }
                else
                {
                    found[npc.Name] = npc;
                }
            }

            return true;
        });
        Event @event = Game1.currentLocation?.currentEvent;
        if (@event != null)
        {
            foreach (NPC actor in @event.actors)
            {
                if (actor.IsVillager && actor.CanSocialize)
                {
                    found[actor.Name] = actor;
                }
            }
        }

        foreach (string key in Game1.player.friendshipData.Keys)
        {
            if (nonSocial.Contains(key) || found.ContainsKey(key) || !NPC.TryGetData(key, out var _))
            {
                continue;
            }

            string textureNameForCharacter = NPC.getTextureNameForCharacter(key);
            string text = "Characters\\" + textureNameForCharacter;
            string assetName = "Portraits\\" + textureNameForCharacter;
            if (Game1.content.DoesAssetExist<Texture2D>(text) && Game1.content.DoesAssetExist<Texture2D>(assetName))
            {
                try
                {
                    AnimatedSprite sprite = new AnimatedSprite(text, 0, 16, 32);
                    Texture2D portrait = Game1.content.Load<Texture2D>(assetName);
                    found[key] = new NPC(sprite, Vector2.Zero, "Town", 0, key, portrait, eventActor: false);
                }
                catch
                {
                }
            }
        }

        return found.Values;
    }

    //
    // Summary:
    //     Load the clickable components to display.
    public void CreateComponents()
    {
        var hasEntry = false;
        sprites.Clear();
        characterSlots.Clear();
        for (int i = 0; i < SocialEntries.Count; i++)
        {
            sprites.Add(CreateSpriteComponent(SocialEntries[i], i));
            var clickableTextureComponent = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + borderWidth, 0, 256 + 12, rowPosition(1) - rowPosition(0)), null, new Rectangle(0, 0, 0, 0), 4f)
            {
                myID = i,
                downNeighborID = i + 1,
                upNeighborID = i - 1
            };
            if (clickableTextureComponent.upNeighborID < 0)
            {
                clickableTextureComponent.upNeighborID = 12342;
            }

            characterSlots.Add(clickableTextureComponent);
            if (!hasEntry && IsCharacterSlotClickable(i))
            {
                hasEntry = true;
                _boardPage.SetEntry(SocialEntries[i]);
            }
        }

        upButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        downButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        scrollBar = new ClickableTextureComponent(new Rectangle(upButton.bounds.X + 12, upButton.bounds.Y + upButton.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        scrollBarRunner = new Rectangle(scrollBar.bounds.X, upButton.bounds.Y + upButton.bounds.Height + 4, scrollBar.bounds.Width, height - 128 - upButton.bounds.Height - 8);
    }

    //
    // Summary:
    //     Create the clickable texture component for a character's portrait.
    //
    // Parameters:
    //   entry:
    //     The social character to render.
    //
    //   index:
    //     The index in the list of entries.
    public ClickableTextureComponent CreateSpriteComponent(SocialEntry entry, int index)
    {
        Rectangle bounds = new Rectangle(xPositionOnScreen + IClickableMenu.borderWidth + 4, 0, width, 64);
        Rectangle sourceRect = !entry.IsPlayer && entry.Character is NPC nPC ? nPC.getMugShotSourceRect() : Rectangle.Empty;
        return new ClickableTextureComponent(index.ToString(), bounds, null, "", entry.Character.Sprite.Texture, sourceRect, 4f);
    }

    //
    // Summary:
    //     Get the social entry from its index in the list.
    //
    // Parameters:
    //   index:
    //     The index in the social list.
    public SocialEntry GetSocialEntry(int index)
    {
        if (index < 0 || index >= SocialEntries.Count)
        {
            index = 0;
        }

        return SocialEntries[index];
    }

    public override void snapToDefaultClickableComponent()
    {
        if (slotPosition < characterSlots.Count)
        {
            currentlySnappedComponent = characterSlots[slotPosition];
        }

        snapCursorToCurrentSnappedComponent();
    }

    public void UpdateSlots()
    {
        for (int i = 0; i < characterSlots.Count; i++)
        {
            characterSlots[i].bounds.Y = rowPosition(i - 1);
        }

        int num = 0;
        for (int j = slotPosition; j < slotPosition + 5; j++)
        {
            if (sprites.Count > j)
            {
                int y = yPositionOnScreen + IClickableMenu.borderWidth + 32 + 112 * num + 32;
                sprites[j].bounds.Y = y;
            }

            num++;
        }

        populateClickableComponentList();
        addTabsToClickableComponents();
    }

    public void addTabsToClickableComponents()
    {
        if (Game1.activeClickableMenu is GameMenu gameMenu && !allClickableComponents.Contains(gameMenu.tabs[0]))
        {
            allClickableComponents.AddRange(gameMenu.tabs);
        }
    }

    protected void _SelectSlot(SocialEntry entry)
    {
        bool flag = false;
        for (int i = 0; i < SocialEntries.Count; i++)
        {
            SocialEntry socialEntry = SocialEntries[i];
            if (socialEntry.InternalName == entry.InternalName && socialEntry.IsPlayer == entry.IsPlayer && socialEntry.IsChild == entry.IsChild)
            {
                _SelectSlot(characterSlots[i]);
                flag = true;
                break;
            }
        }

        if (!flag)
        {
            _SelectSlot(characterSlots[0]);
        }
    }

    protected void _SelectSlot(ClickableComponent slot_component)
    {
        if (slot_component != null && characterSlots.Contains(slot_component))
        {
            int num = characterSlots.IndexOf(slot_component as ClickableTextureComponent);
            currentlySnappedComponent = slot_component;
            if (num < slotPosition)
            {
                slotPosition = num;
            }
            else if (num >= slotPosition + 5)
            {
                slotPosition = num - 5 + 1;
            }

            setScrollBarToCurrentIndex();
            UpdateSlots();
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                snapCursorToCurrentSnappedComponent();
            }
        }
    }

    public void ConstrainSelectionToVisibleSlots()
    {
        if (characterSlots.Contains(currentlySnappedComponent))
        {
            int v = characterSlots.IndexOf(currentlySnappedComponent as ClickableTextureComponent);
            int num = v;
            if (num < slotPosition)
            {
                num = slotPosition;
            }
            else if (num >= slotPosition + 5)
            {
                num = slotPosition + 5 - 1;
            }

            currentlySnappedComponent = characterSlots[num];
            if (Game1.options.snappyMenus && Game1.options.gamepadControls)
            {
                snapCursorToCurrentSnappedComponent();
            }
        }
    }

    public override void snapCursorToCurrentSnappedComponent()
    {
        if (currentlySnappedComponent != null && characterSlots.Contains(currentlySnappedComponent))
        {
            Game1.setMousePosition(currentlySnappedComponent.bounds.Left + 64, currentlySnappedComponent.bounds.Center.Y);
        }
        else
        {
            base.snapCursorToCurrentSnappedComponent();
        }
    }

    public override void applyMovementKey(int direction)
    {
        base.applyMovementKey(direction);
        if (characterSlots.Contains(currentlySnappedComponent))
        {
            _SelectSlot(currentlySnappedComponent);
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (scrolling)
        {
            int y2 = scrollBar.bounds.Y;
            scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + upButton.bounds.Height + 20));
            float num = (y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
            slotPosition = Math.Min(sprites.Count - 5, Math.Max(0, (int)(sprites.Count * num)));
            setScrollBarToCurrentIndex();
            if (y2 != scrollBar.bounds.Y)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        scrolling = false;
    }

    private void setScrollBarToCurrentIndex()
    {
        if (sprites.Count > 0)
        {
            scrollBar.bounds.Y = scrollBarRunner.Height / Math.Max(1, sprites.Count - 5 + 1) * slotPosition + upButton.bounds.Bottom + 4;
            if (slotPosition == sprites.Count - 5)
            {
                scrollBar.bounds.Y = downButton.bounds.Y - scrollBar.bounds.Height - 4;
            }
        }

        UpdateSlots();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction > 0 && slotPosition > 0)
        {
            upArrowPressed();
            ConstrainSelectionToVisibleSlots();
            Game1.playSound("shiny4");
        }
        else if (direction < 0 && slotPosition < Math.Max(0, sprites.Count - 5))
        {
            downArrowPressed();
            ConstrainSelectionToVisibleSlots();
            Game1.playSound("shiny4");
        }
    }

    public void upArrowPressed()
    {
        slotPosition--;
        UpdateSlots();
        upButton.scale = 3.5f;
        setScrollBarToCurrentIndex();
    }

    public void downArrowPressed()
    {
        slotPosition++;
        UpdateSlots();
        downButton.scale = 3.5f;
        setScrollBarToCurrentIndex();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upButton.containsPoint(x, y) && slotPosition > 0)
        {
            upArrowPressed();
            Game1.playSound("shwip");
            return;
        }

        if (downButton.containsPoint(x, y) && slotPosition < sprites.Count - 5)
        {
            downArrowPressed();
            Game1.playSound("shwip");
            return;
        }

        if (scrollBar.containsPoint(x, y))
        {
            scrolling = true;
            return;
        }

        if (!downButton.containsPoint(x, y) && x > xPositionOnScreen + width && x < xPositionOnScreen + width + 128 && y > yPositionOnScreen && y < yPositionOnScreen + height)
        {
            scrolling = true;
            leftClickHeld(x, y);
            releaseLeftClick(x, y);
            return;
        }

        for (int i = 0; i < characterSlots.Count; i++)
        {
            if (i < slotPosition || i >= slotPosition + 5 || !characterSlots[i].bounds.Contains(x, y))
            {
                continue;
            }

            var socialEntry = GetSocialEntry(i);
            if (!socialEntry.IsPlayer && !socialEntry.IsChild)
            {
                Character character = socialEntry.Character;
                if (Game1.player.friendshipData.ContainsKey(character.Name))
                {
                    _boardPage.SetEntry(socialEntry);
                    /*
                    Game1.playSound("bigSelect");
                    int cached_slot_position = slotPosition;
                    ProfileMenu profileMenu = new ProfileMenu(socialEntry, SocialEntries);
                    profileMenu.exitFunction = delegate
                    {
                        if (((GameMenu)(Game1.activeClickableMenu = new GameMenu(GameMenu.socialTab, -1, playOpeningSound: false))).GetCurrentPage() is BetterSocialPage socialPage)
                        {
                            socialPage.slotPosition = cached_slot_position;
                            socialPage._SelectSlot(profileMenu.Current);
                        }
                    };
                    Game1.activeClickableMenu = profileMenu;
                    if (Game1.options.SnappyMenus)
                    {
                        profileMenu.snapToDefaultClickableComponent();
                    }
                    */
                    return;
                }
            }

            Game1.playSound("shiny4");
            break;
        }

        slotPosition = Math.Max(0, Math.Min(sprites.Count - 5, slotPosition));
    }

    public override void performHoverAction(int x, int y)
    {
        hoverText = "";
        upButton.tryHover(x, y);
        downButton.tryHover(x, y);
    }

    private bool IsCharacterSlotClickable(int i)
    {
        SocialEntry socialEntry = GetSocialEntry(i);
        if (socialEntry != null && !socialEntry.IsPlayer && !socialEntry.IsChild)
        {
            return socialEntry.IsMet;
        }
        return false;
    }

    private void DrawNPCSlot(SpriteBatch b, int i)
    {
        SocialEntry socialEntry = GetSocialEntry(i);
        if (socialEntry == null) return;

        if (IsCharacterSlotClickable(i) && characterSlots[i].bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
        {
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + borderWidth - 4, sprites[i].bounds.Y - 4, characterSlots[i].bounds.Width, characterSlots[i].bounds.Height - 12), Color.White * 0.25f);
        }

        sprites[i].draw(b);
        string internalName = socialEntry.InternalName;
        Gender gender = socialEntry.Gender;
        bool isDatable = socialEntry.IsDatable;
        bool isDating = socialEntry.IsDatingCurrentPlayer();
        bool isMarried = socialEntry.IsMarriedToCurrentPlayer();
        bool isRoommate = socialEntry.IsRoommateForCurrentPlayer();
        float y = Game1.smallFont.MeasureString("W").Y;
        float num = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko ? (0f - y) / 2f : 0f;
        b.DrawString(Game1.dialogueFont, socialEntry.DisplayName, new Vector2((float)(xPositionOnScreen + IClickableMenu.borderWidth * 3 / 2 + 64 - 20 + 96) - Game1.dialogueFont.MeasureString(socialEntry.DisplayName).X / 2f, (float)(sprites[i].bounds.Y + 48) + num - (float)(isDatable ? 24 : 20)), Game1.textColor);
        return;

        for (int j = 0; j < Math.Max(Utility.GetMaximumHeartsForCharacter(Game1.getCharacterFromName(internalName)), 10); j++)
        {
            drawNPCSlotHeart(b, i, socialEntry, j, isDating, isMarried);
        }

        if (isDatable || isRoommate)
        {
            string text = !Game1.content.ShouldUseGenderedCharacterTranslations() ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11635") : gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11635").Split('/')[0] : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11635").Split('/').Last();
            if (isRoommate)
            {
                text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Housemate");
            }
            else if (isMarried)
            {
                text = gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11636") : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11637");
            }
            else if (socialEntry.IsMarriedToAnyone())
            {
                text = gender == Gender.Male ? Game1.content.LoadString("Strings\\UI:SocialPage_MarriedToOtherPlayer_MaleNPC") : Game1.content.LoadString("Strings\\UI:SocialPage_MarriedToOtherPlayer_FemaleNPC");
            }
            else if (!Game1.player.isMarriedOrRoommates() && isDating)
            {
                text = gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11639") : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11640");
            }
            else if (socialEntry.IsDivorcedFromCurrentPlayer())
            {
                text = gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11642") : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11643");
            }

            int num2 = (IClickableMenu.borderWidth * 3 + 128 - 40 + 192) / 2;
            text = Game1.parseText(text, Game1.smallFont, num2);
            Vector2 vector = Game1.smallFont.MeasureString(text);
            b.DrawString(Game1.smallFont, text, new Vector2((float)(xPositionOnScreen + 192 + 8) - vector.X / 2f, (float)sprites[i].bounds.Bottom - (vector.Y - y)), Game1.textColor);
        }

        if (!isMarried && !socialEntry.IsChild)
        {
            Utility.drawWithShadow(b, Game1.mouseCursors2, new Vector2(xPositionOnScreen + 384 + 304, sprites[i].bounds.Y - 4), new Rectangle(166, 174, 14, 12), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.88f, 0, -1, 0.2f);
            Texture2D mouseCursors = Game1.mouseCursors;
            Vector2 position = new Vector2(xPositionOnScreen + 384 + 296, sprites[i].bounds.Y + 32 + 20);
            Friendship friendship = socialEntry.Friendship;
            b.Draw(mouseCursors, position, new Rectangle(227 + (friendship != null && friendship.GiftsThisWeek >= 2 ? 9 : 0), 425, 9, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
            Texture2D mouseCursors2 = Game1.mouseCursors;
            Vector2 position2 = new Vector2(xPositionOnScreen + 384 + 336, sprites[i].bounds.Y + 32 + 20);
            Friendship friendship2 = socialEntry.Friendship;
            b.Draw(mouseCursors2, position2, new Rectangle(227 + (friendship2 != null && friendship2.GiftsThisWeek >= 1 ? 9 : 0), 425, 9, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
            Utility.drawWithShadow(b, Game1.mouseCursors2, new Vector2(xPositionOnScreen + 384 + 424, sprites[i].bounds.Y), new Rectangle(180, 175, 13, 11), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 0.88f, 0, -1, 0.2f);
            Texture2D mouseCursors3 = Game1.mouseCursors;
            Vector2 position3 = new Vector2(xPositionOnScreen + 384 + 432, sprites[i].bounds.Y + 32 + 20);
            Friendship friendship3 = socialEntry.Friendship;
            b.Draw(mouseCursors3, position3, new Rectangle(227 + (friendship3 != null && friendship3.TalkedToToday ? 9 : 0), 425, 9, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        }

        if (isMarried)
        {
            if (!isRoommate || internalName == "Krobus")
            {
                b.Draw(Game1.objectSpriteSheet, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth * 7 / 4 + 192, sprites[i].bounds.Y), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, isRoommate ? 808 : 460, 16, 16), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.88f);
            }
        }
        else if (isDating)
        {
            b.Draw(Game1.objectSpriteSheet, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth * 7 / 4 + 192, sprites[i].bounds.Y), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, isRoommate ? 808 : 458, 16, 16), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.88f);
        }
    }

    private void drawNPCSlotHeart(SpriteBatch b, int npcIndex, SocialEntry entry, int hearts, bool isDating, bool isCurrentSpouse)
    {
        bool flag = entry.IsDatable && !isDating && !isCurrentSpouse && hearts >= 8;
        int x = hearts < entry.HeartLevel || flag ? 211 : 218;
        Color color = hearts < 10 && flag ? Color.Black * 0.35f : Color.White;
        if (hearts < 10)
        {
            b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen + 320 - 4 + hearts * 32, sprites[npcIndex].bounds.Y + 64 - 28), new Rectangle(x, 428, 7, 6), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        }
        else
        {
            b.Draw(Game1.mouseCursors, new Vector2(xPositionOnScreen + 320 - 4 + (hearts - 10) * 32, sprites[npcIndex].bounds.Y + 64), new Rectangle(x, 428, 7, 6), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        }
    }



    private int rowPosition(int i)
    {
        int num = i - slotPosition;
        int num2 = 112;
        return yPositionOnScreen + IClickableMenu.borderWidth + 160 + 4 + num * num2;
    }

    private void drawFarmerSlot(SpriteBatch b, int i)
    {
        SocialEntry socialEntry = GetSocialEntry(i);
        if (socialEntry == null)
        {
            return;
        }

        if (!socialEntry.IsPlayer)
        {
            Console.WriteLine($"Social page can't draw farmer slot for index {i}: this is NPC '{socialEntry.InternalName}', not a farmer.");
            return;
        }

        Farmer farmer = (Farmer)socialEntry.Character;
        Gender gender = socialEntry.Gender;
        ClickableTextureComponent clickableTextureComponent = sprites[i];
        int x = clickableTextureComponent.bounds.X;
        int y = clickableTextureComponent.bounds.Y;
        Rectangle scissorRectangle = b.GraphicsDevice.ScissorRectangle;
        Rectangle scissorRectangle2 = scissorRectangle;
        scissorRectangle2.Height = Math.Min(scissorRectangle2.Bottom, rowPosition(i)) - scissorRectangle2.Y - 4;
        b.GraphicsDevice.ScissorRectangle = scissorRectangle2;
        FarmerRenderer.isDrawingForUI = true;
        try
        {
            var bathingClothes = farmer.bathingClothes.Value;
            farmer.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(bathingClothes ? 108 : 0, 0, secondaryArm: false, flip: false), bathingClothes ? 108 : 0, new Rectangle(0, bathingClothes ? 576 : 0, 16, 32), new Vector2(x, y), Vector2.Zero, 0.8f, 2, Color.White, 0f, 1f, farmer);
        }
        finally
        {
            b.GraphicsDevice.ScissorRectangle = scissorRectangle;
        }

        FarmerRenderer.isDrawingForUI = false;
        bool num = socialEntry.IsMarriedToCurrentPlayer();
        float y2 = Game1.smallFont.MeasureString("W").Y;
        float num2 = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru ? (0f - y2) / 2f : 0f;
        b.DrawString(Game1.dialogueFont, farmer.Name, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth * 3 / 2 + 96 - 20, (float)(sprites[i].bounds.Y + 48) + num2 - 24f), Game1.textColor);
        string text = !Game1.content.ShouldUseGenderedCharacterTranslations() ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11635") : gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11635").Split('/')[0] : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11635").Split('/').Last();
        if (num)
        {
            text = gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11636") : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11637");
        }
        else if (farmer.isMarriedOrRoommates() && !farmer.hasRoommate())
        {
            text = gender == Gender.Male ? Game1.content.LoadString("Strings\\UI:SocialPage_MarriedToOtherPlayer_MaleNPC") : Game1.content.LoadString("Strings\\UI:SocialPage_MarriedToOtherPlayer_FemaleNPC");
        }
        else if (!Game1.player.isMarriedOrRoommates() && socialEntry.IsDatingCurrentPlayer())
        {
            text = gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11639") : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11640");
        }
        else if (socialEntry.IsDivorcedFromCurrentPlayer())
        {
            text = gender == Gender.Male ? Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11642") : Game1.content.LoadString("Strings\\StringsFromCSFiles:SocialPage.cs.11643");
        }

        text = Game1.parseText(width: (IClickableMenu.borderWidth * 3 + 128 - 40 + 192) / 2, text: text, whichFont: Game1.smallFont);
        Vector2 vector = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2((float)(xPositionOnScreen + 192 + 8) - vector.X / 2f, (float)sprites[i].bounds.Bottom - (vector.Y - y2)), Game1.textColor);
        if (num)
        {
            b.Draw(Game1.objectSpriteSheet, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth * 7 / 4 + 192, sprites[i].bounds.Y), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 801, 16, 16), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.88f);
        }
        else if (socialEntry.IsDatingCurrentPlayer())
        {
            b.Draw(Game1.objectSpriteSheet, new Vector2(xPositionOnScreen + IClickableMenu.borderWidth * 7 / 4 + 192, sprites[i].bounds.Y), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 458, 16, 16), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.88f);
        }
    }

    public override void update(GameTime time)
    {
        _boardPage.Update(time);
    }

    public override void draw(SpriteBatch b)
    {
        b.End();
        b.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);

        _boardPage.Draw(b);

        void DrawHorizontalLine(int dy)
        {
            var color = Color.White;
            var texture = Game1.menuTexture;
            var y = yPositionOnScreen + borderWidth;
            var w = 256 + 12;
            b.Draw(texture, new Rectangle(xPositionOnScreen + 32, y + dy, w, 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 25), color);
        }

        // Horizontal lines
        DrawHorizontalLine(128 + 4);
        DrawHorizontalLine(192 + 32 + 20);
        DrawHorizontalLine(320 + 36);
        DrawHorizontalLine(384 + 32 + 52);

        // drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 128 + 4, small: true);
        // drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 192 + 32 + 20, small: true);
        // drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 320 + 36, small: true);
        // drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 384 + 32 + 52, small: true);

        for (int i = slotPosition; i < slotPosition + 5 && i < sprites.Count; i++)
        {
            SocialEntry socialEntry = GetSocialEntry(i);
            if (socialEntry != null)
            {
                if (socialEntry.IsPlayer)
                {
                    drawFarmerSlot(b, i);
                }
                else
                {
                    DrawNPCSlot(b, i);
                }
            }
        }

        Rectangle scissorRectangle = b.GraphicsDevice.ScissorRectangle;
        Rectangle scissorRectangle2 = scissorRectangle;
        scissorRectangle2.Y = Math.Max(0, rowPosition(numFarmers - 1));
        scissorRectangle2.Height -= scissorRectangle2.Y;
        if (scissorRectangle2.Height > 0)
        {
            b.GraphicsDevice.ScissorRectangle = scissorRectangle2;
            try
            {
                drawVerticalPartition(b, xPositionOnScreen + 256 + 12, small: true);
                // drawVerticalPartition(b, xPositionOnScreen + 384 + 368, small: true);
                // drawVerticalPartition(b, xPositionOnScreen + 256 + 12 + 352, small: true);
            }
            finally
            {
                b.GraphicsDevice.ScissorRectangle = scissorRectangle;
            }
        }

        upButton.draw(b);
        downButton.draw(b);
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollBarRunner.X, scrollBarRunner.Y, scrollBarRunner.Width, scrollBarRunner.Height, Color.White, 4f);
        scrollBar.draw(b);
        if (!hoverText.Equals(""))
        {
            drawHoverText(b, hoverText, Game1.smallFont);
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    public class BetterNpcBoardPage
    {
        private SocialEntry? socialEntry;
        private AnimatedSprite? _animatedSprite;

        private int _currentDirection;
        private int _characterSpriteRandomInt;
        private float _directionChangeTimer;
        private float _hiddenEmoteTimer = -1f;

        private readonly Rectangle rectangle;

        public BetterNpcBoardPage(Rectangle rect)
        {
            rectangle = rect;
        }

        public void SetEntry(SocialEntry entry)
        {
            socialEntry = entry;
            if (entry.Character is not NPC npc) return;

            _animatedSprite = npc.Sprite.Clone();
            _animatedSprite.tempSpriteHeight = -1;
            _animatedSprite.faceDirection(2);

            _currentDirection = 2;
            _hiddenEmoteTimer = -1f;
            _directionChangeTimer = 2000f;
        }

        public void PlayHiddenEmote()
        {
            if (socialEntry == null) return;
            if (socialEntry.HeartLevel >= 4)
            {
                _currentDirection = 2;
                _characterSpriteRandomInt = Game1.random.Next(4);
                CharacterData data = socialEntry.Data;
                Game1.playSound(data?.HiddenProfileEmoteSound ?? "drumkit6");
                _hiddenEmoteTimer = ((data != null && data.HiddenProfileEmoteDuration >= 0) ? ((float)data.HiddenProfileEmoteDuration) : 4000f);
            }
            else
            {
                _currentDirection = 2;
                _directionChangeTimer = 5000f;
                Game1.playSound("Cowboy_Footstep");
            }
        }

        public void Update(GameTime time)
        {
            AnimateSprite(time);
        }

        private void AnimateSprite(GameTime time)
        {
            if (socialEntry == null || _animatedSprite == null) return;

            if (_hiddenEmoteTimer > 0f)
            {
                _hiddenEmoteTimer -= time.ElapsedGameTime.Milliseconds;
                if (_hiddenEmoteTimer <= 0f)
                {
                    _hiddenEmoteTimer = -1f;
                    _currentDirection = 2;
                    _directionChangeTimer = 2000f;
                    if (socialEntry.InternalName == "Leo")
                    {
                        socialEntry.Character.Sprite.AnimateDown(time);
                    }
                }
            }
            else if (_directionChangeTimer > 0f)
            {
                _directionChangeTimer -= time.ElapsedGameTime.Milliseconds;
                if (_directionChangeTimer <= 0f)
                {
                    _directionChangeTimer = 2000f;
                    _currentDirection = (_currentDirection + 1) % 4;
                }
            }

            if (_hiddenEmoteTimer > 0f)
            {
                CharacterData data = socialEntry.Data;
                if (data != null && data.HiddenProfileEmoteStartFrame >= 0)
                {
                    int startFrame = ((socialEntry.InternalName == "Emily" && data.HiddenProfileEmoteStartFrame == 16) ? (data.HiddenProfileEmoteStartFrame + _characterSpriteRandomInt * 2) : data.HiddenProfileEmoteStartFrame);
                    _animatedSprite.Animate(time, startFrame, data.HiddenProfileEmoteFrameCount, data.HiddenProfileEmoteFrameDuration);
                }
                else
                {
                    _animatedSprite.AnimateDown(time, 2);
                }

                return;
            }

            switch (_currentDirection)
            {
                case 0:
                    _animatedSprite.AnimateUp(time, 2);
                    break;
                case 2:
                    _animatedSprite.AnimateDown(time, 2);
                    break;
                case 3:
                    _animatedSprite.AnimateLeft(time, 2);
                    break;
                case 1:
                    _animatedSprite.AnimateRight(time, 2);
                    break;
            }
        }

        public void Draw(SpriteBatch b)
        {
            if (socialEntry == null) return;

            /*
            var text = rectangle.ToString();
            var layout = Game1.tinyFont.MeasureString(text);
            var vec = new Vector2(rectangle.X, rectangle.Y + rectangle.Width - layout.Y);
            b.DrawString(Game1.tinyFont, text, vec, Color.Black);
            */

            // sprites[i].draw(b);
            string internalName = socialEntry.InternalName;
            Gender gender = socialEntry.Gender;
            bool isDatable = socialEntry.IsDatable;
            bool isDating = socialEntry.IsDatingCurrentPlayer();
            bool isMarried = socialEntry.IsMarriedToCurrentPlayer();
            bool isRoommate = socialEntry.IsRoommateForCurrentPlayer();
            float y = Game1.smallFont.MeasureString("W").Y;
            float num = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko ? (0f - y) / 2f : 0f;

            // b.Draw(Game1.staminaRect, _fullPageRect, Color.Yellow * 0.6f);
            // b.Draw(Game1.staminaRect, _scrollAreaRect, Color.Blue * 0.6f);
            b.Draw(Game1.staminaRect, rectangle, Color.Red * 0.6f);

            var bounds = new Rectangle(rectangle.Location + new Point(8, 8), rectangle.Size - new Point(16, 16));
            b.Draw(Game1.staminaRect, bounds, Color.Yellow * 0.6f);

            if (socialEntry.Character is NPC npc)
            {
                var charBgPos = bounds.Location.ToVector2();
                b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, charBgPos, Color.White);

                if (_animatedSprite != null)
                {
                    var screenPosition = new Vector2(charBgPos.X + (Game1.daybg.Width - _animatedSprite.SpriteWidth * 4) / 2, charBgPos.Y + 32f + (32 - _animatedSprite.SpriteHeight) * 4);
                    _animatedSprite.draw(b, screenPosition, 0.8f);
                }
            }

            // Draw character name...
            var remainWidth = bounds.Width - Game1.daybg.Width - 8;
            var nameScrollPos = new Point(bounds.X + Game1.daybg.Width + 8 + remainWidth / 2, bounds.Y);
            SpriteText.drawStringWithScrollCenteredAt(b, socialEntry.DisplayName, nameScrollPos.X, nameScrollPos.Y);

            // Draw character gifts...
            var scrollHeight = 18 * 4;
            var giftsBox = new Rectangle(bounds.X + Game1.daybg.Width + 8, bounds.Y + scrollHeight + 8, remainWidth, bounds.Height - scrollHeight - 8);
            b.Draw(Game1.staminaRect, giftsBox, Color.Blue);

            if (socialEntry.Character is NPC npc2)
            {
                int numHearts = Math.Max(10, Utility.GetMaximumHeartsForCharacter(npc2));
                var heartsPerRow = (int)MathF.Ceiling(numHearts / 2f);

                var heartsWidth = 32 * heartsPerRow;
                var heartsOffsetX = (giftsBox.Width - heartsWidth) / 2;
                float heartDrawStartX = giftsBox.X + heartsOffsetX;

                for (int i = 0; i < numHearts; i++)
                {
                    var hx = heartDrawStartX + (i % heartsPerRow) * 32;
                    var hy = giftsBox.Y + (i / heartsPerRow) * 32;
                    DrawNPCSlotHeart(b, hx, hy, socialEntry, i, isDating, isMarried);
                }
            }
        }

        private void DrawNPCSlotHeart(SpriteBatch b, float heartDrawStartX, float heartDrawStartY, SocialEntry entry, int hearts, bool isDating, bool isCurrentSpouse)
        {
            bool flag = entry.IsDatable && !isDating && !isCurrentSpouse && hearts >= 8;
            int x = ((hearts < entry.HeartLevel || flag) ? 211 : 218);
            Color color = ((hearts < 10 && flag) ? (Color.Black * 0.35f) : Color.White);
            b.Draw(Game1.mouseCursors, new Vector2(heartDrawStartX, heartDrawStartY), new Rectangle(x, 428, 7, 6), color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        }
    }
}
