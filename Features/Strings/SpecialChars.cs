using KeyControl.Configuration;
using KeyControl.Util;
using PlayifyUtility.Jsons;
using PlayifyUtility.Utils.Extensions;
using PlayifyUtility.Windows.Features.Hooks;
using PlayifyUtility.Windows.Features.Interact;

namespace KeyControl.Features.Strings;

[InitOnLoad]
public static class SpecialChars{
	private static readonly ConfigValue<Dictionary<Keys,string>> KeyMap=new(
		new Dictionary<Keys,string>{
			{Keys.A,"α"},
			{Keys.B,"β"},
			{Keys.C,"γ"},
			{Keys.O,"Ω"},
			{Keys.W,"ω"},
			{Keys.D,"Δ"},
			{Keys.S,"ϟ"},
			{Keys.M,"μ"},
			{Keys.N,"η"},
			{Keys.T,"τ"},
			{Keys.V,"ϑ"},
			{Keys.Space,"​"},
			{Keys.P,"π"},
			{Keys.L,"λ"},
			{Keys.Y,"|"},
		},
		json=>json.AsObject().Select(pair=>(SendBuilder.StringToKey(pair.Key),pair.Value.AsString())).ToDictionary(),
		dict=>new JsonObject(dict.Select(pair=>(SendBuilder.KeyToString(pair.Key),(Json)pair.Value))),
		true,
		"SpecialChars");

	static SpecialChars()=>GlobalKeyboardHook.KeyDown+=KeyDown;

	private static void KeyDown(KeyEvent e){
		if(Modifiers.Combined!=ModifierKeys.AltGr) return;
		if(!KeyMap.Value.TryGetValue(e.Key,out var text)) return;

		e.Handled=true;
		if(true) new Send().Text(text).SendNow();
	}
}