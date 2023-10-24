// See https://aka.ms/new-console-template for more information
using CardDrawAnalysis;

Console.WriteLine("Starting Calculations...");
DateTime dtStart = DateTime.Now;

string outdir = @"C:\mtg_out";

var t1 = Task.Run(() =>
{
    Probabilities probabilities60 = new Probabilities();

    probabilities60.WriteData(outdir, "mtg_exact", ProbabilityType.Exact, CardDrawAnalysis.MatchType.Bo3);
    probabilities60.WriteData(outdir, "mtg_atleast", ProbabilityType.AtLeast, CardDrawAnalysis.MatchType.Bo3);
    probabilities60.WriteData(outdir, "mtg_atmost", ProbabilityType.AtMost, CardDrawAnalysis.MatchType.Bo3);
    probabilities60.WriteData(outdir, "mtg_atmost_bo1_inverse", ProbabilityType.AtMost, CardDrawAnalysis.MatchType.Bo1, InverseType.Inverse);
});

var t2 = Task.Run(() =>
{
    Probabilities probabilities53 = new Probabilities(53);

    probabilities53.WriteData(outdir, "mtg_exact", ProbabilityType.Exact, CardDrawAnalysis.MatchType.Bo3);
    probabilities53.WriteData(outdir, "mtg_atleast", ProbabilityType.AtLeast, CardDrawAnalysis.MatchType.Bo3);
    probabilities53.WriteData(outdir, "mtg_atmost", ProbabilityType.AtMost, CardDrawAnalysis.MatchType.Bo3);
    probabilities53.WriteData(outdir, "mtg_atmost_bo1_inverse", ProbabilityType.AtMost, CardDrawAnalysis.MatchType.Bo1, InverseType.Inverse);
});

Task.WaitAll(t1, t2);

Console.WriteLine("Files written to {0}.", outdir);

DateTime dtEnd = DateTime.Now;
var time = dtEnd - dtStart;

Console.WriteLine("Elapsed Time: {0:c}", time);

Console.WriteLine("Press Enter to exit.");

Console.ReadLine();