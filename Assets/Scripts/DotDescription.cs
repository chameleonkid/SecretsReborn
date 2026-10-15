using System.Globalization;
using System.Text;

namespace SecretsReborn
{
    public static class DotDescription
    {
        public static string Append(string description,DotApplication[] effects)
        {
            if(effects==null || effects.Length==0) return description;
            var text=new StringBuilder(description);
            foreach(var effect in effects)
            {
                if(effect==null || !effect.Valid) continue;
                if(text.Length>0) text.Append('\n');
                text.Append(effect.kind).Append(": ").Append(effect.damagePerTick);
                if(effect.tickInterval==1) text.Append(" Schaden/s");
                else text.Append(" Schaden alle ").Append(Number(effect.tickInterval)).Append(" s");
                text.Append(" für ").Append(Number(effect.duration)).Append(" s · ")
                    .Append(Number(effect.chancePercent)).Append(" % Chance");
            }
            return text.ToString();
        }
        private static string Number(float value) => value.ToString("0.######",CultureInfo.GetCultureInfo("de-DE"));
    }
}
