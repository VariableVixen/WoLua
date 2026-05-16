using System.Numerics;

using Dalamud.Game.ClientState.Fates;

using MoonSharp.Interpreter;

using VariableVixen.WoLua.Constants;
using VariableVixen.WoLua.Lua.Docs;

using Fate = Dalamud.Game.ClientState.Fates.IFate;
using FateContext = FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext;

namespace VariableVixen.WoLua.Lua.Api.Game;

[MoonSharpUserData]
[MoonSharpHideMember(nameof(WorldFate))]
[MoonSharpHideMember(nameof(Equals))]
[MoonSharpHideMember("<Clone>$")]
[MoonSharpHideMember(nameof(Deconstruct))]
public sealed record class FateWrapper(Fate? WorldFate): IWorldObjectWrapper {
	public static readonly FateWrapper Empty = new((Fate?)null);

	internal unsafe FateContext* Struct => this.Valid ? (FateContext*)this.WorldFate!.Address : null;

	[LuaDoc("Whether this FATE is a valid object in the game's memory. If this is false, this wrapper is meaningless.")]
	public bool Valid => this.WorldFate is Fate fate && Service.Objects.LocalPlayer?.IsValid() is true;

	[LuaDoc("Whether this FATE still exists in the world. This will be true if the FATE is valid and has not yet ended, even if it is WAITING to end.")]
	public bool Exists => this.State is not (FateState.Ended or FateState.Failed);

	#region Display

	[LuaDoc("The name of this FATE.")]
	public string? Name => this.Valid ? this.WorldFate!.Name.TextValue : null;

	[LuaDoc("The current progress of this FATE towards its completion, as a percentage.")]
	public byte? Progress => this.Exists ? this.WorldFate!.Progress! : null;

	#endregion

	#region Levels

	[LuaDoc("The (minimum) level of this FATE. You are recommended to be at least this level to participate.",
		"If you are below this level, the game weighs your contribution less, and combat is expected to be much harder.")]
	public byte? MinLevel => this.Valid ? this.WorldFate!.Level! : null;

	[LuaDoc("The maximum level of this FATE, above which you need to manually level sync down in order to participate.")]
	public unsafe byte? MaxLevel => this.Valid ? this.Struct->MaxLevel : null;

	#endregion

	#region Time

	[LuaDoc("The total duration of this FATE, in seconds.",
		"If this FATE has already ended, this will be `nil`.")]
	public int? Duration => this.Valid ? this.WorldFate!.Duration : null;

	[LuaDoc("The remaining time to complete this FATE, in seconds.",
		"If this FATE has not yet started, this will be equal to `.Duration`.",
		"If this FATE has already ended, this will be `nil`.")]
	public int? TimeLeft => this.Valid
		? this.State switch {
			FateState.Preparing => this.Duration,
			FateState.Running or FateState.Ending => (int)this.WorldFate!.TimeRemaining,
			FateState.Ended or FateState.Failed => 0,
			_ => null,
		}
		: null;

	[LuaDoc("The amount of time this FATE has so far been going for, in seconds.",
		"This is functionally equivalent to `.Duration - .TimeLeft`.",
		"If this FATE has not yet started, this will be `0`.",
		"If this FATE has already ended, this will be equal to `.Duration`.")]
	public int? TimeElapsed => this.Duration is int total && this.TimeLeft is int left ? total - left : null;

	#endregion

	#region States

	[MoonSharpHidden]
	public FateState State => this.Valid ? this.WorldFate!.State : FateState.Ended;

	[LuaDoc("Whether this FATE is in the preparation phase, before it is actually started.")]
	public bool Waiting => this.State is FateState.Preparing;

	[LuaDoc("Whether this FATE is currently running.")]
	public bool Running => this.State is FateState.Running;

	[LuaDoc("Whether this FATE is currently in its ending phase, as seen in item turnin FATEs.")]
	public bool Ending => this.State is FateState.Ending;

	[LuaDoc("Whether this FATE is considered \"active\", either waiting to be started or currently in progress.")]
	public bool Active => this.State is FateState.Preparing or FateState.Running;

	#endregion

	#region Position
	// X and Z are the horizontal coordinates, Y is the vertical one
	// But that's not how the game displays things to the player, because fuck you I guess, so we swap those two around for consistency

	[LuaPlayerDoc("The raw (internal) X coordinate of this FATE.",
		"This represents the east/west position.")]
	public float? PosX => this.Exists ? this.WorldFate!.Position.X : null;

	[LuaPlayerDoc("The raw (internal) Y coordinate of this FATE.",
		"This represents the north/south position.",
		"Please note that this is _actually_ the internal _Z_ coordinate, since the game engine uses X/Z for horizontal position and Y for vertical.",
		"For the sake of consistency with the map coordinates displayed to the player, " + Plugin.Name + " swaps them.")]
	public float? PosY => this.Exists ? this.WorldFate!.Position.Z : null;

	[LuaPlayerDoc("The raw (internal) Z coordinate of this FATE.",
		"This represents the vertical position.",
		"Please note that this is _actually_ the internal _Y_ coordinate, since the game engine uses X/Z for horizontal position and Y for vertical.",
		"For the sake of consistency with the map coordinates displayed to the player, " + Plugin.Name + " swaps them.")]
	public float? PosZ => this.Exists ? this.WorldFate!.Position.Y : null;

	public WorldPosition Position => new(this.PosX, this.PosY, this.PosZ);

	[LuaDoc("The player-friendly map-style X (east/west) coordinate of this FATE.")]
	public float? MapX => this.Position.MapX;
	[LuaDoc("The player-friendly map-style Y (north/south) coordinate of this FATE.")]
	public float? MapY => this.Position.MapY;
	[LuaDoc("The player-friendly map-style Z (height) coordinate of this FATE.")]
	public float? MapZ => this.Position.MapZ;

	[LuaDoc("Provides three values consisting of this FATE's X (east/west), Y (north/south), and Z (vertical) coordinates.",
		"If the FATE doesn't exist in the world (`.Exists == false`), all three values will be nil.")]
	public DynValue MapCoords {
		get {
			Vector3? coords = this.Position.UiCoords;
			return coords is not null
				? DynValue.NewTuple(DynValue.NewNumber(coords.Value.X), DynValue.NewNumber(coords.Value.Y), DynValue.NewNumber(coords.Value.Z))
				: DynValue.NewTuple(null, null, null);
		}
	}

	[LuaDoc("The radius of this FATE.",
		"FATEs are defined as cylinders, with their position being the central point at the bottom.",
		"This is the maximum (horizontal) distance you can be from the middle of it.")]
	public unsafe float? Radius => this.Valid ? this.Struct->Radius : null;

	#endregion

	#region Distance

	[LuaDoc("The flat (horizontal only) distance between the player and the \"position\" of this FATE.",
		"FATEs are defined as cylinders, with their position being the central point at the bottom.")]
	public float? FlatDistanceToCenter => this.Exists
		? this.Position.FlatDistance
		: null;
	[SkipDoc("alternative spelling only")]
	public float? FlatDistanceToCentre => this.FlatDistanceToCenter;

	[LuaDoc("The three-dimensional distance between the player and the \"position\" of this FATE.",
		"FATEs are defined as cylinders, with their position being the central point at the bottom.",
		"Note that this means that if you're above a FATE, the distance to actually \"enter\" it is less than this would suggest.",
		"However, you probably still need to go about that far in order to reach the ground where the objectives will be.")]
	public float? DistanceToCenter => this.Exists
		? this.Position.Distance
		: null;
	[SkipDoc("alternative spelling only")]
	public float? DistanceToCentre => this.DistanceToCenter;

	[LuaDoc("The flat (horizontal only) distance between the player and the edge of this FATE.",
		"This is calculated as the distance to the central point minus the radius.",
		"If the player is currently within the FATE's (horizontal) bounds, this will be negative.",
		"Note that FATEs are defined as cylinders, with their position being the central point at the bottom.",
		"As a result, even if this is negative, the player may be above the FATE's bounds.")]
	public float? FlatDistanceToEdge => this.FlatDistanceToCenter is float dist && this.Radius is float size ? dist - size : null;

	// There is no method for 3d edge distance, due to FATEs being cylinders and not being able to get their height.
	// A calculation based on the distance to the centre minus the radius would be invalid,
	// since that would only calculate distance to a SPHERICAL border around the FATE's position.

	#endregion

	[MoonSharpUserDataMetamethod(Metamethod.Stringify)]
	[LuaDoc("Creates a string describing the FATE, including its name, state, minimum level, and maximum level.")]
	public override string ToString() => this.Valid ? $"FATE[{this.Name ?? "<ERROR: UNKNOWN FATE>"} - {this.State}, {this.MinLevel}/{this.MaxLevel}]" : "FATE[invalid]";

	#region Conversions

	public static implicit operator bool(FateWrapper? fate) => fate?.Exists ?? false;

	#endregion

}
