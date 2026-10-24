using System;using System.Linq;using System.Collections.Generic;
public static class TaskbarScreens {
 public static string[] Targets(bool all,string selected,string primary,IEnumerable<string> connected){var devices=connected.Where(s=>!String.IsNullOrEmpty(s)).Distinct().ToArray();string fallback=devices.Contains(primary)?primary:devices.FirstOrDefault()??"";if(!all)return new[]{devices.Contains(selected)?selected:fallback};return new[]{fallback}.Concat(devices.Where(s=>s!=fallback)).ToArray();}
}
