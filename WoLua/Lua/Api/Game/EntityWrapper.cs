using System;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;

using Lumina.Excel.GeneratedSheets;

using MoonSharp.Interpreter;

using PrincessRTFM.WoLua.Constants;
using PrincessRTFM.WoLua.Lua.Docs;

using CharacterData = FFXIVClientStructs.FFXIV.Client.Game.Character.CharacterData;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace PrincessRTFM.WoLua.Lua.Api.Game;

[MoonSharpUserData]
[MoonSharpHideMember(nameof(Entity))]
[MoonSharpHideMember(nameof(Equals))]
[MoonSharpHideMember("<Clone>$")]
[MoonSharpHideMember(nameof(Deconstruct))]
public sealed record class EntityWrapper(GameObject? Entity): IEquatable<EntityWrapper> { // TODO luadoc all of this
	public static readonly EntityWrapper Empty = new((GameObject?)null);

	#region Conversions
	private unsafe NativeGameObject* go => this ? (NativeGameObject*)this.Entity!.Address : null;
	private unsafe NativeCharacter* cs => this.IsPlayer ? (NativeCharacter*)this.Entity!.Address : null;

	public static implicit operator GameObject?(EntityWrapper? wrapper) => wrapper?.Entity;
	public static implicit operator EntityWrapper(GameObject? entity) => new(entity);

	public static implicit operator bool(EntityWrapper? entity) => entity?.Exists ?? false;
	#endregion

	public bool Exists => this.Entity is not null && this.Entity.IsValid() && this.Entity.ObjectKind is not ObjectKind.None;

	public string? Type => this ? this.Entity!.ObjectKind.ToString() : null;

	[MoonSharpUserDataMetamethod(Metamethod.Stringify)]
	public override string ToString() => this ? $"{this.Type}[{this.Entity!.Name ?? string.Empty}]" : string.Empty;

	public bool? Alive => this ? !this.Entity?.IsDead : null;

	public unsafe MountData Mount {
		get {
			NativeCharacter* player = this.cs;
			if (player is null)
				return new(0);
			NativeCharacter.MountContainer? mount = player->IsMounted() ? player->Mount : null;
			return new(mount?.MountId ?? 0);
		}
	}

	#region Player display

	public string? Name => this
		? this.Entity?.Name?.TextValue ?? string.Empty
		: null;

	public string? Firstname => this.IsPlayer
		? this.Name!.Split(' ')[0]
		: this.Name;

	public string? Lastname => this.IsPlayer
		? this.Name!.Split(' ')[1]
		: this.Name;

	private unsafe Title? playerTitle {
		get {
			if (!this.IsPlayer)
				return null;
			NativeCharacter* player = this.cs;
			CharacterData cdata = player->CharacterData;
			ushort titleId = cdata.TitleID;
			return titleId == 0
				? null
				: ExcelContainer.Titles.GetRow(titleId);
		}
	}
	public bool? HasTitle => this.IsPlayer ? this.playerTitle is not null : null;
	public string? TitleText {
		get {
			if (!this.IsPlayer)
				return null;
			Title? title = this.playerTitle;
			return title is null
				? string.Empty
				: this.MF(title.Masculine, title.Feminine);
		}
	}
	public bool? TitleIsPrefix => this.IsPlayer ? this.playerTitle?.IsPrefix : null;

	public string? CompanyTag => this && this.Entity is Character self ? self.CompanyTag.TextValue : null;

	#endregion

	#region Gender

	public unsafe bool? IsMale => this ? this.go->Gender == 0 : null;
	public unsafe bool? IsFemale => this ? this.go->Gender == 1 : null;
	public unsafe bool? IsGendered => this ? (this.IsMale ?? false) || (this.IsFemale ?? false) : null;

	public string? MF(string male, string female) => this.MFN(male, female, null!);
	public string? MFN(string male, string female, string neither) => this ? (this.IsGendered ?? false) ? (this.IsMale ?? false) ? male : female : neither : null;

	public DynValue MF(DynValue male, DynValue female) => this.MFN(male, female, DynValue.Nil);
	public DynValue MFN(DynValue male, DynValue female, DynValue neither) => this ? (this.IsGendered ?? false) ? (this.IsMale ?? false) ? male : female : neither : DynValue.Nil;

	#endregion

	#region Worlds

	public ushort? HomeWorldId => this.IsPlayer && this.Entity is PlayerCharacter p ? (ushort)p.HomeWorld.GameData!.RowId : null;
	public string? HomeWorld => this.IsPlayer && this.Entity is PlayerCharacter p ? p.HomeWorld.GameData!.Name!.RawString : null;

	public ushort? CurrentWorldId => this.IsPlayer && this.Entity is PlayerCharacter p ? (ushort)p.CurrentWorld.GameData!.RowId : null;
	public string? CurrentWorld => this.IsPlayer && this.Entity is PlayerCharacter p ? p.CurrentWorld.GameData!.Name!.RawString : null;

	#endregion

	#region Entity type

	public bool IsPlayer => this && this.Entity?.ObjectKind is ObjectKind.Player;
	public bool IsCombatNpc => this && this.Entity?.ObjectKind is ObjectKind.BattleNpc;
	public bool IsTalkNpc => this && this.Entity?.ObjectKind is ObjectKind.EventNpc;
	public bool IsNpc => this.IsCombatNpc || this.IsTalkNpc;
	public bool IsTreasure => this && this.Entity?.ObjectKind is ObjectKind.Treasure;
	public bool IsAetheryte => this && this.Entity?.ObjectKind is ObjectKind.Aetheryte;
	public bool IsGatheringNode => this && this.Entity?.ObjectKind is ObjectKind.GatheringPoint;
	public bool IsEventObject => this && this.Entity?.ObjectKind is ObjectKind.EventObj;
	public bool IsMount => this && this.Entity?.ObjectKind is ObjectKind.MountType;
	public bool IsMinion => this && this.Entity?.ObjectKind is ObjectKind.Companion;
	public bool IsRetainer => this && this.Entity?.ObjectKind is ObjectKind.Retainer;
	public bool IsArea => this && this.Entity?.ObjectKind is ObjectKind.Area;
	public bool IsHousingObject => this && this.Entity?.ObjectKind is ObjectKind.Housing;
	public bool IsCutsceneObject => this && this.Entity?.ObjectKind is ObjectKind.Cutscene;
	public bool IsCardStand => this && this.Entity?.ObjectKind is ObjectKind.CardStand;
	public bool IsOrnament => this && this.Entity?.ObjectKind is ObjectKind.Ornament;

	#endregion

	#region Stats

	public byte? Level => this && this.Entity is Character self ? self.Level : null;

	public JobData Job {
		get {
			return this && this.Entity is Character self
				? new(self.ClassJob!.Id, self.ClassJob!.GameData!.Name!.ToString().ToLower(), self.ClassJob!.GameData!.Abbreviation!.ToString().ToUpper())
				: new(0, JobData.InvalidJobName, JobData.InvalidJobAbbr);
		}
	}

	public uint? Hp => this && this.Entity is Character self && self.MaxHp > 0 ? self.CurrentHp : null;
	public uint? MaxHp => this && this.Entity is Character self ? self.MaxHp : null;

	public uint? Mp => this && this.Entity is Character self && self.MaxMp > 0 ? self.CurrentMp : null;
	public uint? MaxMp => this && this.Entity is Character self ? self.MaxMp : null;

	public uint? Cp => this && this.Entity is Character self && self.MaxCp > 0 ? self.CurrentCp : null;
	public uint? MaxCp => this && this.Entity is Character self ? self.MaxCp : null;

	public uint? Gp => this && this.Entity is Character self && self.MaxGp > 0 ? self.CurrentGp : null;
	public uint? MaxGp => this && this.Entity is Character self ? self.MaxGp : null;

	#endregion

	#region Flags

	public bool IsHostile => this && this.Entity is Character self && self.StatusFlags.HasFlag(StatusFlags.Hostile);
	public bool InCombat => this && this.Entity is Character self && self.StatusFlags.HasFlag(StatusFlags.InCombat);
	public bool WeaponDrawn => this && this.Entity is Character self && self.StatusFlags.HasFlag(StatusFlags.WeaponOut);
	public bool IsPartyMember => this && this.Entity is Character self && self.StatusFlags.HasFlag(StatusFlags.PartyMember);
	public bool IsAllianceMember => this && this.Entity is Character self && self.StatusFlags.HasFlag(StatusFlags.AllianceMember);
	public bool IsFriend => this && this.Entity is Character self && self.StatusFlags.HasFlag(StatusFlags.Friend);
	public bool IsCasting => this && this.Entity is BattleChara self && self.IsCasting;
	public bool CanInterrupt => this && this.Entity is BattleChara self && self.IsCasting && self.IsCastInterruptible;

	#endregion

	#region Position
	// X and Z are the horizontal coordinates, Y is the vertical one
	// But that's not how the game displays things to the player, because fuck you I guess, so we swap those two around for consistency

	public float? PosX => this ? this.Entity!.Position.X : null;
	public float? PosY => this ? this.Entity!.Position.Z : null;
	public float? PosZ => this ? this.Entity!.Position.Y : null;

	private Vector3? uiCoords {
		get {
			if (Service.ClientState.TerritoryType > 0 && this.Exists) {
				Map? map = Service.DataManager.GetExcelSheet<Map>()!.GetRow(Service.ClientState.TerritoryType);
				TerritoryTypeTransient? territoryTransient = Service.DataManager.GetExcelSheet<TerritoryTypeTransient>()!.GetRow(Service.ClientState.TerritoryType);
				if (map is not null && territoryTransient is not null) {
					return MapUtil.WorldToMap(this.Entity!.Position, map, territoryTransient, true);
				}
			}
			return null;
		}
	}

	[LuaDoc("The player-friendly map-style X (east/west) coordinate of this entity.")]
	public float? MapX => this.uiCoords?.X;
	[LuaDoc("The player-friendly map-style Y (north/south) coordinate of this entity.")]
	public float? MapY => this.uiCoords?.Y;
	[LuaDoc("The player-friendly map-style Z (height) coordinate of this entity.")]
	public float? MapZ => this.uiCoords?.Z;

	public DynValue MapCoords {
		get {
			Vector3? coords = this.uiCoords;
			return coords is not null
				? DynValue.NewTuple(DynValue.NewNumber(coords.Value.X), DynValue.NewNumber(coords.Value.Y), DynValue.NewNumber(coords.Value.Z))
				: DynValue.NewTuple(null, null, null);
		}
	}

	public double? RotationRadians => this.Entity?.Rotation is float rad ? rad + Math.PI : null;
	public double? RotationDegrees => this.RotationRadians is double rad ? rad * 180 / Math.PI : null;

	#endregion

	#region Distance

	public float? FlatDistanceFrom(EntityWrapper? other) => this.Exists && (other?.Exists ?? false)
		? Vector3.Distance(this.Entity!.Position.WithY(0), other!.Entity!.Position.WithY(0))
		: null;
	public float? FlatDistanceFrom(PlayerApi player) => this.FlatDistanceFrom(player.Entity);

	public float? DistanceFrom(EntityWrapper? other) => this.Exists && (other?.Exists ?? false)
		? Vector3.Distance(this.Entity!.Position, other.Entity!.Position)
		: null;
	public float? DistanceFrom(PlayerApi player) => this.DistanceFrom(player.Entity);

	public float? FlatDistance => this.FlatDistanceFrom(Service.ClientState.LocalPlayer);
	public float? Distance => this.DistanceFrom(Service.ClientState.LocalPlayer);

	#endregion

	#region Target

	public EntityWrapper Target => new(this ? this.Entity!.TargetObject : null);
	public bool? HasTarget => this.Target;

	#endregion

	#region IEquatable
	public bool Equals(EntityWrapper? other) => this.Entity == other?.Entity;
	public override int GetHashCode() => this.Entity?.GetHashCode() ?? 0;
	#endregion

}
