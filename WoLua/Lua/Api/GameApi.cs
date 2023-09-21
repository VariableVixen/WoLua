namespace PrincessRTFM.WoLua.Lua.Api;

using System.Linq;

using MoonSharp.Interpreter;

using PrincessRTFM.WoLua.Constants;
using PrincessRTFM.WoLua.Game;
using PrincessRTFM.WoLua.Lua.Api.Game;
using PrincessRTFM.WoLua.Ui.Chat;

// This API is for everything pertaining to the actual game, including holding more specific APIs.
[MoonSharpUserData]
public class GameApi: ApiBase {

	#region Initialisation

	[MoonSharpHidden]
	internal GameApi(ScriptContainer source) : base(source) { }

	#endregion

	#region Sub-APIs

	public PlayerApi Player { get; private set; } = null!;
	public ChocoboApi Chocobo { get; private set; } = null!;
	public ToastApi Toast { get; private set; } = null!;
	public DalamudApi Dalamud { get; private set; } = null!;

	#endregion

	#region Chat
	public void PrintMessage(params DynValue[] messages) {
		if (this.Disposed)
			return;

		string message = string.Join(
			" ",
			messages.Select(dv => ToUsefulString(dv))
		);
		this.Log(message, LogTag.LocalChat);
		Service.Plugin.Print(message, null, this.Owner.PrettyName);
	}

	public void PrintError(params DynValue[] messages) {
		if (this.Disposed)
			return;

		string message = string.Join(
			" ",
			messages.Select(dv => ToUsefulString(dv))
		);
		this.Log(message, LogTag.LocalChat);
		Service.Plugin.Print(message, Foreground.Error, this.Owner.PrettyName);
	}

	public void SendChat(string chatline) {
		if (this.Disposed)
			return;

		string cleaned = Service.Common.Functions.Chat.SanitiseText(chatline);
		if (!string.IsNullOrWhiteSpace(cleaned)) {
			this.Log(cleaned, LogTag.ServerChat);
			Service.Common.Functions.Chat.SendMessage(cleaned);
		}
	}
	#endregion

	public bool? PlaySoundEffect(int id) {
		if (this.Disposed)
			return null;

		if (!Service.Sounds.Valid)
			return null;
		Sound sound = SoundsExtensions.FromGameIndex(id);
		if (sound.IsSound())
			Service.Sounds.Play(sound);
		return sound.IsSound();
	}

	// TODO map flags?
	// TODO allow examining the object table directly (would allow searching for objects matching criteria, could be useful)
	// TODO allow examining the FATE table directly (would allow effectively recreating TinyCmd's `/fate` command)
	// TODO allow checking game settings via Service.GameConfig
	// TODO allow accessing job gauge data via Service.JobGauges

}
