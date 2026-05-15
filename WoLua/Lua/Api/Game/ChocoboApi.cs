using FFXIVClientStructs.FFXIV.Client.Game.UI;

using MoonSharp.Interpreter;

using VariableVixen.WoLua.Constants;

namespace VariableVixen.WoLua.Lua.Api.Game;

[MoonSharpUserData]
public class ChocoboApi(ScriptContainer source): ApiBase(source) { // TODO luadoc all of this
	private unsafe CompanionInfo? obj {
		get {
			UIState* ui = UIState.Instance();
			return ui is null ? null : ui->Buddy.CompanionInfo;
		}
	}
	public static implicit operator bool(ChocoboApi? bird) => bird?.obj is not null;

	public float? TimeLeft => this.obj?.TimeLeft;
	public bool? Summoned => this ? (this.TimeLeft ?? 0) > 0 : null;
	public uint? CurrentXP => this.obj?.CurrentXP;
	public byte? Rank => this.obj?.Rank;
	public bool? Unlocked => this ? (this.Rank ?? 0) > 0 : null;
	public byte? Stars => this.obj?.Stars;
	public byte? SkillPoints => this.obj?.SkillPoints;
	public byte? DefenderLevel => this.obj?.Levels[0];
	public byte? AttackerLevel => this.obj?.Levels[1];
	public byte? HealerLevel => this.obj?.Levels[2];
	public unsafe string? Name => this.obj?.NameString;

	public unsafe uint? CurrentHp => this.Summoned ?? false ? this.obj!.Value.Companion->CurrentHealth : null;
	public unsafe uint? MaxHp => this.Summoned ?? false ? this.obj!.Value.Companion->MaxHealth : null;

	[MoonSharpUserDataMetamethod(Metamethod.Stringify)]
	public override string ToString() => this.Name ?? string.Empty;
}
