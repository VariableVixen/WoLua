using System;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel.Sheets;

using MoonSharp.Interpreter;

using VariableVixen.WoLua.Lua.Docs;

using VariableVixen.WoLua.Constants;

using CharacterData = FFXIVClientStructs.FFXIV.Client.Game.Character.CharacterData;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace VariableVixen.WoLua.Lua.Api.Game;

[MoonSharpUserData]
[MoonSharpHideMember(nameof(Entity))]
[MoonSharpHideMember(nameof(Equals))]
[MoonSharpHideMember("<Clone>$")]
[MoonSharpHideMember(nameof(Deconstruct))]
public sealed record class EntityWrapper(IGameObject? Entity): IWorldObjectWrapper, IEquatable<EntityWrapper> { // TODO luadoc all of this
	public static readonly EntityWrapper Empty = new((IGameObject?)null);

	#region Conversions
	private unsafe NativeGameObject* go => this ? (NativeGameObject*)this.Entity!.Address : null;
	private unsafe NativeCharacter* cs => this.IsPlayer ? (NativeCharacter*)this.Entity!.Address : null;

	public static implicit operator bool(EntityWrapper? entity) => entity?.Exists ?? false;
	#endregion

	public bool Exists => this.Entity is not null && this.Entity.IsValid() && this.Entity.ObjectKind is not ObjectKind.None;

	public string? Type => this ? this.Entity!.ObjectKind.ToString() : null;

	[MoonSharpUserDataMetamethod(Metamethod.Stringify)]
	public override string ToString() => this ? $"{this.Type}[{this.Name ?? string.Empty}]" : string.Empty;

	public bool? Alive => this ? !this.Entity?.IsDead : null;

	public unsafe MountWrapper Mount {
		get {
			NativeCharacter* player = this.cs;
			if (player is null)
				return new(0);
			MountContainer? mount = player->IsMounted() ? player->Mount : null;
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
			ushort titleId = cdata.TitleId;
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
			return title.HasValue
				? this.MF(title.Value.Masculine.ToString(), title.Value.Feminine.ToString())
				: string.Empty;
		}
	}
	public bool? TitleIsPrefix => this.IsPlayer ? this.playerTitle?.IsPrefix : null;

	public string? CompanyTag => this && this.Entity is ICharacter self ? self.CompanyTag.TextValue : null;

	#endregion

	#region Gender

	public unsafe bool? IsMale => this ? this.go->Sex == 0 : null;
	public unsafe bool? IsFemale => this ? this.go->Sex == 1 : null;
	public unsafe bool? IsGendered => this ? (this.IsMale ?? false) || (this.IsFemale ?? false) : null;

	public string? MF(string male, string female) => this.MFN(male, female, null!);
	public string? MFN(string male, string female, string neither) => this ? this.IsGendered ?? false ? this.IsMale ?? false ? male : female : neither : null;

	public DynValue MF(DynValue male, DynValue female) => this.MFN(male, female, DynValue.Nil);
	public DynValue MFN(DynValue male, DynValue female, DynValue neither) => this ? this.IsGendered ?? false ? this.IsMale ?? false ? male : female : neither : DynValue.Nil;

	#endregion

	#region Worlds

	public ushort? HomeWorldId => this.IsPlayer && this.Entity is IPlayerCharacter p ? (ushort)p.HomeWorld.Value.RowId : null;
	public string? HomeWorld => this.IsPlayer && this.Entity is IPlayerCharacter p ? p.HomeWorld.Value.Name!.ToString() : null;

	public ushort? CurrentWorldId => this.IsPlayer && this.Entity is IPlayerCharacter p ? (ushort)p.CurrentWorld.Value.RowId : null;
	public string? CurrentWorld => this.IsPlayer && this.Entity is IPlayerCharacter p ? p.CurrentWorld.Value.Name!.ToString() : null;

	#endregion

	#region Entity type

	public bool IsPlayer => this && this.Entity?.ObjectKind is ObjectKind.Pc;
	public bool IsCombatNpc => this && this.Entity?.ObjectKind is ObjectKind.BattleNpc;
	public bool IsTalkNpc => this && this.Entity?.ObjectKind is ObjectKind.EventNpc;
	public bool IsNpc => this.IsCombatNpc || this.IsTalkNpc;
	public bool IsTreasure => this && this.Entity?.ObjectKind is ObjectKind.Treasure;
	public bool IsAetheryte => this && this.Entity?.ObjectKind is ObjectKind.Aetheryte;
	public bool IsGatheringNode => this && this.Entity?.ObjectKind is ObjectKind.GatheringPoint;
	public bool IsEventObject => this && this.Entity?.ObjectKind is ObjectKind.EventObj;
	public bool IsMount => this && this.Entity?.ObjectKind is ObjectKind.Mount;
	public bool IsMinion => this && this.Entity?.ObjectKind is ObjectKind.Companion;
	public bool IsRetainer => this && this.Entity?.ObjectKind is ObjectKind.Retainer;
	public bool IsArea => this && this.Entity?.ObjectKind is ObjectKind.AreaObject;
	public bool IsHousingObject => this && this.Entity?.ObjectKind is ObjectKind.HousingEventObject;
	public bool IsCutsceneObject => this && this.Entity?.ObjectKind is ObjectKind.Cutscene;
	public bool IsCardStand => this && this.Entity?.ObjectKind is ObjectKind.CardStand;
	public bool IsOrnament => this && this.Entity?.ObjectKind is ObjectKind.Ornament;

	#endregion

	#region Stats

	public byte? Level => this && this.Entity is ICharacter self ? self.Level : null;

	public JobData Job {
		get {
			return this && this.Entity is ICharacter self
				? new(self.ClassJob.RowId, self.ClassJob!.Value.Name!.ToString().ToLower(), self.ClassJob!.Value.Abbreviation!.ToString().ToUpper())
				: new(0, JobData.InvalidJobName, JobData.InvalidJobAbbr);
		}
	}

	public uint? Hp => this && this.Entity is ICharacter self && self.MaxHp > 0 ? self.CurrentHp : null;
	public uint? MaxHp => this && this.Entity is ICharacter self ? self.MaxHp : null;

	public uint? Mp => this && this.Entity is ICharacter self && self.MaxMp > 0 ? self.CurrentMp : null;
	public uint? MaxMp => this && this.Entity is ICharacter self ? self.MaxMp : null;

	public uint? Cp => this && this.Entity is ICharacter self && self.MaxCp > 0 ? self.CurrentCp : null;
	public uint? MaxCp => this && this.Entity is ICharacter self ? self.MaxCp : null;

	public uint? Gp => this && this.Entity is ICharacter self && self.MaxGp > 0 ? self.CurrentGp : null;
	public uint? MaxGp => this && this.Entity is ICharacter self ? self.MaxGp : null;

	#endregion

	#region Flags

	public bool IsHostile => this && this.Entity is ICharacter self && self.StatusFlags.HasFlag(StatusFlags.Hostile);
	public bool InCombat => this && this.Entity is ICharacter self && self.StatusFlags.HasFlag(StatusFlags.InCombat);
	public bool WeaponDrawn => this && this.Entity is ICharacter self && self.StatusFlags.HasFlag(StatusFlags.WeaponOut);
	public bool IsPartyMember => this && this.Entity is ICharacter self && self.StatusFlags.HasFlag(StatusFlags.PartyMember);
	public bool IsAllianceMember => this && this.Entity is ICharacter self && self.StatusFlags.HasFlag(StatusFlags.AllianceMember);
	public bool IsFriend => this && this.Entity is ICharacter self && self.StatusFlags.HasFlag(StatusFlags.Friend);
	public bool IsCasting => this && this.Entity is IBattleChara self && self.IsCasting;
	public bool CanInterrupt => this && this.Entity is IBattleChara self && self.IsCasting && self.IsCastInterruptible;

	#endregion

	#region Position
	// X and Z are the horizontal coordinates, Y is the vertical one
	// But that's not how the game displays things to the player, because fuck you I guess, so we swap those two around for consistency

	public float? PosX => this ? this.Entity!.Position.X : null;
	public float? PosY => this ? this.Entity!.Position.Z : null;
	public float? PosZ => this ? this.Entity!.Position.Y : null;

	public WorldPosition Position => new(this.PosX, this.PosY, this.PosZ);

	[LuaDoc("The player-friendly map-style X (east/west) coordinate of this entity.")]
	public float? MapX => this.Position.MapX;
	[LuaDoc("The player-friendly map-style Y (north/south) coordinate of this entity.")]
	public float? MapY => this.Position.MapY;
	[LuaDoc("The player-friendly map-style Z (height) coordinate of this entity.")]
	public float? MapZ => this.Position.MapZ;

	public DynValue MapCoords {
		get {
			Vector3? coords = this.Position.UiCoords;
			return coords is not null
				? DynValue.NewTuple(DynValue.NewNumber(coords.Value.X), DynValue.NewNumber(coords.Value.Y), DynValue.NewNumber(coords.Value.Z))
				: DynValue.NewTuple(null, null, null);
		}
	}

	public double? RotationRadians => this.Entity?.Rotation is float rad ? rad + Math.PI : null;
	public double? RotationDegrees => this.RotationRadians is double rad ? rad * 180 / Math.PI : null;

	#endregion

	#region Distance

	public float? FlatDistanceFrom(EntityWrapper? other) => this.Position.FlatDistanceFrom(other);
	public float? FlatDistanceFrom(PlayerApi player) => this.FlatDistanceFrom(player.Entity);

	public float? DistanceFrom(EntityWrapper? other) => this.Position.DistanceFrom(other);
	public float? DistanceFrom(PlayerApi player) => this.DistanceFrom(player.Entity);

	public float? FlatDistance => this.Position.FlatDistance;
	public float? Distance => this.Position.Distance;

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
