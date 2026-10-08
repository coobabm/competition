using System; using System.Text; using LingGuangV05.XingGuang;
static class P { static void Main() {
  var sim = new XgSim { GoldChance = 0 };
  foreach (var d in XgCatalog.Desks) if (!sim.S.desksOpen.Contains(d.id)) sim.S.desksOpen.Add(d.id);
  var sb = new StringBuilder("[");
  bool first = true;
  foreach (var desk in new[] { "logic", "danmu", "spam", "headline", "review" }) {
    sim.S.labels.Add(new XgLabelCount { dataset = desk, count = desk == "logic" ? 120 : 70 });
    for (int i = 0; i < 25; i++) {
      var c = sim.Card(desk);
      string q = desk == "logic" ? c.question : XgMemes.Question(desk) + "「" + c.question + "」";
      if (!first) sb.Append(","); first = false;
      sb.Append("{\"desk\":\"").Append(desk).Append("\",\"q\":").Append(System.Text.Json.JsonSerializer.Serialize(q)).Append(",\"truth\":").Append(c.truth ? "true" : "false").Append(",\"trick\":").Append(c.trick ? "true" : "false").Append("}");
      sim.Answer(desk, c.truth, new H());
    }
  }
  Console.WriteLine(sb.Append("]").ToString());
} }
sealed class H : IXgHost { public double Compute => 1; public double VramMB => 2048; public double Money => 0; public bool Spend(double a) => false; public void Earn(double a) {} public void Train(double s) {} public string Blocker => null; }
